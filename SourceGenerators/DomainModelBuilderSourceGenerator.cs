using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class DomainModelBuilderSourceGenerator : SourceGeneratorBase
{
    // Generator to build a Builder class for each model class in the Domain project.
    // Each Builder class will have a With-method for each property in the model class.
    // The Builder class also supports collections and will generate AddItem and Clear methods for them.
    // The code is built up using the CSharpBuilder class, which is a helper class to build C# code.

    private const string TargetAssembly = "Domain";
    private const string TargetNamespace = "Domain";
    private const string FileName = "DomainModelBuilder";
    private const string BuilderName = "Builder";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {
            try
            {
                if (compilation.Assembly.Name == TargetAssembly)
                {
                    // Start building C# code using the CSharpBuilder.
                    var csharpSourceBuilder = new CSharpBuilder();

                    // Find all model classes to build ModelBuilder for.
                    var typeSymbols = FindTypes(compilation, TargetNamespace);

                    // Foreach model class found...
                    foreach (var typeSymbol in DistinctBy(typeSymbols, x => $"{x}"))
                    {
                        // Build a namespace with a Builder class.
                        var classBuilder = csharpSourceBuilder.WithNamespace($"{typeSymbol.ContainingNamespace}")
                                .WithPartialClass($"{typeSymbol.Name}{BuilderName}");

                        // Find all properties withing the found model class.
                        var properties = FindTypeProperties(typeSymbol);

                        // Build a property for all found properties.
                        foreach (var property in properties)
                        {
                            if (IsCollection(property))
                            {
                                var namedTypeSymbol = (INamedTypeSymbol)property.Type;
                                var genericTypeParameter = namedTypeSymbol.TypeArguments.Single();

                                classBuilder.WithProtectedProperty($"{property.Type}", property.Name)
                                        .WithInitializer($"new List<{genericTypeParameter}>()");
                            }
                            else
                            {
                                classBuilder.WithProtectedProperty($"{property.Type}", property.Name);
                            }
                        }

                        // Build a With-method for all found properties.
                        foreach (var property in properties)
                        {
                            // The generated methods are more crazy for collection typed properties.
                            if (IsCollection(property))
                            {
                                var namedTypeSymbol = (INamedTypeSymbol)property.Type;
                                var genericTypeParameter = namedTypeSymbol.TypeArguments.Single();

                                // Build the With method.
                                classBuilder.WithVirtualMethod($"{typeSymbol.Name}{BuilderName}", $"With{property.Name}")
                                        .WithParams($"{genericTypeParameter}[]", property.Name)
                                        .WithStatement($"this.{property.Name} = {property.Name}.ToList()")
                                        .WithStatement("return this");

                                // Build the AddItem method.
                                classBuilder.WithVirtualMethod($"{typeSymbol.Name}{BuilderName}", $"Add{property.Name}Item")
                                        .WithParameter($"{genericTypeParameter}", "item")
                                        .WithBlockStatement($"if(this.{property.Name} == null)")
                                            .WithStatement($"this.{property.Name} = new {$"{property.Type}".Replace("IList", "List").Replace("ICollection", "List")}()")
                                            .End()
                                        .WithEmptyStatement()
                                        .WithStatement($"this.{property.Name}.Add(item)")
                                        .WithStatement("return this");

                                // Build the Clear method.
                                classBuilder.WithVirtualMethod($"{typeSymbol.Name}{BuilderName}", $"Clear{property.Name}")
                                        .WithBlockStatement($"if(this.{property.Name} == null)")
                                            .WithStatement($"this.{property.Name} = new {$"{property.Type}".Replace("IList", "List").Replace("ICollection", "List")}()")
                                            .End()
                                        .WithEmptyStatement()
                                        .WithStatement($"this.{property.Name}.Clear()")
                                        .WithStatement("return this");
                            }
                            else
                            {
                                classBuilder.WithMethod($"{typeSymbol.Name}{BuilderName}", $"With{property.Name}")
                                        .WithParameter($"{property.Type}", property.Name)
                                        .WithStatement($"this.{property.Name} = {property.Name}")
                                        .WithStatement("return this");
                            }
                        }

                        // Build a Build-method that creates an instance of the request class.
                        var methodBuilder = classBuilder.WithMethod($"{typeSymbol}", "Build")
                                .WithStatement($"var item = ({typeSymbol.Name})Activator.CreateInstance(typeof({typeSymbol.Name}))");

                        // Loop through all found properties to copy values.
                        foreach (var property in properties)
                        {
                            methodBuilder.WithStatement($"item.{property.Name} = {property.Name}");
                        }

                        methodBuilder.WithStatement("return item");
                    }

                    // Build the C# source code from the generated object model and add
                    // it to the compile pipeline as a Source Generated source file.
                    spc.AddSource($"{FileName}.g.cs", SourceText.From(csharpSourceBuilder.Build(), Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
                spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("BUILDER_ERROR", ex.Message, ex.Message, ex.Message, DiagnosticSeverity.Error, true, ex.Message), null));
            }
        });
    }
}
