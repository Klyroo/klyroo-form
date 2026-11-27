using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

// Builds small forms and finds the views a test needs.
internal static class FormKit
{
    public static DynamicFormSchema Schema(params DynamicFieldSchema[] fields) => new() { FormKey = "test", Fields = fields };

    public static DynamicFieldSchema Field(string key, FormFieldKind kind = FormFieldKind.Text, string? label = null) =>
        new() { Key = key, Kind = kind, Label = label ?? key };

    public static KlyDynamicFormEngine Engine(DynamicFormSchema schema, IReadOnlyDictionary<string, object?>? input = null) =>
        new() { Schema = schema, Input = input };

    public static IEnumerable<T> Views<T>(this View root)
        where T : class =>
        FormUi.Descendants(root).OfType<T>();

    public static FormFieldView FieldView(this KlyDynamicFormEngine engine, string key) =>
        engine.ShellView!.Views<FormFieldView>().First(v => v.Node.Key == key && !v.IsCell);

    public static FormControl ControlOf(this KlyDynamicFormEngine engine, string key) => engine.FieldView(key).Control;

    public static T Control<T>(this KlyDynamicFormEngine engine, string key)
        where T : FormControl => (T)engine.FieldView(key).Control;

    public static string Id(this KlyDynamicFormEngine engine, string key) => FormUi.Id(engine.EffectiveController!.Engine, key);
}
