using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class RawDomainModelBuilderSourceGenerator : SourceGeneratorBase
{
    // Functionally identical to the DomainModelBuilderSourceGenerator but writen using only string interpolation instead of the CSharpBuilder class.

    private const string TargetAssembly = "Domain";
    private const string TargetNamespace = "Domain";
    private const string FileName = "RawDomainModelBuilder";
    private const string BuilderName = "RawBuilder";

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

                    var sb = new StringBuilder();
                    // Find all model classes to build ModelBuilder for.
                    var typeSymbols = FindTypes(compilation, TargetNamespace);

                    // Foreach model class found...
                    foreach (var typeSymbol in DistinctBy(typeSymbols, x => $"{x}"))
                    {
                        // Find all properties withing the found model class.
                        var properties = FindTypeProperties(typeSymbol);
                        var builtProperties = properties.Select(x => BuildPropertyAndMethods(typeSymbol, x));
                        var allPropertiesAndMethods = string.Join("\n\n", builtProperties);

                        var classString =
$$"""
namespace {{typeSymbol.ContainingNamespace}}
{
    public partial class {{typeSymbol.Name}}{{BuilderName}}   {
{{allPropertiesAndMethods}}

        public {{typeSymbol}} Build()
        {
            var item = ({{typeSymbol.Name}})Activator.CreateInstance(typeof({{typeSymbol.Name}}));
{{string.Join("\n", properties.Select(x => $"            item.{x.Name} = {x.Name};"))}}
            return item;
        }
    }
}
""";
                        sb.Append(classString);
                        sb.Append("\n\n");
                    }
                    var fullCode = sb.ToString();
                    spc.AddSource($"{FileName}.g.cs", SourceText.From(fullCode, Encoding.UTF8));

                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
                spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("RAW_BUILDER_ERROR", ex.Message, ex.Message, ex.Message, DiagnosticSeverity.Error, true, ex.Message), null));
            }
        });
    }

    private string BuildPropertyAndMethods(INamedTypeSymbol typeSymbol, IPropertySymbol property)
    {
        if (IsCollection(property))
        {
            var namedTypeSymbol = (INamedTypeSymbol)property.Type;
            var genericTypeParameter = namedTypeSymbol.TypeArguments.Single();
            return
$$"""
        protected {{property.Type}} {{property.Name}} { get; set; } = new List<{{genericTypeParameter}}>();

        public {{typeSymbol.Name}}{{BuilderName}} With{{property.Name}}(params {{genericTypeParameter}}[] {{property.Name}})
        {
            this.{{property.MetadataName}} = {{property.Name}}.ToList();
            return this;
        }

        public virtual {{typeSymbol.Name}}{{BuilderName}} Add{{property.Name}}Item({{genericTypeParameter}} item)
        {
            if(this.{{property.Name}} == null)
            {
                this.{{property.Name}} = new System.Collections.Generic.List<{{genericTypeParameter}}>();
            }

            this.{{property.Name}}.Add(item);
            return this;
        }

        public virtual {{typeSymbol.Name}}{{BuilderName}} Clear{{property.Name}}()
        {
            if(this.{{property.Name}} == null)
            {
                this.{{property.Name}} = new System.Collections.Generic.List<{{genericTypeParameter}}>();
            }

            this.{{property.Name}}.Clear();
            return this;
        }
""";
        }
        else
        {
            return
$$"""
        protected {{property.Type}} {{property.Name}} { get; set; }

        public {{typeSymbol.Name}}{{BuilderName}} With{{property.Name}}({{property.Type}} {{property.Name}})
        {
            this.{{property.MetadataName}} = {{property.Name}};
            return this;
        }
""";
        }
    }
}
