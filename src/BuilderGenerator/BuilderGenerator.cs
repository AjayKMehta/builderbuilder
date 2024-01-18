using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static BuilderGenerator.BuilderGeneratorHelper;

namespace BuilderGenerator;

[Generator]
public class BuilderGenerator : IIncrementalGenerator
{
    private const string BuildableAttribute = "BuilderGenerator.BuildableAttribute";

    private static readonly DiagnosticDescriptor s_errorGeneratingBuilderSource = new
    (
        id: "BB001",
        title: "An error has occurred while generating source for builder",
        messageFormat: "An error has ocurred while generating source for builder with name `{0}`: {1}",
        category: "Compilation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor s_successfullyGeneratedBuilderSource = new
    (
        id: "BB002",
        title: "Successfully generated source for builder",
        messageFormat: "Successfully generated source for builder with name `{0}`",
        category: "Compilation",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    // See https://andrewlock.net/exploring-dotnet-6-part-9-source-generator-updates-incremental-generators/.
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "BuildableAttribute.g.cs", SourceText.From($"{Header}{AttributeText}", Encoding.UTF8)));

        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                // 👇 Runs for _every_ syntax node, on _every_ key press!
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                // 👇 Runs for _every_ node selected by the predicate, on _every_ key press!
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        // 👇 Runs for every _new_ value returned by the syntax provider
        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right)> incValueProvider = context.CompilationProvider.Combine(classDeclarations.Collect());

        // 👇 Runs for every _new_ value returned by the syntax provider
        context.RegisterSourceOutput(incValueProvider,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    public static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    public static ClassDeclarationSyntax? GetSemanticTargetForGeneration(in GeneratorSyntaxContext context)
    {
        // we know the node is a cds thanks to IsSyntaxTargetForGeneration
        var cds = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes on the method
        foreach (AttributeListSyntax attributeListSyntax in cds.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;

                // Is the attribute the attribute we are interested in?
                if (attributeContainingTypeSymbol.ToDisplayString() == BuildableAttribute)
                    return cds;
            }
        }

        return null;
    }

    public static void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, in SourceProductionContext context)
    {
        if (!(classes?.Any() ?? false))
        {
            // nothing to do yet
            return;
        }
        INamedTypeSymbol buildableSymbol = compilation.GetTypeByMetadataName(BuildableAttribute)!;

        foreach (ClassDeclarationSyntax @class in classes)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            SemanticModel model = compilation.GetSemanticModel(@class.SyntaxTree, true);
            if (model.GetDeclaredSymbol(@class) is not INamedTypeSymbol typeSymbol)
                continue;

            if (HasAttribute(typeSymbol, buildableSymbol))
                Execute(context, typeSymbol);
        }
    }

    private static bool HasAttribute(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (AttributeData attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true)
                return true;
        }
        return false;
    }

    private static void Execute(in SourceProductionContext context, INamedTypeSymbol typeSymbol)
    {
        try
        {
            var source = TypeBuilderWriter.Write(typeSymbol);
            var sourceText = SourceText.From(source, Encoding.UTF8);
            context.ReportDiagnostic(Diagnostic.Create(s_successfullyGeneratedBuilderSource, Location.None, typeSymbol.Name));
            var name = typeSymbol.Name;
            if (typeSymbol.IsGenericType)
            {
                var idx = name.IndexOf('<');
                if (idx > -1)
                {
                    name = name.Substring(0, idx);
                }
            }
            context.AddSource($"{name}Builder.g.cs", sourceText);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(s_errorGeneratingBuilderSource, Location.None, typeSymbol.Name, ex.Message));
        }
    }
}
