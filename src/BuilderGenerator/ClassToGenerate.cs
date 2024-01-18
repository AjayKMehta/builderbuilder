using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace BuilderGenerator;
public readonly record struct ClassToGenerate
{
#pragma warning disable CA1051 // Do not declare visible instance fields
    public readonly string FullTypeName;
    public readonly string TypeName;
    public readonly string ContainingNameSpace;

    public readonly string BuilderClassName;
    public readonly EquatableArray<PropertyInfo> Properties;
#pragma warning restore CA1051 // Do not declare visible instance fields

    public ClassToGenerate(INamedTypeSymbol type)
    {
        TypeName = type.Name;
        ContainingNameSpace = type.ContainingNamespace.ToDisplayString();
        FullTypeName = GetTypeName(type, false);
        BuilderClassName = GetTypeName(type, true);

        List<PropertyInfo> propInfo = [];
        foreach (ISymbol member in type.GetMembers())
        {
            if (member is IPropertySymbol propSymbol)
                propInfo.Add(new PropertyInfo(propSymbol.Name, propSymbol.Type.ToString()));
        }
        Properties = new([.. propInfo]);
    }

    private static string GetTypeName(INamedTypeSymbol type, bool isBuilder)
    {
        var typeName = type.Name;
        ImmutableArray<SymbolDisplayPart> displayParts = type.ToDisplayParts();
        var numParts = displayParts.Length;

        if (numParts == 0 || !type.IsGenericType || type.IsUnboundGenericType)
            return isBuilder ? typeName + "Builder" : typeName;

        var parts = new List<string>(numParts);
        var capture = false;
        for (var i = 0; i < numParts; i++)
        {
            var part = displayParts[i].ToString();
            if (!capture && part == typeName && i + 2 < numParts && displayParts[i + 1].ToString() == "<")
            {
                capture = true;
                if (isBuilder)
                    part += "Builder";
            }
            if (capture)
                parts.Add(part);
        }
        return string.Concat(parts);
    }

}
