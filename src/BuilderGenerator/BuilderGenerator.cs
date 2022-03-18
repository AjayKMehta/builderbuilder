﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace BuilderGenerator;

[Generator]
public class BuilderGenerator : IIncrementalGenerator
{
    private const string BuildableAttribute = "BuilderGenerator.BuildableAttribute";

    private const string AttributeText = @"
using System;

namespace BuilderGenerator;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BuildableAttribute : Attribute { }

";

    private static readonly DiagnosticDescriptor ErrorGeneratingBuilderSource = new
    (
        id: "BB001",
        title: "An error has occurred while generating source for builder",
        messageFormat: "An error has ocurred while generating source for builder with name `{0}`: {1}",
        category: "Compilation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor SuccessfullyGeneratedBuilderSource = new
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
            "BuildableAttribute.g.cs", SourceText.From(AttributeText, Encoding.UTF8)));

        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right)> incValueProvider = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput(incValueProvider,
    static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    public static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax {AttributeLists.Count: > 0 };

    public static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a cds thanks to IsSyntaxTargetForGeneration
        var cds = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes on the method
        foreach (var attributeListSyntax in cds.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
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

    public static void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (!(classes?.Any() ?? false))
        {
            // nothing to do yet
            return;
        }
        var buildableSymbol = compilation.GetTypeByMetadataName(BuildableAttribute)!;

        foreach (var @class in classes)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            var model = compilation.GetSemanticModel(@class.SyntaxTree, true);
            if (model.GetDeclaredSymbol(@class) is not INamedTypeSymbol typeSymbol)
                continue;

            if (HasAttribute(typeSymbol, buildableSymbol))
                Execute(context, typeSymbol);
        }
    }

    private static bool HasAttribute(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true)
                return true;
        }
        return false;
    }

    private static void Execute(SourceProductionContext context, INamedTypeSymbol typeSymbol)
    {
        try
        {
            var source = TypeBuilderWriter.Write(typeSymbol);
            var sourceText = SourceText.From(source, Encoding.UTF8);
            context.ReportDiagnostic(Diagnostic.Create(SuccessfullyGeneratedBuilderSource, Location.None, typeSymbol.Name));
            string name = typeSymbol.Name;
            var idx = name.IndexOf('<');
            if (idx > -1)
            {
                name = name.Substring(0, idx);
            }
            context.AddSource($"{name}Builder.cs", sourceText);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(ErrorGeneratingBuilderSource, Location.None, typeSymbol.Name, ex.Message));
        }
    }
}