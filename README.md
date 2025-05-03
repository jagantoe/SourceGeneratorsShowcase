# SourceGeneratorsShowcase
This project serves as a basic introduction to how "Source Generators" work.

More specifically [Incremental Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md) which serve as the follow up of the original [Source Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md) that were introduced.

To inspect the generated code you can open the Analyzers of the specific project.
![Generated file](image.png)

To debug the source generator you can add the following inside the generator and then build the project.
```
if(!Debugger.IsAttached) Debugger.Launch();
```

4 projects:
- Domain
    - Contains the domain classes and will contain the generated Builder classes.
- Translations
    - Contains .json files and will contain the generated classes with const strings respresenting translations.
- SourceGenerators
    - Contains the source generators for the builders and translations.
    All the credit for the CSharpBuilder and DomainModelBuilderSourceGenerator code goes to [Johnny Hooyberghs](https://github.com/Djohnnie).
- SourceGeneratorsShowcase
    - Consumes the generated code.
