using System.Diagnostics.CodeAnalysis;

namespace BuilderGenerator;

[ExcludeFromCodeCoverage]
public record PropertyInfo
{
    public string PropertyName { get; }

    public string PropertyType { get; }

    public PropertyInfo(string name, string type) => (PropertyName, PropertyType) = (name, type);
}
