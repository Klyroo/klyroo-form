using System.Reflection;

namespace Klyroo.Form;

/// <summary>Describes the member of a model a field is declared from. A convention (<see cref="FormFieldConventionContext"/>) receives it to decide the settings of the field, and <c>Klyroo.Form.DataAnnotations</c> reads the attributes of <see cref="Property"/>.</summary>
public sealed class FormMemberInfo
{
    internal FormMemberInfo(string key, IReadOnlyList<PropertyInfo> path, Type modelType)
    {
        Key = key;
        Path = path;
        Property = path[^1];
        ModelType = modelType;
        Type = Property.PropertyType;
        UnderlyingType = Nullable.GetUnderlyingType(Type) ?? Type;
        IsNullableValueType = Nullable.GetUnderlyingType(Type) is not null;
    }

    /// <summary>The key of the field: the names of the members from the model down to this one, joined with dots (<c>Address.City</c>). It is the key of the value in the controller and in <see cref="FormController{TModel}.CreateModel"/>.</summary>
    public string Key { get; }

    /// <summary>The name of the member itself (<c>City</c> for <c>Address.City</c>).</summary>
    public string Name => Property.Name;

    /// <summary>The members from the model to this one. The last one is <see cref="Property"/>.</summary>
    public IReadOnlyList<PropertyInfo> Path { get; }

    /// <summary>The member the field reads and writes.</summary>
    public PropertyInfo Property { get; }

    /// <summary>The type of the model the form is declared for.</summary>
    public Type ModelType { get; }

    /// <summary>The declared type of the member, <see cref="Nullable{T}"/> included.</summary>
    public Type Type { get; }

    /// <summary>The type of the member without <see cref="Nullable{T}"/>.</summary>
    public Type UnderlyingType { get; }

    /// <summary><c>true</c> when the member is a <see cref="Nullable{T}"/>.</summary>
    public bool IsNullableValueType { get; }

    /// <summary>The key a sibling member of this one has: same parent, another member name. <c>Address.City</c> and <c>Zip</c> give <c>Address.Zip</c>.</summary>
    /// <param name="memberName">The name of the sibling member.</param>
    public string SiblingKey(string memberName)
    {
        var dot = Key.LastIndexOf('.');
        return dot < 0 ? memberName : string.Concat(Key.AsSpan(0, dot + 1), memberName);
    }
}
