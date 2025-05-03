using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class TranslationsSourceGenerator : SourceGeneratorBase
{
    // Generator to read JSON files from the project and generate a C# class with properties for each translation key.
    // Not very practical, but a good example of how to use the Source Generator API with non-code files and other libraries.
    // A more practical solution might be to convert .json files to .resx files and use the built-in .NET localization features.

    private const string Namespace = "Translations";
    private const string FileName = "Translations";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // We can filter all the AdditionalFiiles that are provided
        var translations = context.AdditionalTextsProvider
                           .Where(static textFile => textFile.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && textFile.GetText()?.ToString() is not null)
                           .Collect();

        // Register the filtered files and not the entire context.
        context.RegisterSourceOutput(translations, (spc, jsonFiles) =>
        {
            if (jsonFiles.Length == 0) return;
            try
            {
                // Start building C# code using the CSharpBuilder.
                var csharpSourceBuilder = new CSharpBuilder();
                var namespaceBuilder = csharpSourceBuilder.WithNamespace(Namespace);
                foreach (var item in jsonFiles)
                {
                    var content = item.GetText()?.ToString();
                    if (content.Length == 0) continue;

                    var className = item.Path.Split('\\').Last().Replace(".json","").Replace(".","_");
                    var classBuilder = namespaceBuilder.WithSealedClass($"{className}");

                    // Using an external library in source generators is possible but won't work out of the box, see the .csproj file for required configuration.
                    var translations = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    foreach (var translation in translations)
                    {
                        classBuilder.WithPublicProperty(typeof(string).Name, translation.Key)
                        .WithConst().WithField()
                        .WithInitializer($"\"{translation.Value}\"");
                    }
                }

                // Build the C# source code from the generated object model and add
                // it to the compile pipeline as a Source Generated source file.
                spc.AddSource($"{FileName}.g.cs", SourceText.From(csharpSourceBuilder.Build(), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Debugger.Break();
                spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("TRANSLATIONS_ERROR", ex.Message, ex.Message, ex.Message, DiagnosticSeverity.Error, true, ex.Message), null));
            }
        });
    }
}
