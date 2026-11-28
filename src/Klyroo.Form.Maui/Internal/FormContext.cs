using System.Runtime.CompilerServices;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// What the views that draw one form share: its engine, the renderers of its custom fields, the views already made for its fields (a field keeps its view, and so its focus and what the user
/// is typing, as long as the form lives) and the places to move the focus to after a failed submit.
/// </summary>
internal sealed class FormContext
{
    private readonly Dictionary<FieldNode, FormNodeView?> _views = [];
    private readonly Dictionary<string, (Func<bool> Focus, View View)> _targets = new(StringComparer.Ordinal);
    private readonly FormTemplateRegistry _registry;

    public FormContext(FormEngine engine, double compactWidth, IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? templates)
    {
        Engine = engine;
        CompactWidth = compactWidth;
        Templates = templates;
        _registry = FormTemplateRegistry.For(engine);
        _registry.Changed += OnRegistryChanged;
    }

    public FormEngine Engine { get; }

    public double CompactWidth { get; }

    /// <summary>The renderers of the custom fields of a dynamic form, by custom key.</summary>
    public IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? Templates { get; }

    public FormTemplateRegistry Registry => _registry;

    /// <summary>The number of columns of the grid of the form.</summary>
    public int Columns => Math.Clamp(Engine.Schema.Columns, 1, 12);

    /// <summary>Called when the user presses the return key in a single-line input: the form submits or goes to the next step.</summary>
    public Action? Enter { get; set; }

    /// <summary>Raised when the views of the fields must be made again: a template appeared or went away.</summary>
    public event Action? Rebuilt;

    /// <summary>The view of a field, made the first time it is asked for. Hidden inputs have none.</summary>
    public FormNodeView? ViewOf(FieldNode node)
    {
        if (!_views.TryGetValue(node, out var view))
        {
            view = FormNodes.Create(this, node);
            _views[node] = view;
        }
        return view;
    }

    public void RegisterTarget(string id, Func<bool> focus, View view) => _targets[id] = (focus, view);

    /// <summary>Puts the focus in the control with this id and scrolls it into view. False when there is none.</summary>
    public bool Focus(string id)
    {
        if (!_targets.TryGetValue(id, out var target)) return false;
        _ = FormUi.RevealAsync(target.View);
        return target.Focus();
    }

    public void Detach() => _registry.Changed -= OnRegistryChanged;

    private void OnRegistryChanged()
    {
        _views.Clear();
        _targets.Clear();
        Rebuilt?.Invoke();
    }
}

/// <summary>
/// The <see cref="KlyFieldTemplate{TModel, TValue}"/>s of a form, by field key. They register with the controller they belong to, and the view of a field looks its key up when it picks its control,
/// so a template works whether it sits in a <see cref="KlyFormEngine{TModel}"/> or in a layout built with <see cref="KlyFormFieldView{TModel}"/>.
/// </summary>
internal sealed class FormTemplateRegistry
{
    private static readonly ConditionalWeakTable<FormEngine, FormTemplateRegistry> Registries = new();

    private readonly Dictionary<string, Entry> _items = new(StringComparer.Ordinal);

    private sealed record Entry(object Owner, Type ValueType, Func<FormControlContext, View> Render);

    public event Action? Changed;

    public static FormTemplateRegistry For(FormEngine engine) => Registries.GetValue(engine, _ => new FormTemplateRegistry());

    public void Set(string key, object owner, Type valueType, Func<FormControlContext, View> render)
    {
        lock (_items) _items[key] = new Entry(owner, valueType, render);
        Changed?.Invoke();
    }

    public void Remove(string key, object owner)
    {
        bool removed;
        lock (_items) removed = _items.TryGetValue(key, out var entry) && ReferenceEquals(entry.Owner, owner) && _items.Remove(key);
        if (removed) Changed?.Invoke();
    }

    // A template made for another value type than the field has is not used: it would read the control as the wrong type.
    public bool TryGet(string key, Type? valueType, out Func<FormControlContext, View> render)
    {
        lock (_items)
        {
            if (_items.TryGetValue(key, out var entry) && entry.ValueType == valueType)
            {
                render = entry.Render;
                return true;
            }
        }
        render = null!;
        return false;
    }
}

/// <summary>Picks the control of a field: a template of the field, then the renderers of custom fields, then the control of its kind.</summary>
internal static class FormControlFactory
{
    public static FormControl Create(FormContext context, FormSlot slot)
    {
        var field = slot.Field;

        // A template belongs to a field of the form, not to a column of a list that happens to share its key.
        if (!slot.IsCell && field.ContextFactory is { } create && context.Registry.TryGet(field.Key, field.ValueType, out var template))
        {
            return new FormCustomControl(slot, s => template(create(s.Binding())));
        }
        if (field.Kind == FormFieldKind.Custom) return new FormCustomControl(slot, s => RenderCustom(context, s));

        return field.Kind switch
        {
            FormFieldKind.Textarea => new FormTextareaControl(slot),
            FormFieldKind.Password => new FormPasswordControl(slot),
            FormFieldKind.Number => new FormNumberControl(slot),
            FormFieldKind.Slider => new FormSliderControl(slot),
            FormFieldKind.Checkbox or FormFieldKind.Switch => new FormCheckControl(slot),
            FormFieldKind.Radio => new FormChoiceControl(slot, multiple: false),
            FormFieldKind.CheckboxGroup => new FormChoiceControl(slot, multiple: true),
            FormFieldKind.Select or FormFieldKind.AutoComplete when field.Multiple => new FormChoiceControl(slot, multiple: true),
            FormFieldKind.Select => new FormSelectControl(slot),
            FormFieldKind.AutoComplete => new FormAutoCompleteControl(slot),
            FormFieldKind.DateRange => new FormDateRangeControl(slot),
            FormFieldKind.Rating => new FormRatingControl(slot),
            FormFieldKind.Tag => new FormTagsControl(slot),
            FormFieldKind.File => new FormFileControl(slot),
            FormFieldKind.Date => new FormDateControl(slot),
            FormFieldKind.Time => new FormTimeControl(slot),
            FormFieldKind.DateTime => new FormDateTimeControl(slot),
            FormFieldKind.ColorPicker => new FormColorControl(slot),
            FormFieldKind.PhoneNumber => new FormTextControl(slot, Keyboard.Telephone, limitLength: true),
            FormFieldKind.OneTimeCode => new FormTextControl(slot, Keyboard.Numeric, limitLength: true),
            FormFieldKind.Text => new FormTextControl(slot, Keyboard.Default, limitLength: true),
            _ => new FormTextControl(slot, Keyboard.Default, limitLength: false),
        };
    }

    // The renderer of a custom field: the one a dynamic form gives for its custom key, then the ones registered for every form.
    private static View? RenderCustom(FormContext context, FormSlot slot)
    {
        var field = slot.Field;
        var key = field.CustomKey;
        if (string.IsNullOrEmpty(key)) return FormMissingRenderer.Notice(field);
        if (field.ContextFactory is { } create)
        {
            return FormCustomFields.TryGet(key, field.ValueType!, out var typed) ? typed(create(slot.Binding())) : FormMissingRenderer.Notice(field);
        }
        if (context.Templates is not null && context.Templates.TryGetValue(key, out var template)) return template(new DynamicControlContext(slot.Binding()));
        return FormCustomFields.TryGet(key, out var registered) ? registered(new DynamicControlContext(slot.Binding())) : FormMissingRenderer.Notice(field);
    }
}
