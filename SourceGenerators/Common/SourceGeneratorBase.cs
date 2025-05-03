using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SourceGenerators.Common;

public abstract class SourceGeneratorBase : IIncrementalGenerator
{
    public virtual void Initialize(IncrementalGeneratorInitializationContext context)
    {

    }

    protected bool IsCollection(IPropertySymbol propertySymbol)
    {
        var propertyType = $"{propertySymbol.Type}";

        return propertyType.StartsWith("System.Collections.Generic.List") ||
            propertyType.StartsWith("System.Collections.Generic.IList") ||
            propertyType.StartsWith("System.Collections.Generic.ICollection");
    }

    protected IEnumerable<IPropertySymbol> FindTypeProperties(INamedTypeSymbol typeSymbol)
    {
        var typeMembers = new List<IPropertySymbol>();

        foreach (var typeMember in typeSymbol.GetMembers())
        {
            if (typeMember.Kind == SymbolKind.Property && typeMember is IPropertySymbol propertyMember && !propertyMember.IsReadOnly)
            {
                if (propertyMember.DeclaredAccessibility == Accessibility.Public && propertyMember.SetMethod.DeclaredAccessibility == Accessibility.Public)
                {
                    typeMembers.Add(propertyMember);
                }
            }
        }

        var baseType = typeSymbol.BaseType;

        while (baseType != null)
        {
            foreach (var typeMember in baseType.GetMembers())
            {
                if (typeMember.Kind == SymbolKind.Property && typeMember is IPropertySymbol propertyMember && !propertyMember.IsReadOnly)
                {
                    if (propertyMember.DeclaredAccessibility == Accessibility.Public && propertyMember.SetMethod.DeclaredAccessibility == Accessibility.Public)
                    {
                        typeMembers.Add(propertyMember);
                    }
                }
            }

            baseType = baseType.BaseType;
        }

        return DistinctBy(typeMembers, x => x.Name);
    }

    protected IEnumerable<T> DistinctBy<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        using var enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var set = new HashSet<TKey>();

            do
            {
                var element = enumerator.Current;
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
            while (enumerator.MoveNext());
        }
    }

    protected List<INamedTypeSymbol> FindTypes(Compilation compilation, string includedNamespace)
    {
        return FindTypes(compilation.GlobalNamespace, includedNamespace);
    }

    protected List<INamedTypeSymbol> FindTypes(INamespaceSymbol namespaceSymbol, string includedNamespace)
    {
        var requestTypes = new List<INamedTypeSymbol>();

        if ($"{namespaceSymbol}".ToLower().StartsWith(includedNamespace.ToLower()))
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                if (type.IsType && type.TypeKind == TypeKind.Class && !type.IsStatic)
                {
                    requestTypes.Add(type);
                }
            }
        }

        foreach (var childNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
        {
            requestTypes.AddRange(FindTypes(childNamespaceSymbol, includedNamespace));
        }

        return requestTypes;
    }
}
