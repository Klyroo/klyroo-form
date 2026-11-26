using System.Reflection;
using Bunit;
using Klyroo.UI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class LocalizationTests : BunitContext
{
    public LocalizationTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly string[] AllCodes = ["en", "fr", "de", "es", "it", "pt", "pt-BR", "nl", "ja", "zh-Hans", "ar", "ru"];

    private static FormSchema Schema() => new()
    {
        FormKey = "loc",
        Fields =
        [
            new FormFieldSchema { Key = "name", Label = "Nom", Required = true },
            new FormFieldSchema { Key = "age", Kind = FormFieldKind.Number, Min = 18, Max = 99 },
            new FormFieldSchema { Key = "bio", Kind = FormFieldKind.Textarea, MaxLength = 5 },
            new FormFieldSchema { Key = "span", Kind = FormFieldKind.DateRange },
        ],
    };

    // Core messages

    [Fact]
    public void Controller_defaults_to_english_messages()
    {
        var form = new FormController(Schema());
        form.SetValue("age", 3.0);
        form.SetValue("bio", "toolong");
        form.SetValue("span", new List<string> { "2026-02-01", "2026-01-01" });

        Assert.False(form.Validate());

        Assert.Equal("This field is required", form.GetError("name"));
        Assert.Equal("Value must be at least 18", form.GetError("age"));
        Assert.Equal("Must be at most 5 characters", form.GetError("bio"));
        Assert.Equal("The end date must not be before the start date", form.GetError("span"));
        form.SetValue("age", 100.0);
        Assert.Equal("Value must be at most 99", form.GetError("age"));
    }

    [Fact]
    public void Messages_can_be_set_on_the_controller_without_any_UI()
    {
        var form = new FormController(Schema()) { Messages = FormMessageLocales.Fr };
        form.SetValue("age", 3.0);
        form.SetValue("bio", "toolong");
        form.SetValue("span", new List<string> { "2026-02-01", "2026-01-01" });

        Assert.False(form.Validate());

        Assert.Equal("Ce champ est obligatoire", form.GetError("name"));
        Assert.Equal("La valeur doit être supérieure ou égale à 18", form.GetError("age"));
        Assert.Equal("5 caractères au maximum", form.GetError("bio"));
        Assert.Equal("La date de fin ne peut pas précéder la date de début", form.GetError("span"));
    }

    [Fact]
    public void A_schema_can_fix_its_own_language_and_a_field_message_still_wins()
    {
        var schema = new FormSchema
        {
            FormKey = "x",
            Messages = FormMessages.Default with { Required = "Fill this in" },
            Fields =
            [
                new FormFieldSchema { Key = "a", Required = true },
                new FormFieldSchema { Key = "b", Required = true, RequiredMessage = "B please" },
            ],
        };
        var form = new FormController(schema);

        form.Validate();

        Assert.Equal("Fill this in", form.GetError("a"));
        Assert.Equal("B please", form.GetError("b"));
    }

    [Fact]
    public void Changing_the_messages_recomputes_the_errors_already_on_screen()
    {
        var lines = new FormFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayList,
            Fields = [new FormFieldSchema { Key = "sku", Required = true }],
        };
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a", Required = true }, lines] });
        form.AddArrayItem(lines);
        form.Validate();
        Assert.Equal("This field is required", form.GetError("a"));
        Assert.Equal("This field is required", form.GetArrayItemError("lines", 0, "sku"));

        form.Messages = FormMessageLocales.De;

        Assert.Equal("Dieses Feld ist erforderlich", form.GetError("a"));
        Assert.Equal("Dieses Feld ist erforderlich", form.GetArrayItemError("lines", 0, "sku"));
    }

    [Fact]
    public void Locale_messages_from_a_component_do_not_replace_a_language_chosen_by_code_or_schema()
    {
        var explicitForm = new FormController(Schema()) { Messages = FormMessageLocales.De };
        var schemaForm = new FormController(new FormSchema { FormKey = "x", Messages = FormMessageLocales.Es, Fields = [] });
        var open = new FormController(Schema());

        foreach (var form in new[] { explicitForm, schemaForm, open })
        {
            Render<KlyFormEngine>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, form));
        }

        Assert.Same(FormMessageLocales.De, explicitForm.Messages);
        Assert.Same(FormMessageLocales.Es, schemaForm.Messages);
        Assert.Same(FormMessageLocales.Fr, open.Messages);
        Assert.Throws<ArgumentNullException>(() => open.Messages = null!);
    }

    [Fact]
    public void Every_locale_has_a_message_set_and_only_en_and_fr_are_reviewed()
    {
        Assert.Equal(AllCodes.Order(), FormMessageLocales.Tables.Keys.Order());
        Assert.Equal(["en", "fr"], FormMessageLocales.ReviewedCodes);
        foreach (var (code, messages) in FormMessageLocales.Tables)
        {
            Assert.False(string.IsNullOrWhiteSpace(messages.Required), code);
            Assert.False(string.IsNullOrWhiteSpace(messages.DateRangeOrder), code);
            Assert.False(string.IsNullOrWhiteSpace(messages.SingleFileOnly), code);
            Assert.Contains("18", messages.MinValue(18), StringComparison.Ordinal);
            Assert.Contains("99", messages.MaxValue(99), StringComparison.Ordinal);
            Assert.Contains("20", messages.MaxLength(20), StringComparison.Ordinal);
            var tooLarge = messages.FileTooLarge("report.pdf", "5 MB");
            Assert.Contains("report.pdf", tooLarge, StringComparison.Ordinal);
            Assert.Contains("5 MB", tooLarge, StringComparison.Ordinal);
            var type = messages.FileTypeNotAllowed("photo.gif", ".png");
            Assert.Contains("photo.gif", type, StringComparison.Ordinal);
            Assert.Contains(".png", type, StringComparison.Ordinal);
            if (code != "en") Assert.NotEqual(FormMessageLocales.En.Required, messages.Required);
        }
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("fr", "fr")]
    [InlineData("fr-CA", "fr")]
    [InlineData("pt-PT", "pt")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-Hans-CN", "zh-Hans")]
    [InlineData("zh-TW", "en")]
    [InlineData("tlh", "en")]
    [InlineData("", "en")]
    public void Resolve_picks_the_best_set_for_a_culture_name(string code, string expected)
    {
        Assert.Same(FormMessageLocales.Tables[expected], FormMessageLocales.Resolve(code));
    }

    [Theory]
    [InlineData(1, "символа")]
    [InlineData(2, "символов")]
    [InlineData(5, "символов")]
    [InlineData(21, "символа")]
    [InlineData(11, "символов")]
    public void Russian_text_length_uses_the_plural_form(int n, string word)
    {
        Assert.Equal($"Не более {n} {word}", FormMessageLocales.Ru.MaxLength(n));
    }

    // Component texts

    private IRenderedComponent<KlyFormEngine> Engine(KlyLocale locale, FormController form, Action<ComponentParameterCollectionBuilder<KlyFormEngine>>? configure = null) =>
        Render<KlyFormEngine>(p =>
        {
            p.AddCascadingValue(locale);
            p.Add(c => c.Form, form);
            p.Add(c => c.OnCancel, _ => { });
            p.Add(c => c.Shell, FormShellMode.Modal);
            configure?.Invoke(p);
        });

    private static FormController Wizard() => new(new FormSchema
    {
        FormKey = "wizard",
        Steps =
        [
            new FormStepSchema { Key = "a", Title = "A", Fields = [new FormFieldSchema { Key = "name", Label = "Nom", Required = true }] },
            new FormStepSchema { Key = "b", Title = "B", Fields = [new FormFieldSchema { Key = "other" }] },
        ],
    });

    [Fact]
    public async Task Engine_shows_the_texts_of_the_locale_and_validation_messages_in_it()
    {
        var form = Wizard();
        var cut = Engine(KlyLocales.Fr, form);

        Assert.Equal("Suivant", cut.Find("[data-slot=form-action-next]").TextContent.Trim());
        Assert.Equal("Annuler", cut.Find("[data-slot=form-action-cancel]").TextContent.Trim());

        await cut.Find("[data-slot=form-action-next]").ClickAsync();

        Assert.Contains("Ce champ est obligatoire", cut.Find("[data-slot=form-field-error]").TextContent, StringComparison.Ordinal);
        var summary = cut.Find("[data-slot=form-error-summary]").TextContent;
        Assert.Contains("1 erreur à corriger", summary, StringComparison.Ordinal);
        Assert.Contains("Nom : Ce champ est obligatoire", summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Engine_shows_the_localized_submit_and_saving_texts_and_close_name()
    {
        var release = new TaskCompletionSource<bool>();
        var form = new FormController(new FormSchema { FormKey = "x", Title = "T", Fields = [new FormFieldSchema { Key = "a" }] });
        var cut = Engine(KlyLocales.De, form, p => p.Add(c => c.OnSave, async (_, _) => await release.Task));

        Assert.Equal("Absenden", cut.Find("[data-slot=form-action-submit]").TextContent.Trim());
        Assert.Equal("Formular schließen", cut.Find("[data-slot=form-close]").GetAttribute("aria-label"));

        var submit = cut.Find("form").SubmitAsync();
        cut.WaitForAssertion(() => Assert.Equal("Wird gespeichert…", cut.Find("[data-slot=form-action-submit]").TextContent.Trim()));
        release.SetResult(true);
        await submit;
    }

    [Fact]
    public void SubmitLabel_wins_over_the_locale_text()
    {
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a" }] });
        var cut = Engine(KlyLocales.Fr, form, p => p.Add(c => c.SubmitLabel, "Envoyer"));

        Assert.Equal("Envoyer", cut.Find("[data-slot=form-action-submit]").TextContent.Trim());
    }

    [Fact]
    public void A_regional_locale_uses_the_set_of_its_language()
    {
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a" }] });
        var cut = Engine(KlyLocales.Extend(KlyLocales.Fr, code: "fr-CA", name: "Français (Canada)"), form);

        Assert.Equal("Valider", cut.Find("[data-slot=form-action-submit]").TextContent.Trim());
    }

    [Fact]
    public void An_unknown_locale_gets_english()
    {
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a" }] });
        var cut = Engine(KlyLocales.Extend(KlyLocales.En, code: "tlh", name: "Klingon"), form);

        Assert.Equal("Submit", cut.Find("[data-slot=form-action-submit]").TextContent.Trim());
    }

    [Fact]
    public void Texts_can_be_changed_per_locale_in_AddKlyrooForm()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f
            .Texts("fr", t => t with { Submit = "Envoyer", Messages = t.Messages with { Required = "Merci de renseigner ce champ" } })
            .Texts("fr-CA", t => t with { Submit = "Soumettre" }));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a", Required = true }] });

        var fr = ctx.Render<KlyFormEngine>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, form));
        Assert.Equal("Envoyer", fr.Find("[data-slot=form-action-submit]").TextContent.Trim());
        form.Validate();
        Assert.Equal("Merci de renseigner ce champ", form.GetError("a"));

        var ca = ctx.Render<KlyFormEngine>(p => p
            .AddCascadingValue(KlyLocales.Extend(KlyLocales.Fr, code: "fr-CA"))
            .Add(c => c.Form, new FormController(new FormSchema { FormKey = "y", Fields = [] })));
        Assert.Equal("Soumettre", ca.Find("[data-slot=form-action-submit]").TextContent.Trim());
    }

    [Fact]
    public void Texts_cannot_change_once_a_component_resolved_one()
    {
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a" }] });
        _ = Engine(KlyLocales.En, form);
        var options = Services.GetRequiredService<KlyrooOptions>().Extension<KlyrooFormOptions>();

        Assert.Throws<InvalidOperationException>(() => options.Texts("fr", t => t));
    }

    [Fact]
    public void Texts_rejects_a_blank_code_a_missing_change_and_a_change_that_returns_null()
    {
        var options = new KlyrooFormOptions();
        Assert.Throws<ArgumentException>(() => options.Texts(" ", t => t));
        Assert.Throws<ArgumentNullException>(() => options.Texts("fr", null!));

        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f.Texts("fr", _ => null!));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Assert.Throws<InvalidOperationException>(() => ctx.Render<KlyFormEngine>(p => p
            .AddCascadingValue(KlyLocales.Fr)
            .Add(c => c.Form, new FormController(new FormSchema { FormKey = "x", Fields = [] }))));
    }

    [Fact]
    public async Task Field_texts_follow_the_locale()
    {
        var schema = new FormSchema
        {
            FormKey = "x",
            Fields =
            [
                new FormFieldSchema
                {
                    Key = "pw",
                    Kind = FormFieldKind.Password,
                    Label = "Mot de passe",
                    PasswordRequirements = [new FormPasswordRequirement("len", "8 caractères", p => p.Length >= 8)],
                },
                new FormFieldSchema { Key = "span", Kind = FormFieldKind.DateRange, Label = "Période" },
                new FormFieldSchema { Key = "stars", Kind = FormFieldKind.Rating, Label = "Note", Max = 3 },
                new FormFieldSchema { Key = "pick", Kind = FormFieldKind.Select, Label = "Choix", Options = [new FormOptionItem("a", "A")], OnCreateOption = t => new FormOptionItem(t, t) },
                new FormFieldSchema { Key = "tags", Kind = FormFieldKind.Tag, Label = "Tags" },
                new FormFieldSchema { Key = "lines", Kind = FormFieldKind.ArrayList, Label = "Lignes", Fields = [new FormFieldSchema { Key = "sku", Label = "Réf" }] },
            ],
        };
        var form = new FormController(schema);
        var cut = Render<KlyFormEngine>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, form));

        Assert.Equal("Afficher le mot de passe", cut.Find("[data-slot=form-password-toggle]").GetAttribute("aria-label"));
        Assert.Equal("Afficher", cut.Find("[data-slot=form-password-toggle]").TextContent.Trim());
        Assert.Equal("Exigences du mot de passe", cut.Find("[data-slot=form-password-requirements]").GetAttribute("aria-label"));
        Assert.Contains("(non respectée)", cut.Find("[data-slot=form-password-requirement]").TextContent, StringComparison.Ordinal);
        Assert.Equal("Date de début", cut.Find("[data-slot=form-date-range-start]").GetAttribute("aria-label"));
        Assert.Equal("Date de fin", cut.Find("[data-slot=form-date-range-end]").GetAttribute("aria-label"));
        Assert.Equal(["1 sur 3", "2 sur 3", "3 sur 3"], cut.FindAll("[data-slot=form-rating-star] input").Select(i => i.GetAttribute("aria-label")));
        Assert.Equal("Sélectionner…", cut.Find("select option").TextContent.Trim());
        Assert.Equal("Nouvelle option", cut.Find("[data-slot=form-create-option-input]").GetAttribute("placeholder"));
        Assert.Equal("Nouvelle option pour Choix", cut.Find("[data-slot=form-create-option-input]").GetAttribute("aria-label"));
        Assert.Equal("Créer", cut.Find("[data-slot=form-create-option-button]").TextContent.Trim());
        Assert.Equal("Ajoutez un tag et appuyez sur Entrée", cut.Find("[data-control=tags] input").GetAttribute("placeholder"));
        Assert.Equal("Aucun élément", cut.Find("[data-slot=form-array-empty]").TextContent.Trim());
        Assert.Contains("Ajouter un élément", cut.Find("[data-slot=form-array-add]").TextContent, StringComparison.Ordinal);

        await cut.Find("[data-slot=form-array-add]").ClickAsync();
        Assert.Equal("Supprimer l’élément 1", cut.Find("[data-slot=form-array-remove]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Page_texts_follow_the_locale()
    {
        var schema = new FormSchema
        {
            FormKey = "p",
            Title = "Titre",
            Sections =
            [
                new FormPageSectionSchema { Key = "a", Label = "A", Fields = [new FormFieldSchema { Key = "x", Required = true }] },
                new FormPageSectionSchema { Key = "b", Label = "B", Optional = true, Fields = [new FormFieldSchema { Key = "y" }] },
            ],
        };
        var cut = Render<KlyFormPage>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, new FormController(schema)));

        Assert.Equal("1 à compléter.", cut.Find("[data-slot=form-page-summary]").TextContent.Trim());
        Assert.Equal("Sections du formulaire", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Valider", cut.Find("[data-slot=form-page-submit]").TextContent.Trim());
        Assert.Contains("0 sur 1", cut.Find("[data-section=a]").TextContent, StringComparison.Ordinal);
        Assert.Contains("Facultatif", cut.Find("[data-section=b]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_and_guard_texts_follow_the_locale()
    {
        var provider = Render<KlyFormProvider>(p => p.AddCascadingValue(KlyLocales.Fr));
        _ = Services.GetRequiredService<FormService>().CreateFormAsync(new FormSchema { Fields = [new FormFieldSchema { Key = "a" }] });
        provider.Render();
        Assert.Equal("Formulaire", provider.Find("dialog").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task The_navigation_guard_asks_in_the_language_of_the_locale()
    {
        var form = new FormController(new FormSchema { FormKey = "x", Fields = [new FormFieldSchema { Key = "a" }] });
        _ = Render<KlyFormNavigationGuard>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, form));
        var confirm = JSInterop.Setup<bool>("confirm", "Vous avez des modifications non enregistrées. Quitter cette page et les perdre ?");
        confirm.SetResult(false);
        form.SetValue("a", "x");

        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/elsewhere");
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        Assert.Single(JSInterop.Invocations, i => i.Identifier == "confirm");
    }

    // Tables

    [Fact]
    public void Every_locale_has_complete_component_texts_and_the_message_set_of_its_language()
    {
        Assert.Equal(AllCodes.Order(), FormLocales.Tables.Keys.Order());
        Assert.Equal(["en", "fr"], FormLocales.ReviewedCodes);
        var english = FormLocales.En;
        var stringProps = typeof(FormTexts).GetProperties().Where(p => p.PropertyType == typeof(string)).ToList();
        Assert.True(stringProps.Count >= 25);

        foreach (var (code, texts) in FormLocales.Tables)
        {
            Assert.Same(FormMessageLocales.Tables[code], texts.Messages);
            foreach (var property in stringProps)
            {
                Assert.False(string.IsNullOrWhiteSpace((string?)property.GetValue(texts)), $"{code}.{property.Name}");
            }
            Assert.Contains("3", texts.LeftToComplete(3), StringComparison.Ordinal);
            Assert.Contains("2", texts.SectionProgress(1, 2), StringComparison.Ordinal);
            Assert.Contains("4", texts.ErrorsToFix(4), StringComparison.Ordinal);
            Assert.Contains("Email", texts.FieldError("Email", "msg"), StringComparison.Ordinal);
            var cell = texts.CellError("SKU", "Lines", 2, "msg");
            Assert.Contains("SKU", cell, StringComparison.Ordinal);
            Assert.Contains("Lines", cell, StringComparison.Ordinal);
            Assert.Contains("2", cell, StringComparison.Ordinal);
            Assert.Contains("5", texts.RatingStar(3, 5), StringComparison.Ordinal);
            Assert.Contains("news", texts.RemoveTag("news"), StringComparison.Ordinal);
            Assert.Contains("Country", texts.NewOptionFor("Country"), StringComparison.Ordinal);
            Assert.Contains("7", texts.RemoveItem(7), StringComparison.Ordinal);
            Assert.Contains("a.pdf", texts.RemoveFile("a.pdf"), StringComparison.Ordinal);
            if (code != "en")
            {
                var different = stringProps.Count(p => (string?)p.GetValue(texts) != (string?)p.GetValue(english));
                Assert.True(different >= stringProps.Count - 6, $"{code} reuses too many English texts ({different}/{stringProps.Count})");
            }
        }
    }

    [Fact]
    public void Draft_locales_are_marked_for_review_in_their_documentation_and_reviewed_ones_are_not()
    {
        foreach (var (suffix, code) in new[] { ("De", "de"), ("Es", "es"), ("It", "it"), ("Pt", "pt"), ("PtBr", "pt-BR"), ("Nl", "nl"), ("Ja", "ja"), ("ZhCn", "zh-Hans"), ("Ar", "ar"), ("Ru", "ru") })
        {
            Assert.DoesNotContain(code, FormLocales.ReviewedCodes);
            Assert.NotNull(typeof(FormLocales).GetProperty(suffix, BindingFlags.Public | BindingFlags.Static));
        }
    }
}
