using System.Text;
using Bunit;
using Klyroo.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class ExtensibilityTests : BunitContext
{
    public ExtensibilityTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static FormController Form(params FormFieldSchema[] fields) => new(new FormSchema { FormKey = "ext", Fields = fields });

    // A custom control: an input wired to the field through its context.
    private static RenderFragment<FormControlContext> TextBox(string marker = "custom") => ctx => builder =>
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "id", ctx.Id);
        builder.AddAttribute(2, "data-custom", marker);
        builder.AddAttribute(3, "value", ctx.Value?.ToString());
        builder.AddAttribute(4, "aria-describedby", ctx.DescribedBy);
        builder.AddAttribute(5, "aria-invalid", ctx.Error is null ? null : "true");
        builder.AddAttribute(6, "disabled", ctx.Disabled);
        builder.AddAttribute(7, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(ctx, e => ctx.Set(e.Value)));
        builder.AddAttribute(8, "onblur", EventCallback.Factory.Create(ctx, ctx.Blur));
        builder.CloseElement();
    };

    // FormControlContext

    [Fact]
    public void FormControlContext_edits_the_field_through_the_controller_and_describes_its_ids()
    {
        var field = new FormFieldSchema { Key = "name", Label = "Name", Description = "d", Help = "h", Required = true };
        var form = Form(field);
        var ctx = new FormControlContext(form, field, "kly-name");

        Assert.Same(form, ctx.Form);
        Assert.Same(field, ctx.Field);
        Assert.Equal("kly-name", ctx.Id);
        Assert.True(ctx.Required);
        Assert.False(ctx.Disabled);
        Assert.False(ctx.Validating);
        Assert.False(ctx.IsCell);
        Assert.Null(ctx.Error);
        Assert.Equal("kly-name-description kly-name-help", ctx.DescribedBy);
        Assert.Equal("kly-name-error", ctx.ErrorId);

        ctx.Set("Ada");
        Assert.Equal("Ada", ctx.Current());
        Assert.Equal("Ada", ctx.Current<string>());
        Assert.Null(ctx.Value);
        Assert.Empty(form.Errors);

        ctx.Commit("");
        Assert.Equal("This field is required", form.GetError("name"));
        var withError = new FormControlContext(form, field, "kly-name");
        Assert.Equal("This field is required", withError.Error);
        Assert.Equal("kly-name-description kly-name-help kly-name-error", withError.DescribedBy);

        form.SetValue("name", "x");
        form.ResetField("name");
        ctx.Blur();
        Assert.NotNull(form.GetError("name"));
    }

    [Fact]
    public void FormControlContext_reports_disabled_and_validating_and_converts_the_value()
    {
        var field = new FormFieldSchema
        {
            Key = "n",
            Kind = FormFieldKind.Number,
            DisabledWhen = f => f.GetValue<bool>("off"),
            AsyncValidators = [new FormAsyncValidatorRule("slow", async (_, _, ct) => { await Task.Delay(Timeout.Infinite, ct); return null; }) { Debounce = TimeSpan.Zero }],
        };
        var form = Form(field, new FormFieldSchema { Key = "off", Kind = FormFieldKind.Switch });
        form.SetValue("off", true);
        form.SetValue("n", 4.0, markTouched: true);

        var ctx = new FormControlContext(form, field, "id");

        Assert.True(ctx.Disabled);
        Assert.True(ctx.Validating);
        Assert.Equal(4, ctx.Current<int>());
        Assert.Throws<ArgumentNullException>(() => new FormControlContext(null!, field, "id"));
    }

    // Custom

    [Fact]
    public void Custom_fields_are_stateful_validated_and_part_of_the_output()
    {
        var form = Form(new FormFieldSchema
        {
            Key = "pick",
            Kind = FormFieldKind.Custom,
            CustomKey = "x",
            Required = true,
            Validators = [new FormValidatorRule("no-bad", "Bad pick", (v, _) => !Equals(v, "bad"))],
        });

        Assert.False(form.Validate());
        Assert.Equal("This field is required", form.GetError("pick"));
        form.SetValue("pick", "bad");
        Assert.Equal("Bad pick", form.GetError("pick"));
        form.SetValue("pick", new List<string> { "a" });
        Assert.True(form.Validate());
        Assert.Equal(["a"], Assert.IsType<List<string>>(form.BuildOutput()["pick"]));
        Assert.Contains("pick", form.ActiveFields.Select(f => f.Key));
    }

    [Fact]
    public void A_custom_renderer_registered_in_the_options_draws_the_control_with_the_label_and_error_around_it()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f.CustomField("textbox", TextBox("registered")));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var form = Form(new FormFieldSchema { Key = "nick", Kind = FormFieldKind.Custom, CustomKey = "textbox", Label = "Nickname", Help = "Shown to others", Required = true });
        var cut = ctx.Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        var input = cut.Find("[data-custom=registered]");
        Assert.Equal("kly-ff-nick", input.Id);
        Assert.Equal("kly-ff-nick", cut.Find("label[data-slot=form-field-label]").GetAttribute("for"));
        Assert.Equal("kly-ff-nick-help", input.GetAttribute("aria-describedby"));

        input.Input("Ada");
        input.Blur();
        Assert.Equal("Ada", form.GetValue("nick"));

        cut.Find("[data-custom=registered]").Input("");
        cut.Find("[data-custom=registered]").Blur();
        var updated = cut.Find("[data-custom=registered]");
        Assert.Equal("true", updated.GetAttribute("aria-invalid"));
        Assert.Equal("kly-ff-nick-help kly-ff-nick-error", updated.GetAttribute("aria-describedby"));
        Assert.Contains("This field is required", cut.Find("#kly-ff-nick-error").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void The_FieldTemplates_parameter_wins_over_the_registry_and_reaches_nested_fields()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f.CustomField("textbox", TextBox("registered")));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var form = Form(
            new FormFieldSchema { Key = "top", Kind = FormFieldKind.Custom, CustomKey = "textbox" },
            new FormFieldSchema
            {
                Key = "box",
                Kind = FormFieldKind.Card,
                Fields = [new FormFieldSchema { Key = "nested", Kind = FormFieldKind.Custom, CustomKey = "textbox" }],
            });
        var templates = new Dictionary<string, RenderFragment<FormControlContext>> { ["textbox"] = TextBox("param") };

        var cut = ctx.Render<KlyFormEngine>(p => p.Add(c => c.Form, form).Add(c => c.FieldTemplates, templates));

        Assert.Equal(2, cut.FindAll("[data-custom=param]").Count);
        Assert.Empty(cut.FindAll("[data-custom=registered]"));
    }

    [Fact]
    public void Custom_fields_render_in_tabs_pages_and_cells_of_lists()
    {
        var templates = new Dictionary<string, RenderFragment<FormControlContext>> { ["textbox"] = TextBox("param") };
        var lines = new FormFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayList,
            Fields = [new FormFieldSchema { Key = "note", Kind = FormFieldKind.Custom, CustomKey = "textbox" }],
        };
        var schema = new FormSchema
        {
            FormKey = "p",
            Sections =
            [
                new FormPageSectionSchema
                {
                    Key = "s",
                    Label = "S",
                    Fields =
                    [
                        new FormFieldSchema
                        {
                            Key = "tabs",
                            Kind = FormFieldKind.Tabs,
                            Tabs = [new FormTabItemSchema { Key = "t", Label = "T", Fields = [new FormFieldSchema { Key = "intab", Kind = FormFieldKind.Custom, CustomKey = "textbox" }] }],
                        },
                        lines,
                    ],
                },
            ],
        };
        var form = new FormController(schema);
        form.AddArrayItem(lines);

        var cut = Render<KlyFormPage>(p => p.Add(c => c.Form, form).Add(c => c.FieldTemplates, templates));

        Assert.Equal(2, cut.FindAll("[data-custom=param]").Count);
        var cell = cut.Find("[data-slot=form-array-cell] [data-custom=param]");
        cell.Input("hello");
        Assert.Equal("hello", form.GetArrayItems("lines")[0]["note"]);
    }

    [Fact]
    public void A_custom_field_without_a_renderer_shows_a_notice_instead_of_failing()
    {
        var form = Form(
            new FormFieldSchema { Key = "a", Kind = FormFieldKind.Custom, CustomKey = "nope" },
            new FormFieldSchema { Key = "b", Kind = FormFieldKind.Custom });

        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        var notices = cut.FindAll("[data-slot=form-custom-missing]");
        Assert.Equal(2, notices.Count);
        Assert.Contains("\"nope\"", notices[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("\"b\"", notices[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Registering_a_custom_field_validates_its_arguments()
    {
        var options = new KlyrooFormOptions();
        Assert.Throws<ArgumentException>(() => options.CustomField(" ", TextBox()));
        Assert.Throws<ArgumentNullException>(() => options.CustomField("x", null!));
        Assert.Same(options, options.CustomField("x", TextBox()));
    }

    [Fact]
    public void A_custom_value_survives_a_draft_as_it_was_stored()
    {
        var form = Form(new FormFieldSchema { Key = "pick", Kind = FormFieldKind.Custom, CustomKey = "x" });
        form.SetValue("pick", new List<string> { "a", "b" });

        var draft = FormDraft.FromJson(form.ExportState().ToJson());
        var other = Form(new FormFieldSchema { Key = "pick", Kind = FormFieldKind.Custom, CustomKey = "x" });
        other.RestoreDraft(draft);

        Assert.Equal(["a", "b"], other.GetValue<List<string>>("pick"));
    }

    // File values

    [Fact]
    public async Task FormFileValue_opens_its_content_and_describes_it()
    {
        var file = FormFileValue.FromBytes("a.txt", Encoding.UTF8.GetBytes("hello"), "text/plain");

        Assert.Equal("a.txt", file.Name);
        Assert.Equal(5, file.Size);
        Assert.Equal("text/plain", file.ContentType);
        Assert.Null(file.LastModified);
        await using var stream = await file.OpenReadAsync(Xunit.TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        Assert.Equal("hello", await reader.ReadToEndAsync(Xunit.TestContext.Current.CancellationToken));
        await using var again = await file.OpenReadAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(5, again.Length);
        Assert.Equal("a.txt (5 B)", file.ToString());
    }

    [Fact]
    public void FormFileValue_validates_its_arguments_and_compares_by_description()
    {
        Assert.Throws<ArgumentNullException>(() => new FormFileValue(null!, 1, "", _ => ValueTask.FromResult<Stream>(Stream.Null)));
        Assert.Throws<ArgumentNullException>(() => new FormFileValue("a", 1, "", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormFileValue("a", -1, "", _ => ValueTask.FromResult<Stream>(Stream.Null)));

        var a = FormFileValue.FromBytes("a.txt", new byte[3], "text/plain");
        var b = FormFileValue.FromBytes("a.txt", new byte[3], "text/plain");
        var c = FormFileValue.FromBytes("a.txt", new byte[4], "text/plain");
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.False(a.Equals((object?)null));
        Assert.False(a.Equals("a.txt"));
        Assert.Equal("", new FormFileValue("n", 1, null!, _ => ValueTask.FromResult<Stream>(Stream.Null)).ContentType);
    }

    [Theory]
    [InlineData(null, "a.bin", "", true)]
    [InlineData("  ", "a.bin", "", true)]
    [InlineData(".png,.jpg", "Photo.PNG", "", true)]
    [InlineData(".png, .jpg", "photo.jpg", "image/jpeg", true)]
    [InlineData(".png,.jpg", "photo.gif", "image/gif", false)]
    [InlineData("image/*", "x", "image/webp", true)]
    [InlineData("image/*", "x.png", "application/pdf", false)]
    [InlineData("image/*", "x.png", "", false)]
    [InlineData("application/pdf", "x", "Application/PDF", true)]
    [InlineData("application/pdf,.txt", "x.txt", "", true)]
    [InlineData(",,", "x", "", false)]
    public void Accept_lists_follow_the_html_attribute_syntax(string? accept, string name, string contentType, bool expected)
    {
        Assert.Equal(expected, FormFileValue.FromBytes(name, [], contentType).Matches(accept));
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5L * 1024 * 1024, "5 MB")]
    [InlineData(3L * 1024 * 1024 * 1024 / 2, "1.5 GB")]
    [InlineData(2L * 1024 * 1024 * 1024 * 1024, "2 TB")]
    [InlineData(4096L * 1024 * 1024 * 1024 * 1024, "4096 TB")]
    public void FormatSize_uses_binary_units_in_the_invariant_culture(long bytes, string expected)
    {
        Assert.Equal(expected, FormFileValue.FormatSize(bytes));
    }

    // File validation

    private static FormFieldSchema FileField(Action<FormFieldSchema>? _ = null, bool multiple = false, string? accept = null, long? max = null, bool required = false) => new()
    {
        Key = "doc",
        Kind = FormFieldKind.File,
        Multiple = multiple,
        Accept = accept,
        MaxFileSize = max,
        Required = required,
    };

    [Fact]
    public void A_required_file_field_needs_a_file()
    {
        var form = Form(FileField(required: true));
        Assert.False(form.Validate());
        Assert.Equal("This field is required", form.GetError("doc"));

        form.SetValue("doc", FormFileValue.FromBytes("a.txt", [1]));
        Assert.True(form.Validate());
        form.SetValue("doc", new List<FormFileValue>());
        Assert.False(form.Validate());
    }

    [Fact]
    public void MaxFileSize_is_checked_for_every_file_with_a_message_that_names_the_file_and_the_limit()
    {
        var form = Form(FileField(multiple: true, max: 2048));
        var small = FormFileValue.FromBytes("small.bin", new byte[100]);
        var large = FormFileValue.FromBytes("large.bin", new byte[4096]);

        form.SetValue("doc", new List<FormFileValue> { small, large });
        Assert.False(form.Validate());
        Assert.Equal("large.bin is larger than 2 KB", form.GetError("doc"));

        form.SetValue("doc", new List<FormFileValue> { small });
        Assert.True(form.Validate());
        form.SetValue("doc", FormFileValue.FromBytes("edge.bin", new byte[2048]));
        Assert.True(form.Validate());
    }

    [Fact]
    public void Accept_is_checked_on_validation_not_only_by_the_picker()
    {
        var form = Form(FileField(accept: ".png,image/jpeg"));

        form.SetValue("doc", FormFileValue.FromBytes("evil.exe", [1], "application/x-msdownload"));
        Assert.False(form.Validate());
        Assert.Equal("evil.exe is not an accepted type (.png,image/jpeg)", form.GetError("doc"));

        form.SetValue("doc", FormFileValue.FromBytes("photo.jpg", [1], "image/jpeg"));
        Assert.True(form.Validate());
    }

    [Fact]
    public void A_single_file_field_rejects_several_files_and_a_multiple_one_accepts_them()
    {
        var one = Form(FileField());
        one.SetValue("doc", new List<FormFileValue> { FormFileValue.FromBytes("a", [1]), FormFileValue.FromBytes("b", [1]) });
        Assert.False(one.Validate());
        Assert.Equal("Choose one file only", one.GetError("doc"));

        var many = Form(FileField(multiple: true));
        many.SetValue("doc", new List<FormFileValue> { FormFileValue.FromBytes("a", [1]), FormFileValue.FromBytes("b", [1]) });
        Assert.True(many.Validate());
    }

    [Fact]
    public void File_errors_are_localized_and_validators_still_run_after_them()
    {
        var form = Form(FileField(max: 10));
        form.Messages = FormMessageLocales.Fr;
        form.SetValue("doc", FormFileValue.FromBytes("gros.bin", new byte[2048]));

        Assert.False(form.Validate());
        Assert.Equal("gros.bin dépasse 10 B", form.GetError("doc"));

        var withRule = new FormFieldSchema
        {
            Key = "doc",
            Kind = FormFieldKind.File,
            Validators = [new FormValidatorRule("named", "Name it report", (v, _) => v is FormFileValue { Name: "report.pdf" })],
        };
        var form2 = Form(withRule);
        form2.SetValue("doc", FormFileValue.FromBytes("other.pdf", [1]));
        Assert.False(form2.Validate());
        Assert.Equal("Name it report", form2.GetError("doc"));
    }

    [Fact]
    public void Files_are_tracked_for_dirtiness_copied_into_the_output_and_never_exported_in_a_draft()
    {
        var form = Form(FileField(multiple: true), new FormFieldSchema { Key = "name", DefaultValue = "n" });
        var a = FormFileValue.FromBytes("a.txt", [1, 2]);
        form.SetValue("doc", new List<FormFileValue> { a });
        Assert.Contains("doc", form.DirtyPaths);

        var output = (List<FormFileValue>)form.BuildOutput()["doc"]!;
        output.Clear();
        Assert.Single((List<FormFileValue>)form.GetValue("doc")!);

        form.SetValue("doc", new List<FormFileValue> { FormFileValue.FromBytes("a.txt", [1, 2]) });
        form.SetValue("doc", new List<FormFileValue> { FormFileValue.FromBytes("a.txt", [9, 9]) });
        Assert.Contains("doc", form.DirtyPaths);

        var draft = form.ExportState(includeSensitive: true);
        Assert.False(draft.Values.ContainsKey("doc"));
        Assert.True(draft.Values.ContainsKey("name"));
    }

    [Fact]
    public async Task Submitting_retakes_the_baseline_for_files()
    {
        var form = Form(FileField());
        form.SetValue("doc", FormFileValue.FromBytes("a.txt", [1]));

        Assert.True(await form.SubmitAsync());

        Assert.False(form.IsDirty);
        Assert.Equal("a.txt", form.GetValue<FormFileValue>("doc")!.Name);
    }

    // File control

    [Fact]
    public void The_file_control_is_an_InputFile_with_accept_and_multiple_and_the_localized_button()
    {
        var form = Form(
            new FormFieldSchema { Key = "one", Kind = FormFieldKind.File, Label = "CV", Accept = ".pdf", Required = true, Help = "PDF only" },
            new FormFieldSchema { Key = "many", Kind = FormFieldKind.File, Label = "Photos", Multiple = true, Accept = "image/*" });

        var cut = Render<KlyFormEngine>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, form));

        var one = cut.Find("input#kly-ff-one");
        Assert.Equal("file", one.GetAttribute("type"));
        Assert.Equal(".pdf", one.GetAttribute("accept"));
        Assert.False(one.HasAttribute("multiple"));
        Assert.Equal("true", one.GetAttribute("aria-required"));
        Assert.Equal("kly-ff-one-help", one.GetAttribute("aria-describedby"));
        var many = cut.Find("input#kly-ff-many");
        Assert.True(many.HasAttribute("multiple"));
        Assert.Equal("image/*", many.GetAttribute("accept"));
        var pickers = cut.FindAll("[data-slot=form-file-picker]");
        Assert.Contains("Choisir un fichier", pickers[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Choisir des fichiers", pickers[1].TextContent, StringComparison.Ordinal);
        Assert.All(cut.FindAll("[data-slot=form-file-empty]"), e => Assert.Equal("Aucun fichier choisi", e.TextContent.Trim()));
    }

    [Fact]
    public async Task Choosing_a_file_stores_a_FormFileValue_that_can_be_read_and_removing_it_clears_the_field()
    {
        var form = Form(new FormFieldSchema { Key = "doc", Kind = FormFieldKind.File, Label = "Doc", Accept = ".txt", MaxFileSize = 1024 });
        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        var value = Assert.IsType<FormFileValue>(form.GetValue("doc"));
        Assert.Equal("note.txt", value.Name);
        Assert.Equal(5, value.Size);
        Assert.Equal("text/plain", value.ContentType);
        await using (var stream = await value.OpenReadAsync(Xunit.TestContext.Current.CancellationToken))
        using (var reader = new StreamReader(stream))
        {
            Assert.Equal("hello", await reader.ReadToEndAsync(Xunit.TestContext.Current.CancellationToken));
        }
        Assert.Equal("note.txt", cut.Find("[data-slot=form-file]").GetAttribute("data-name"));
        Assert.Contains("5 B", cut.Find("[data-slot=form-file]").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-slot=form-field-error]"));

        await cut.Find("[data-slot=form-file-remove]").ClickAsync();

        Assert.Null(form.GetValue("doc"));
        Assert.NotEmpty(cut.FindAll("[data-slot=form-file-empty]"));
    }

    [Fact]
    public void A_file_that_breaks_a_rule_shows_the_error_at_once()
    {
        var form = Form(new FormFieldSchema { Key = "doc", Kind = FormFieldKind.File, Label = "Doc", Accept = ".png", MaxFileSize = 3 });
        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));
        Assert.Contains("note.txt is not an accepted type (.png)", cut.Find("[data-slot=form-field-error]").TextContent, StringComparison.Ordinal);

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "big.png", contentType: "image/png"));
        Assert.Contains("big.png is larger than 3 B", cut.Find("[data-slot=form-field-error]").TextContent, StringComparison.Ordinal);
        Assert.Equal("true", cut.Find("input#kly-ff-doc").GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task A_multiple_file_field_keeps_a_list_and_removes_one_file_at_a_time()
    {
        var form = Form(new FormFieldSchema { Key = "docs", Kind = FormFieldKind.File, Label = "Docs", Multiple = true });
        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "a.txt"),
            InputFileContent.CreateFromText("bb", "b.txt"));

        var list = Assert.IsType<List<FormFileValue>>(form.GetValue("docs"));
        Assert.Equal(["a.txt", "b.txt"], list.Select(f => f.Name));
        Assert.Equal(2, cut.FindAll("[data-slot=form-file]").Count);

        await cut.FindAll("[data-slot=form-file-remove]")[0].ClickAsync();
        Assert.Equal(["b.txt"], Assert.IsType<List<FormFileValue>>(form.GetValue("docs")).Select(f => f.Name));

        await cut.FindAll("[data-slot=form-file-remove]")[0].ClickAsync();
        Assert.Null(form.GetValue("docs"));
    }

    [Fact]
    public void A_file_cell_in_a_repeatable_list_works_like_a_field()
    {
        var lines = new FormFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayList,
            Fields = [new FormFieldSchema { Key = "attachment", Kind = FormFieldKind.File, Label = "Attachment" }],
        };
        var form = Form(lines);
        form.AddArrayItem(lines);
        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "x.txt"));

        Assert.Equal("x.txt", ((FormFileValue)form.GetArrayItems("lines")[0]["attachment"]!).Name);
        Assert.Equal("x.txt", cut.Find("[data-slot=form-file]").GetAttribute("data-name"));
    }
}
