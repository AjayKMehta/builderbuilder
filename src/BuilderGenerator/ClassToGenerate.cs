using System.Collections.Generic;

namespace BuilderGenerator;
public readonly record struct ClassToGenerate
{
#pragma warning disable CA1051 // Do not declare visible instance fields
    public readonly string Name;

    public readonly EquatableArray<PropertyInfo> Properties;
#pragma warning restore CA1051 // Do not declare visible instance fields
    public ClassToGenerate(string name, List<PropertyInfo> properties)
    {
        Name = name;
        Properties = new([.. properties]);
    }
}
