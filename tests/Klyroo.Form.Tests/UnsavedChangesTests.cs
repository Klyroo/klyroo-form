using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class UnsavedChangesTests : BunitContext
{
    private static FormSchema Schema() => new()
    {
        FormKey = "draft",
        Steps =
        [
            new FormStepSchema
            {
                Key = "a",
                Title = "A",
                Fields =
                [
                    new FormFieldSchema { Key = "name", Label = "Name" },
                    new FormFieldSchema { Key = "age", Kind = FormFieldKind.Number },
                    new FormFieldSchema { Key = "rating", Kind = FormFieldKind.Rating },
                    new FormFieldSchema { Key = "agree", Kind = FormFieldKind.Switch },
                    new FormFieldSchema { Key = "tags", Kind = FormFieldKind.Tag },
                    new FormFieldSchema { Key = "password", Kind = FormFieldKind.Password },
                    new FormFieldSchema { Key = "code", Kind = FormFieldKind.OneTimeCode },
                    new FormFieldSchema
                    {
                        Key = "lines",
                        Kind = FormFieldKind.ArrayList,
                        Fields =
                        [
                            new FormFieldSchema { Key = "sku" },
                            new FormFieldSchema { Key = "qty", Kind = FormFieldKind.Number },
                        ],
                    },
                ],
            },
            new FormStepSchema { Key = "b", Title = "B", Fields = [new FormFieldSchema { Key = "note" }] },
        ],
    };

    private static readonly Dictionary<string, object?> Initial = new() { ["name"] = "Ada" };

    private static FormController Edited()
    {
        var form = new FormController(Schema(), Initial);
        form.SetValue("name", "Grace");
        form.SetValue("age", 36.0);
        form.SetValue("rating", 4);
        form.SetValue("agree", true);
        form.SetValue("tags", new List<string> { "x", "y" });
        form.SetValue("password", "secret");
        form.SetValue("code", "123456");
        form.AddArrayItem(form.Schema.Steps[0].Fields[7]);
        form.SetArrayItemValue("lines", 0, "sku", "A-1");
        form.SetArrayItemValue("lines", 0, "qty", 3.0);
        form.SetValue("note", "later");
        return form;
    }

    [Fact]
    public void A_restored_draft_is_dirty_against_the_original_baseline()
    {
        var draft = Edited().ExportState();
        var fresh = new FormController(Schema(), Initial);
        Assert.False(fresh.IsDirty);

        fresh.RestoreDraft(draft);

        Assert.True(fresh.IsDirty);
        Assert.Contains("name", fresh.DirtyPaths);
        Assert.Equal("Grace", fresh.GetValue<string>("name"));
        Assert.Equal("Ada", fresh.GetInitialValue("name"));
        Assert.Equal(36, fresh.GetValue<int>("age"));
        Assert.Equal(["x", "y"], fresh.GetValue<List<string>>("tags"));
        Assert.Equal("A-1", fresh.GetArrayItems("lines")[0]["sku"]);
    }

    [Fact]
    public void Reset_with_the_same_values_would_have_made_the_draft_clean_which_is_what_RestoreDraft_avoids()
    {
        var draft = Edited().ExportState();
        var viaReset = new FormController(Schema(), Initial);
        viaReset.Reset(draft.Values);
        Assert.False(viaReset.IsDirty);

        var viaRestore = new FormController(Schema(), Initial);
        viaRestore.RestoreDraft(draft);
        Assert.True(viaRestore.IsDirty);
    }

    [Fact]
    public void Restoring_the_baseline_values_makes_the_form_clean_again()
    {
        var form = new FormController(Schema(), Initial);
        form.SetValue("name", "Grace");
        var typed = form.ExportState();
        form.ResetField("name");
        Assert.False(form.IsDirty);

        form.RestoreDraft(typed);
        Assert.True(form.IsDirty);
        form.RestoreDraft(new FormController(Schema(), Initial).ExportState());
        Assert.False(form.IsDirty);
    }

    [Fact]
    public void ExportState_leaves_sensitive_fields_out_unless_asked_and_copies_lists()
    {
        var form = Edited();

        var draft = form.ExportState();
        Assert.False(draft.Values.ContainsKey("password"));
        Assert.False(draft.Values.ContainsKey("code"));
        Assert.Equal("secret", form.ExportState(includeSensitive: true).Values["password"]);

        form.SetValue("tags", new List<string> { "z" });
        form.SetArrayItemValue("lines", 0, "sku", "CHANGED");
        Assert.Equal(["x", "y"], (List<string>)draft.Values["tags"]!);
        Assert.Equal("A-1", ((List<Dictionary<string, object?>>)draft.Values["lines"]!)[0]["sku"]);
    }

    [Fact]
    public void A_draft_keeps_the_step_and_a_field_it_does_not_mention_keeps_its_value()
    {
        var source = Edited();
        source.SetValue("name", "Grace");
        var draft = new FormDraft(new Dictionary<string, object?> { ["note"] = "n" }, StepIndex: 1);
        var form = new FormController(Schema(), Initial);

        form.RestoreDraft(draft);

        Assert.Equal(1, form.CurrentStepIndex);
        Assert.Equal("Ada", form.GetValue<string>("name"));
        Assert.Equal("n", form.GetValue<string>("note"));
        Assert.Equal(["note"], form.DirtyPaths);
    }

    [Fact]
    public void RestoreDraft_clears_errors_touched_state_and_ignores_unknown_keys_and_out_of_range_steps()
    {
        var form = new FormController(
            new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a", Required = true }] });
        form.Validate();
        form.SetFormError("boom");
        Assert.True(form.HasBlockingErrors);

        form.RestoreDraft(new FormDraft(new Dictionary<string, object?> { ["a"] = "v", ["ghost"] = 1 }, StepIndex: 9));

        Assert.False(form.HasBlockingErrors);
        Assert.Empty(form.Errors);
        Assert.False(form.InternalState.ContainsKey("ghost"));
        Assert.Equal(0, form.CurrentStepIndex);
    }

    [Fact]
    public void RestoreDraft_does_not_call_Watch()
    {
        var calls = 0;
        var form = new FormController(new FormSchema
        {
            FormKey = "x",
            Fields = [new FormFieldSchema { Key = "a", Watch = (_, _) => calls++ }],
        });

        form.RestoreDraft(new FormDraft(new Dictionary<string, object?> { ["a"] = "v" }));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void A_draft_round_trips_through_json_and_values_take_the_type_of_their_field()
    {
        var json = Edited().ExportState().ToJson();
        var draft = FormDraft.FromJson(json);
        var form = new FormController(Schema(), Initial);

        form.RestoreDraft(draft);

        Assert.IsType<double>(form.GetValue("age"));
        Assert.IsType<int>(form.GetValue("rating"));
        Assert.Equal(4, form.GetValue("rating"));
        Assert.IsType<bool>(form.GetValue("agree"));
        Assert.IsType<List<string>>(form.GetValue("tags"));
        var rows = Assert.IsType<List<Dictionary<string, object?>>>(form.GetValue("lines"));
        Assert.Equal("A-1", rows[0]["sku"]);
        Assert.Equal(3.0, rows[0]["qty"]);
        Assert.Equal(0, draft.StepIndex);
        Assert.True(form.IsDirty);
    }

    [Fact]
    public void An_empty_list_in_json_becomes_an_empty_set_of_rows_and_dates_stay_text()
    {
        var schema = new FormSchema
        {
            FormKey = "x",
            Fields =
            [
                new FormFieldSchema { Key = "lines", Kind = FormFieldKind.ArrayList, Fields = [new FormFieldSchema { Key = "sku" }] },
                new FormFieldSchema { Key = "when", Kind = FormFieldKind.Date },
                new FormFieldSchema { Key = "span", Kind = FormFieldKind.DateRange },
            ],
        };
        var form = new FormController(schema);

        form.RestoreDraft(FormDraft.FromJson("""{"version":1,"step":0,"values":{"lines":[],"when":"2026-11-26","span":["2026-01-01","2026-01-31"]}}"""));

        Assert.IsType<List<Dictionary<string, object?>>>(form.GetValue("lines"));
        Assert.Equal("2026-11-26", form.GetValue("when"));
        Assert.Equal(["2026-01-01", "2026-01-31"], form.GetValue<List<string>>("span"));
    }

    [Fact]
    public void FromJson_rejects_text_that_is_not_a_draft()
    {
        Assert.Throws<FormatException>(() => FormDraft.FromJson("not json"));
        Assert.Throws<FormatException>(() => FormDraft.FromJson("""{"step":1}"""));
        Assert.Throws<ArgumentNullException>(() => FormDraft.FromJson(null!));
    }

    [Fact]
    public void ToJson_writes_other_types_as_text_and_non_finite_numbers_as_null()
    {
        var draft = new FormDraft(new Dictionary<string, object?>
        {
            ["day"] = new DateOnly(2026, 11, 26),
            ["bad"] = double.NaN,
            ["n"] = 1.5,
        });

        var back = FormDraft.FromJson(draft.ToJson());

        Assert.Equal("2026-11-26", back.Values["day"]);
        Assert.Null(back.Values["bad"]);
        Assert.Equal(1.5, back.Values["n"]);
    }

    // The guard

    private static async Task Navigate(NavigationManager nav, string uri)
    {
        nav.NavigateTo(uri);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
    }

    private (IRenderedComponent<KlyFormNavigationGuard> Cut, NavigationManager Nav) Guard(FormController form, Action<ComponentParameterCollectionBuilder<KlyFormNavigationGuard>>? configure = null)
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<KlyFormNavigationGuard>(p =>
        {
            p.Add(c => c.Form, form);
            configure?.Invoke(p);
        });
        return (cut, Services.GetRequiredService<NavigationManager>());
    }

    [Fact]
    public async Task Guard_prevents_internal_navigation_while_dirty_when_the_user_declines()
    {
        var form = new FormController(Schema(), Initial);
        var (_, nav) = Guard(form, p => p.Add(c => c.ConfirmLeave, _ => Task.FromResult(false)));
        var start = nav.Uri;
        form.SetValue("name", "Grace");

        await Navigate(nav, "/elsewhere");

        Assert.Equal(start, nav.Uri);
    }

    [Fact]
    public async Task Guard_lets_internal_navigation_through_when_the_user_confirms_or_the_form_is_clean()
    {
        var asked = 0;
        var form = new FormController(Schema(), Initial);
        var (_, nav) = Guard(form, p => p.Add(c => c.ConfirmLeave, _ =>
        {
            asked++;
            return Task.FromResult(true);
        }));

        await Navigate(nav, "/clean");
        Assert.Equal(0, asked);
        Assert.EndsWith("/clean", nav.Uri, StringComparison.Ordinal);

        form.SetValue("name", "Grace");
        await Navigate(nav, "/dirty");
        Assert.Equal(1, asked);
        Assert.EndsWith("/dirty", nav.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Guard_uses_window_confirm_with_the_message_by_default()
    {
        var form = new FormController(Schema(), Initial);
        var (_, nav) = Guard(form, p => p.Add(c => c.Message, "Really leave?"));
        var confirm = JSInterop.Setup<bool>("confirm", "Really leave?");
        confirm.SetResult(false);
        form.SetValue("name", "Grace");
        var start = nav.Uri;

        await Navigate(nav, "/elsewhere");

        Assert.Equal(start, nav.Uri);
        Assert.Single(JSInterop.Invocations, i => i.Identifier == "confirm");
    }

    [Fact]
    public async Task Guard_stops_blocking_once_a_submission_succeeds()
    {
        var form = new FormController(Schema(), Initial);
        var asked = 0;
        var (_, nav) = Guard(form, p => p.Add(c => c.ConfirmLeave, _ =>
        {
            asked++;
            return Task.FromResult(false);
        }));
        form.SetValue("name", "Grace");

        Assert.True(await form.SubmitAsync());
        await Navigate(nav, "/after-save");

        Assert.Equal(0, asked);
        Assert.EndsWith("/after-save", nav.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disabled_guard_never_asks()
    {
        var form = new FormController(Schema(), Initial);
        var (_, nav) = Guard(form, p => p.Add(c => c.Disabled, true).Add(c => c.ConfirmLeave, _ => Task.FromResult(false)));
        form.SetValue("name", "Grace");

        await Navigate(nav, "/free");

        Assert.EndsWith("/free", nav.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Guard_asks_the_browser_to_confirm_external_navigation_only_while_dirty()
    {
        var form = new FormController(Schema(), Initial);
        var (cut, _) = Guard(form);
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier.Contains("enableNavigationPrompt", StringComparison.Ordinal));

        form.SetValue("name", "Grace");
        cut.WaitForAssertion(() => Assert.Contains(JSInterop.Invocations, i => i.Identifier.Contains("enableNavigationPrompt", StringComparison.Ordinal)));

        form.ResetField("name");
        cut.WaitForAssertion(() => Assert.Contains(JSInterop.Invocations, i => i.Identifier.Contains("disableNavigationPrompt", StringComparison.Ordinal)));
    }

    [Fact]
    public void Guard_without_external_confirmation_does_not_enable_the_browser_prompt()
    {
        var form = new FormController(Schema(), Initial);
        _ = Guard(form, p => p.Add(c => c.ConfirmExternalNavigation, false));

        form.SetValue("name", "Grace");

        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier.Contains("enableNavigationPrompt", StringComparison.Ordinal));
    }
}
