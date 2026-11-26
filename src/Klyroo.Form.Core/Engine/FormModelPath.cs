using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Klyroo.Form;

internal static class FormModelTrim
{
    // What the schema reads and writes on a model: its public properties (getters, setters, init accessors) and its parameterless constructor.
    public const DynamicallyAccessedMemberTypes Members =
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
}

internal sealed class FormModelPath(IReadOnlyList<PropertyInfo> properties)
{
    public IReadOnlyList<PropertyInfo> Properties { get; } = properties;

    public string Key => string.Join('.', Properties.Select(p => p.Name));

    public FormModelPath Append(FormModelPath tail) => new([.. Properties, .. tail.Properties]);

    // A member access, or a chain of them, from the parameter of the lambda. The expression is read, never compiled. When the lambda is typed to a base of the
    // member (a list member given as IEnumerable), the conversion on the way is skipped and expected is null.
    public static FormModelPath Resolve(LambdaExpression lambda, Type? expected)
    {
        var chain = new List<PropertyInfo>();
        var node = lambda.Body;
        while (node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary) node = unary.Operand;
        while (node is MemberExpression member)
        {
            if (member.Member is not PropertyInfo property)
            {
                throw new ArgumentException($"'{member.Member.Name}' is a field: only properties can be bound.", nameof(lambda));
            }
            chain.Insert(0, property);
            node = member.Expression;
        }
        if (node is not ParameterExpression || chain.Count == 0)
        {
            throw new ArgumentException("Use a member access such as m => m.Name, or a chain such as m => m.Address.City.", nameof(lambda));
        }
        if (expected is not null && chain[^1].PropertyType != expected)
        {
            throw new ArgumentException($"'{chain[^1].Name}' is a {chain[^1].PropertyType.Name}, not a {expected.Name}.", nameof(lambda));
        }
        for (var i = 0; i < chain.Count - 1; i++)
        {
            if (chain[i].PropertyType.IsValueType)
            {
                throw new ArgumentException($"'{chain[i].Name}' is a struct: only members of a class can be bound through it.", nameof(lambda));
            }
        }
        return new FormModelPath(chain);
    }
}
