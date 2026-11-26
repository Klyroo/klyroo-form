using Bunit;
using Klyroo.Form;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class FieldRenderingTests : BunitContext
{
    public FieldRenderingTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private (IRenderedComponent<KlyDynamicFormEngine> Cut, DynamicFormController Form) Engine(params DynamicFieldSchema[] fields)
    {
        var form = new DynamicFormController(new DynamicFormSchema { Fields = fields });
        return (Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, form)), form);
    }

    private static string Control(DynamicFieldSchema field) => $"[data-form-field={field.Key}] [data-slot=form-control]";

    [Fact]
    public void KlyFormPage_renders_container_children_and_lists_with_typed_controls_and_labels()
    {
        var lines = new DynamicFieldSchema { Key = "lines", Label = "Lines", Kind = FormFieldKind.ArrayList, Fields = [new DynamicFieldSchema { Key = "sku", Label = "SKU" }] };
        var form = new DynamicFormController(new DynamicFormSchema
        {
            Sections =
            [
                new DynamicSectionSchema
                {
                    Key = "main", Label = "Main",
                    Fields =
                    [
                        new DynamicFieldSchema { Key = "box", Kind = FormFieldKind.Card, Label = "Box", Fields = [new DynamicFieldSchema { Key = "inner", Label = "Inner" }] },
                        new DynamicFieldSchema { Key = "agree", Label = "Agree", Kind = FormFieldKind.Checkbox },
                        lines,
                    ],
                },
            ],
        });
        var cut = Render<KlyDynamicFormPage>(p => p.Add(x => x.Form, form));

        var inner = cut.Find("[data-form-field=inner] input");
        Assert.Equal(inner.Id, cut.Find("[data-form-field=inner] label").GetAttribute("for"));
        Assert.Equal("checkbox", cut.Find("[data-form-field=agree] input").GetAttribute("type"));

        cut.Find("[data-slot=form-array-add]").Click();
        cut.Find("[data-slot=form-array-input]").Input("A-1");
        Assert.Equal("A-1", form.GetArrayItems("lines")[0]["sku"]);
    }

    [Fact]
    public async Task KlyFormPage_is_a_form_so_Enter_submits_it()
    {
        IReadOnlyDictionary<string, object?>? submitted = null;
        var form = new DynamicFormController(new DynamicFormSchema
        {
            Sections = [new DynamicSectionSchema { Key = "s", Label = "S", Fields = [new DynamicFieldSchema { Key = "name", DefaultValue = "Acme" }] }],
        });
        var cut = Render<KlyDynamicFormPage>(p => p.Add(x => x.Form, form).Add(x => x.OnSubmit, d => submitted = d));

        Assert.Equal("submit", cut.Find("[data-slot=form-page-submit]").GetAttribute("type"));
        await cut.Find("form[data-slot=form-page]").SubmitAsync();
        Assert.Equal("Acme", submitted?["name"]);
    }

    [Fact]
    public void Repeatable_lists_show_their_error_required_marker_texts_and_disabled_state()
    {
        var lines = new DynamicFieldSchema
        {
            Key = "lines",
            Label = "Lines",
            Kind = FormFieldKind.ArrayList,
            Required = true,
            Disabled = true,
            DirtyCheck = true,
            Description = "The order lines",
            Hint = "Max 10",
            Help = "One per product",
            Fields = [new DynamicFieldSchema { Key = "sku", Label = "SKU" }],
        };
        var (cut, form) = Engine(lines);
        form.AddArrayItem(lines);
        form.RemoveArrayItem("lines", 0);
        form.Validate();
        cut.Render();

        var root = cut.Find("[data-slot=form-array]");
        Assert.Equal("This field is required", root.QuerySelector("[data-slot=form-field-error]")!.TextContent);
        Assert.Contains("*", root.QuerySelector("[data-slot=form-field-label]")!.TextContent);
        Assert.NotNull(root.QuerySelector("[data-slot=form-field-description]"));
        Assert.NotNull(root.QuerySelector("[data-slot=form-field-hint]"));
        Assert.NotNull(root.QuerySelector("[data-slot=form-field-help]"));
        Assert.True(cut.Find("[data-slot=form-array-add]").HasAttribute("disabled"));

        form.AddArrayItem(lines);
        cut.Render();
        Assert.True(cut.Find("[data-slot=form-array-remove]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-slot=form-array-input]").HasAttribute("disabled"));
        Assert.NotNull(cut.Find("[data-form-field=lines] [data-slot=form-field-reset]"));
    }

    [Fact]
    public void Cells_render_their_kind_and_show_their_errors()
    {
        var lines = new DynamicFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayTable,
            Fields = [new DynamicFieldSchema { Key = "qty", Label = "Qty", Kind = FormFieldKind.Number, Required = true }],
        };
        var (cut, form) = Engine(lines);
        form.AddArrayItem(lines);
        cut.Render();

        var input = cut.Find("[data-slot=form-array-input]");
        Assert.Equal("number", input.GetAttribute("type"));
        input.Input("3");
        Assert.Equal(3.0, form.GetArrayItems("lines")[0]["qty"]);

        cut.Find("[data-slot=form-array-input]").Input("");
        cut.Find("[data-slot=form-array-input]").Blur();
        Assert.Equal("This field is required", cut.Find("[data-slot=form-array-cell-error]").TextContent);
    }

    [Fact]
    public void A_Button_field_renders_a_non_submitting_button_that_runs_OnClick()
    {
        var clicks = 0;
        var submits = 0;
        var form = new DynamicFormController(new DynamicFormSchema
        {
            Fields = [new DynamicFieldSchema { Key = "fill", Label = "Fill from profile", Kind = FormFieldKind.Button, OnClick = _ => { clicks++; return Task.CompletedTask; } }],
            OnSubmit = _ => { submits++; return Task.CompletedTask; },
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, form));

        var button = cut.Find("[data-slot=form-button]");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Fill from profile", button.TextContent.Trim());
        button.Click();
        Assert.Equal(1, clicks);
        Assert.Equal(0, submits);
        Assert.Empty(cut.FindAll("input[type=text]"));
    }

    [Fact]
    public void Empty_tabs_and_containers_render_nothing()
    {
        var (cut, _) = Engine(
            new DynamicFieldSchema { Key = "t", Label = "T", Kind = FormFieldKind.Tabs, Tabs = [] },
            new DynamicFieldSchema { Key = "g", Label = "G", Kind = FormFieldKind.Group });

        Assert.Empty(cut.FindAll("input"));
        Assert.Empty(cut.FindAll("[data-slot=form-container]"));
        Assert.DoesNotContain(">T<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Switch_label_shows_once_and_checkbox_hint_and_reset_show()
    {
        var sw = new DynamicFieldSchema { Key = "sw", Label = "Notifications", Kind = FormFieldKind.Switch };
        var cb = new DynamicFieldSchema { Key = "cb", Label = "Agree", Kind = FormFieldKind.Checkbox, Hint = "Required by law", DirtyCheck = true };
        var plain = new DynamicFieldSchema { Key = "plain", Hint = "No label", DirtyCheck = true };
        var (cut, form) = Engine(sw, cb, plain);
        form.SetValue("cb", true);
        form.SetValue("plain", "x");
        cut.Render();

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(cut.Find("[data-form-field=sw]").TextContent, "Notifications"));
        Assert.Equal("switch", cut.Find("[data-form-field=sw] input").GetAttribute("role"));
        Assert.Equal("Required by law", cut.Find("[data-form-field=cb] [data-slot=form-field-hint]").TextContent);
        Assert.NotNull(cut.Find("[data-form-field=cb] [data-slot=form-field-reset]"));
        Assert.NotNull(cut.Find("[data-form-field=plain] [data-slot=form-field-hint]"));
        Assert.NotNull(cut.Find("[data-form-field=plain] [data-slot=form-field-reset]"));
    }

    [Fact]
    public void A_radio_group_has_an_id_and_is_labelled_by_an_existing_element()
    {
        var radio = new DynamicFieldSchema { Key = "plan", Label = "Plan", Kind = FormFieldKind.Radio, Options = [new("a", "A"), new("b", "B")] };
        var (cut, _) = Engine(radio);

        var group = cut.Find("[role=radiogroup]");
        Assert.False(string.IsNullOrEmpty(group.Id));
        var labelledBy = group.GetAttribute("aria-labelledby");
        Assert.Equal("Plan", cut.Find($"#{labelledBy}").TextContent.Trim());
        Assert.Empty(cut.FindAll($"label[for={group.Id}]"));
    }

    [Fact]
    public void Textarea_and_password_have_maxlength()
    {
        var (cut, _) = Engine(
            new DynamicFieldSchema { Key = "bio", Kind = FormFieldKind.Textarea, MaxLength = 200 },
            new DynamicFieldSchema { Key = "pw", Kind = FormFieldKind.Password, MaxLength = 64 });

        Assert.Equal("200", cut.Find("textarea").GetAttribute("maxlength"));
        Assert.Equal("64", cut.Find("input[type=password]").GetAttribute("maxlength"));
    }

    [Fact]
    public void A_null_slider_shows_its_minimum_and_stays_null_until_moved()
    {
        var slider = new DynamicFieldSchema { Key = "vol", Kind = FormFieldKind.Slider, Min = 10, Max = 90 };
        var (cut, form) = Engine(slider);

        Assert.Equal("10", cut.Find("input[type=range]").GetAttribute("value"));
        Assert.Null(form.GetValue("vol"));

        cut.Find("input[type=range]").Input("40");
        Assert.Equal(40.0, form.GetValue("vol"));
        Assert.Equal("40", cut.Find("input[type=range]").GetAttribute("value"));
    }

    [Fact]
    public void Inputs_with_an_error_are_marked_invalid_and_described_by_the_error()
    {
        var (cut, form) = Engine(
            new DynamicFieldSchema { Key = "name", Label = "Name", Required = true },
            new DynamicFieldSchema { Key = "kind", Label = "Kind", Kind = FormFieldKind.Select, Required = true, Options = [new("a", "A")] },
            new DynamicFieldSchema { Key = "plan", Label = "Plan", Kind = FormFieldKind.Radio, Required = true, Options = [new("a", "A")] });
        Assert.Null(cut.Find("[id$='-name']").GetAttribute("aria-invalid"));

        form.Validate();
        cut.Render();

        foreach (var key in new[] { "name", "kind", "plan" })
        {
            var control = cut.Find($"[id$='-{key}']");
            Assert.Equal("true", control.GetAttribute("aria-invalid"));
            var describedBy = control.GetAttribute("aria-describedby")!.Split(' ');
            var error = cut.Find($"[data-form-field={key}] [data-slot=form-field-error]");
            Assert.Contains(error.Id, describedBy);
        }
    }

    [Fact]
    public void CheckboxGroup_writes_a_list_of_values()
    {
        var field = new DynamicFieldSchema { Key = "days", Label = "Days", Kind = FormFieldKind.CheckboxGroup, Options = [new("mon", "Monday"), new("tue", "Tuesday")] };
        var (cut, form) = Engine(field);

        cut.FindAll("[data-form-field=days] input[type=checkbox]")[1].Change(true);
        cut.FindAll("[data-form-field=days] input[type=checkbox]")[0].Change(true);
        Assert.Equal(["tue", "mon"], form.GetValue<IReadOnlyList<string>>("days"));

        cut.FindAll("[data-form-field=days] input[type=checkbox]")[1].Change(false);
        Assert.Equal(["mon"], form.GetValue<IReadOnlyList<string>>("days"));
    }

    [Theory]
    [InlineData(FormFieldKind.DateTime, "datetime-local", null, null)]
    [InlineData(FormFieldKind.PhoneNumber, "tel", "tel", "tel")]
    [InlineData(FormFieldKind.ColorPicker, "color", null, null)]
    [InlineData(FormFieldKind.OneTimeCode, "text", "one-time-code", "numeric")]
    public void Text_like_kinds_render_their_input_type(FormFieldKind kind, string type, string? autocomplete, string? inputMode)
    {
        var (cut, _) = Engine(new DynamicFieldSchema { Key = "f", Kind = kind, MaxLength = 6 });

        var input = cut.Find("[id$='-f']");
        Assert.Equal(type, input.GetAttribute("type"));
        Assert.Equal(autocomplete, input.GetAttribute("autocomplete"));
        Assert.Equal(inputMode, input.GetAttribute("inputmode"));
        if (kind == FormFieldKind.OneTimeCode) Assert.Equal("6", input.GetAttribute("maxlength"));
    }

    [Fact]
    public void DateRange_renders_two_dates_and_writes_a_two_item_list()
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "stay", Label = "Stay", Kind = FormFieldKind.DateRange });

        Assert.Equal(2, cut.FindAll("[data-form-field=stay] input[type=date]").Count);
        cut.Find("[data-slot=form-date-range-start]").Input("2026-05-01");
        cut.Find("[data-slot=form-date-range-end]").Input("2026-05-04");
        Assert.Equal(["2026-05-01", "2026-05-04"], form.GetValue<IReadOnlyList<string>>("stay"));

        cut.Find("[data-slot=form-date-range-start]").Input("");
        cut.Find("[data-slot=form-date-range-end]").Input("");
        Assert.Null(form.GetValue("stay"));
    }

    [Fact]
    public void Rating_renders_accessible_radio_stars_and_stores_an_int()
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "score", Label = "Score", Kind = FormFieldKind.Rating, Max = 4 });

        var stars = cut.FindAll("[data-form-field=score] input[type=radio]");
        Assert.Equal(4, stars.Count);
        Assert.Equal("3 of 4", stars[2].GetAttribute("aria-label"));
        stars[2].Change("3");
        Assert.Equal(3, form.GetValue("score"));
        Assert.Equal(3, cut.FindAll("[data-slot=form-rating-star][data-active=true]").Count);
    }

    [Fact]
    public void Tag_adds_on_Enter_and_comma_and_removes_with_its_button()
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "tags", Label = "Tags", Kind = FormFieldKind.Tag });

        var input = cut.Find("[id$='-tags']");
        Assert.Equal("kly-form-detached", input.GetAttribute("form"));
        input.Input("blazor");
        cut.Find("[id$='-tags']").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.Find("[id$='-tags']").Input("dotnet, ui");
        Assert.Equal(["blazor", "dotnet"], form.GetValue<IReadOnlyList<string>>("tags"));
        Assert.Equal(" ui", cut.Find("[id$='-tags']").GetAttribute("value"));

        cut.FindAll("[data-slot=form-tag-remove]")[0].Click();
        Assert.Equal(["dotnet"], form.GetValue<IReadOnlyList<string>>("tags"));
    }

    [Fact]
    public void Multiple_select_renders_checkboxes_writing_a_list()
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "langs", Label = "Languages", Kind = FormFieldKind.Select, Multiple = true, Options = [new("cs", "C#"), new("fs", "F#")] });

        Assert.Empty(cut.FindAll("select"));
        cut.FindAll("[data-form-field=langs] input[type=checkbox]")[0].Change(true);
        Assert.Equal(["cs"], form.GetValue<IReadOnlyList<string>>("langs"));
    }

    [Fact]
    public void OnCreateOption_renders_a_create_entry_that_adds_and_selects_the_option()
    {
        var field = new DynamicFieldSchema { Key = "tag", Label = "Tag", Kind = FormFieldKind.Select, Options = [new("a", "A")], OnCreateOption = t => new FormOptionItem(t.ToLowerInvariant(), t) };
        var (cut, form) = Engine(field);

        cut.Find("[data-slot=form-create-option-input]").Input("Beta");
        cut.Find("[data-slot=form-create-option-button]").Click();

        Assert.Equal("beta", form.GetValue("tag"));
        Assert.Contains(cut.FindAll("select option"), o => o.GetAttribute("value") == "beta");
    }

    [Fact]
    public void AutoComplete_is_a_text_input_with_suggestions_that_stores_the_option_value()
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "city", Label = "City", Kind = FormFieldKind.AutoComplete, Options = [new("par", "Paris"), new("lyo", "Lyon")] });

        var input = cut.Find("[id$='-city']");
        Assert.Equal("text", input.GetAttribute("type"));
        var list = cut.Find($"datalist#{input.GetAttribute("list")}");
        Assert.Equal(2, list.QuerySelectorAll("option").Length);

        input.Input("paris");
        Assert.Equal("par", form.GetValue("city"));
        cut.Find("[id$='-city']").Input("Nantes");
        Assert.Equal("Nantes", form.GetValue("city"));
    }

    [Theory]
    [InlineData(FormFieldKind.Textarea, "text")]
    [InlineData(FormFieldKind.Password, "secret")]
    [InlineData(FormFieldKind.Number, 4.0)]
    public void Textarea_password_and_number_are_clearable(FormFieldKind kind, object value)
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "f", Kind = kind, Clearable = true, DefaultValue = value });

        cut.Find("[data-slot=form-control-clear]").Click();
        Assert.True(DynamicFormController.IsEmptyValue(form.GetValue("f")));
    }

    [Fact]
    public void Option_groups_descriptions_and_icons_are_rendered()
    {
        FormOptionItem[] options =
        [
            new("eu-par", "Paris", Description: "Capital", Group: "Europe", Icon: "star"),
            new("us-nyc", "New York", Group: "America"),
            new("eu-ber", "Berlin", Group: "Europe"),
        ];
        var (cut, _) = Engine(
            new DynamicFieldSchema { Key = "sel", Kind = FormFieldKind.Select, Options = options },
            new DynamicFieldSchema { Key = "rad", Kind = FormFieldKind.Radio, Options = options });

        var optgroups = cut.FindAll("[data-form-field=sel] optgroup");
        Assert.Equal(["Europe", "America"], optgroups.Select(g => g.GetAttribute("label")));
        Assert.Equal(2, optgroups[0].QuerySelectorAll("option").Length);

        Assert.Equal(["Europe", "America"], cut.FindAll("[data-form-field=rad] [data-slot=form-option-group-label]").Select(l => l.TextContent));
        var paris = cut.Find("[data-form-field=rad] [data-slot=form-option][data-value=eu-par]");
        Assert.Equal("Capital", paris.QuerySelector("[data-slot=form-option-description]")!.TextContent);
        Assert.NotNull(paris.QuerySelector("svg[data-icon=star]"));
    }

    [Fact]
    public void Collapsible_containers_render_a_disclosure()
    {
        var (cut, _) = Engine(
            new DynamicFieldSchema { Key = "open", Label = "Open", Kind = FormFieldKind.Group, Collapsible = true, Fields = [new DynamicFieldSchema { Key = "a" }] },
            new DynamicFieldSchema { Key = "closed", Label = "Closed", Kind = FormFieldKind.Card, Collapsible = true, DefaultCollapsed = true, Fields = [new DynamicFieldSchema { Key = "b" }] });

        var details = cut.FindAll("details[data-slot=form-container]");
        Assert.Equal(2, details.Count);
        Assert.True(details[0].HasAttribute("open"));
        Assert.False(details[1].HasAttribute("open"));
        Assert.Equal("Closed", details[1].QuerySelector("summary")!.TextContent);
        Assert.NotNull(details[1].QuerySelector("[data-form-field=b]"));
    }

    [Fact]
    public void Info_blocks_apply_DisplayTone_and_DisplayKind()
    {
        var (cut, _) = Engine(
            new DynamicFieldSchema { Key = "warn", Kind = FormFieldKind.Info, DisplayText = "Careful", DisplayTone = "Warning" },
            new DynamicFieldSchema { Key = "plain", Kind = FormFieldKind.Info, DisplayText = "Note", DisplayKind = "text" });

        var warn = cut.Find("[data-form-field=warn]");
        Assert.Equal("warning", warn.GetAttribute("data-tone"));
        Assert.Contains("text-(--klyroo-warning-text)", warn.InnerHtml, StringComparison.Ordinal);
        var plain = cut.Find("[data-form-field=plain]");
        Assert.Equal("text", plain.GetAttribute("data-display-kind"));
        Assert.DoesNotContain("border", plain.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void The_current_step_description_is_shown_and_submit_appears_on_the_last_reachable_step()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            Steps =
            [
                new DynamicStepSchema { Key = "one", Title = "One", Description = "Tell us about you", Fields = [new DynamicFieldSchema { Key = "a" }] },
                new DynamicStepSchema { Key = "two", Title = "Two", SkipWhen = _ => true },
            ],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, form));

        Assert.Equal("Tell us about you", cut.Find("[data-slot=form-step-description]").TextContent);
        Assert.NotNull(cut.Find("[data-slot=form-action-submit]"));
        Assert.Empty(cut.FindAll("[data-slot=form-action-next]"));
    }

    [Fact]
    public void Columns_and_section_span_set_the_grids()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            Columns = 2,
            Sections =
            [
                new DynamicSectionSchema { Key = "a", Label = "A", Span = 6, Fields = [new DynamicFieldSchema { Key = "x", Span = 1 }, new DynamicFieldSchema { Key = "y" }] },
            ],
        });
        var cut = Render<KlyDynamicFormPage>(p => p.Add(x => x.Form, form));

        Assert.Contains("repeat(2,", cut.Find("[data-slot=form-page-fields]").GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("span 1 / span 1", cut.Find("[data-form-field=x]").GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("span 2 / span 2", cut.Find("[data-form-field=y]").GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("span 6 / span 6", cut.Find("section[data-key=a]").GetAttribute("style"), StringComparison.Ordinal);
    }

    [Fact]
    public void Password_requirements_render_a_live_checklist()
    {
        var (cut, _) = Engine(new DynamicFieldSchema
        {
            Key = "pw",
            Kind = FormFieldKind.Password,
            PasswordRequirements = [new("len", "8 characters", p => p.Length >= 8), new("digit", "A digit", p => p.Any(char.IsDigit))],
        });

        cut.Find("input[type=password]").Input("abc1");
        var items = cut.FindAll("[data-slot=form-password-requirement]");
        Assert.Equal(["false", "true"], items.Select(i => i.GetAttribute("data-met")));
        Assert.Contains(cut.Find("[data-slot=form-password-requirements]").Id!, cut.Find("input[type=password]").GetAttribute("aria-describedby"), StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_hosts_the_drawer_in_a_slideover_dialog()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<DynamicFormService>();
        _ = service.CreateFormAsync(new DynamicFormSchema { Title = "Edit", Fields = [new DynamicFieldSchema { Key = "a" }] }, mode: FormShellMode.Drawer);
        provider.Render();

        Assert.Equal("drawer", provider.Find("[data-slot=form-provider-overlay]").GetAttribute("data-mode"));
        var dialog = provider.Find("dialog[data-form-dialog]");
        Assert.Equal("right", dialog.GetAttribute("data-side"));
        Assert.Equal("Edit", dialog.GetAttribute("aria-label"));
        Assert.NotNull(dialog.QuerySelector("[data-slot=form-engine]"));
    }

    [Fact]
    public void Provider_hosts_fullscreen_in_a_modal_that_fills_the_viewport()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<DynamicFormService>();
        _ = service.CreateFormAsync(new DynamicFormSchema { Fields = [new DynamicFieldSchema { Key = "a" }] }, mode: FormShellMode.Fullscreen);
        provider.Render();

        var dialog = provider.Find("dialog[data-form-dialog]");
        Assert.Contains("h-dvh", dialog.ClassName, StringComparison.Ordinal);
        Assert.Contains("w-screen", dialog.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("max-w-2xl", dialog.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void The_selected_tab_survives_a_sibling_appearing_or_disappearing()
    {
        var (cut, form) = Engine(
            new DynamicFieldSchema { Key = "show", Kind = FormFieldKind.Checkbox, DefaultValue = true },
            new DynamicFieldSchema { Key = "maybe", Condition = f => f.GetValue("show") is true },
            new DynamicFieldSchema
            {
                Key = "tabs",
                Kind = FormFieldKind.Tabs,
                Tabs =
                [
                    new DynamicTabSchema { Key = "a", Label = "A", Fields = [new DynamicFieldSchema { Key = "fa" }] },
                    new DynamicTabSchema { Key = "b", Label = "B", Fields = [new DynamicFieldSchema { Key = "fb" }] },
                ],
            });

        cut.Find("[data-slot=form-tab-trigger][data-tab=b]").Click();
        form.SetValue("show", false);
        cut.Render();

        Assert.Equal("true", cut.Find("[data-slot=form-tab-trigger][data-tab=b]").GetAttribute("aria-selected"));
        Assert.NotNull(cut.Find("[data-form-field=fb]"));
    }

    [Fact]
    public void AutoComplete_keeps_the_typed_text_while_the_user_types()
    {
        var (cut, form) = Engine(new DynamicFieldSchema { Key = "country", Kind = FormFieldKind.AutoComplete, Options = [new("DE", "Germany"), new("DK", "Denmark")] });

        cut.Find("[id$='-country']").Input("De");
        Assert.Equal("De", cut.Find("[id$='-country']").GetAttribute("value"));
        cut.Find("[id$='-country']").Input("denmark");
        Assert.Equal("DK", form.GetValue("country"));

        cut.Find("[id$='-country']").Blur();
        Assert.Equal("Denmark", cut.Find("[id$='-country']").GetAttribute("value"));
    }

    [Fact]
    public void Rating_renders_every_star_up_to_Max()
    {
        var (cut, _) = Engine(new DynamicFieldSchema { Key = "score", Kind = FormFieldKind.Rating, Max = 12 });

        Assert.Equal(12, cut.FindAll("[data-form-field=score] input[type=radio]").Count);
    }

    [Fact]
    public async Task The_stepper_keeps_the_current_step_and_Enter_before_the_last_step_moves_next()
    {
        var submitted = false;
        var form = new DynamicFormController(new DynamicFormSchema
        {
            Steps =
            [
                new DynamicStepSchema { Key = "s1", Title = "One", SkipWhen = f => f.GetValue<string>("x") == "skip", Fields = [new DynamicFieldSchema { Key = "x" }] },
                new DynamicStepSchema { Key = "s2", Title = "Two", Fields = [new DynamicFieldSchema { Key = "y", Required = true }] },
            ],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, form).Add(x => x.OnSubmit, _ => submitted = true));

        form.SetValue("x", "skip");
        cut.Render();
        Assert.NotNull(cut.Find("[data-slot=form-step-badge][data-step=s1]"));

        form.SetValue("x", "");
        await cut.Find("form").SubmitAsync();
        Assert.Equal("s2", form.CurrentStep?.Key);
        Assert.False(submitted);
    }
}
