using System.Collections.Generic;
using System.Text;

using static BuilderGenerator.BuilderGeneratorHelper;

namespace BuilderGenerator;

internal static class TypeBuilderWriter
{
    private static readonly string s_toolName = typeof(BuilderGenerator).Assembly.GetName().Name;

    public static string Write(ClassToGenerate classToGenerate)
    {
        StringBuilder sb = new();

        const string Version = "v1.0.0.0";

        _ = sb.AppendLine(Header)
            .AppendLine("using System;")
            .AppendLine()
            .Append("namespace ")
            .Append(classToGenerate.ContainingNameSpace)
            .AppendLine(";")
            .AppendLine()
            // See https://github.com/dotnet/runtime/issues/64541.
            .Append("[System.CodeDom.Compiler.GeneratedCode(\"")
            .Append(s_toolName)
            .Append("\", \"")
            .Append(Version)
            .AppendLine("\")]")
            .Append("public class ")
            .Append(classToGenerate.BuilderClassName)
            .AppendLine("{");

        foreach (PropertyInfo prop in classToGenerate.Properties)
        {
            _ = sb
                .Indent(4)
                .Append("public ")
                .Append(prop.PropertyType)
                .Append(' ')
                .Append(prop.PropertyName)
                .AppendLine(" { get; set; }");
        }
        _ = sb.AppendLine();

        AppendBuildMethod(sb, classToGenerate.FullTypeName, classToGenerate.Properties, 4);
        _ = sb.AppendLine("}")
            .AppendLine();

        return sb.ToString();
    }


    private static StringBuilder Indent(this StringBuilder sb, int spaces) => sb.Append(' ', spaces);

    private static void AppendBuildMethod(StringBuilder sb, string typeName, IEnumerable<PropertyInfo> props, int spaces)
    {
        const string Separator = ", ";

        sb = sb
            .Indent(spaces)
            .Append("public ")
            .Append(typeName)
            .AppendLine(" Build() =>")
            .Indent(spaces * 2)
            .Append("new ")
            .Append(typeName)
            .Append('(');

        var hasProp = false;
        foreach (PropertyInfo prop in props)
        {
            hasProp = true;
            _ = sb.Append(prop.PropertyName).Append(Separator);
        }
        if (hasProp)
            sb.Length -= Separator.Length;

        _ = sb.AppendLine(");");
    }
}
