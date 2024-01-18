using System;
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

        IncrementalValuesProvider<ClassToGenerate?  > classesToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BuildableAttribute,
                // 👇 Runs for _every_ syntax node, on _every_ key press!
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                // 👇 Runs for _every_ node selected by the predicate, on _every_ key press!
                transform: static (ctx, _) => GetClassToGenerate(ctx.SemanticModel, ctx.TargetNode));


        // 👇 Runs for every _new_ value returned by the syntax provider
        context.RegisterImplementationSourceOutput(classesToGenerate,
            static (spc, source) => Execute(source, spc));
    }

    public static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };


    private static ClassToGenerate? GetClassToGenerate(SemanticModel semanticModel, SyntaxNode classDeclarationSyntax)
    {
        return semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol typeSymbol
            ? null
            : (ClassToGenerate?)new ClassToGenerate(typeSymbol);
    }

    private static void Execute(ClassToGenerate? classToGenerate, SourceProductionContext context)
    {
        if (context.CancellationToken.IsCancellationRequested)
            return;

        if (classToGenerate is { } value)
        {
            var typeName = value.TypeName;
            try
            {
                var source = TypeBuilderWriter.Write(value);
                var sourceText = SourceText.From(source, Encoding.UTF8);
                context.ReportDiagnostic(Diagnostic.Create(s_successfullyGeneratedBuilderSource, Location.None, typeName));
                var name = value.FullTypeName;
                var idx = name.IndexOf('<');
                if (idx > -1)
                {
                    name = name.Substring(0, idx);
                }
                context.AddSource($"{name}Builder.g.cs", sourceText);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_errorGeneratingBuilderSource, Location.None, typeName, ex.Message));
            }
        }
    }
}
