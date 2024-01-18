namespace BuilderGenerator;

public record PropertyInfo
{
    public string PropertyName { get; }

    public string PropertyType { get; }

    public PropertyInfo(string name, string type) => (PropertyName, PropertyType) = (name, type);
}
