using System.Collections.Generic;
using System.Text;

namespace SourceGenerators.Common;

internal class CSharpBuilder
{
    private List<UsingStatement> _usings = new();
    private List<NamespaceBlock> _namespaces = new();

    public CSharpBuilder WithUsing(string namespaceName)
    {
        _usings.Add(new UsingStatement { Namespace = namespaceName });

        return this;
    }

    public CSharpNamespaceBuilder WithNamespace(string namespaceName)
    {
        var namespaceBlock = new NamespaceBlock { Name = namespaceName };
        var namespaceBuilder = new CSharpNamespaceBuilder(this, namespaceBlock);

        _namespaces.Add(namespaceBlock);

        return namespaceBuilder;
    }

    public string Build()
    {
        var stringBuilder = new StringBuilder();

        // Build the using statements.
        foreach (var usingStatement in _usings)
        {
            stringBuilder.AppendLine(usingStatement.Build());
        }

        stringBuilder.AppendLine();

        // Build the namespaces
        foreach (var namespaceBlock in _namespaces)
        {
            stringBuilder.AppendLine(namespaceBlock.Build());
        }

        return stringBuilder.ToString();
    }
}

internal class CSharpNamespaceBuilder
{
    private readonly CSharpBuilder _builder;
    private readonly NamespaceBlock _namespaceBlock;

    public string NamespaceName { get; set; }

    public CSharpNamespaceBuilder(CSharpBuilder builder, NamespaceBlock namespaceBlock)
    {
        _builder = builder;
        _namespaceBlock = namespaceBlock;
    }

    public CSharpClassBuilder WithSealedClass(string className, string baseType = null)
    {
        var classBlock = new ClassBlock { IsPartial = false, IsSealed = true, Indentation = "    ", Name = className, Base = baseType };
        var classBuilder = new CSharpClassBuilder(this, classBlock);

        _namespaceBlock.Classes.Add(classBlock);

        return classBuilder;
    }

    public CSharpClassBuilder WithPartialClass(string className, string baseType = null)
    {
        var classBlock = new ClassBlock { IsPartial = true, Indentation = "    ", Name = className, Base = baseType };
        var classBuilder = new CSharpClassBuilder(this, classBlock);

        _namespaceBlock.Classes.Add(classBlock);

        return classBuilder;
    }

    public CSharpBuilder Finish()
    {
        return _builder;
    }
}

internal class CSharpClassBuilder
{
    private readonly CSharpNamespaceBuilder _namespaceBuilder;
    private readonly ClassBlock _classBlock;

    public CSharpClassBuilder(CSharpNamespaceBuilder namespaceBuilder, ClassBlock classBlock)
    {
        _namespaceBuilder = namespaceBuilder;
        _classBlock = classBlock;
    }

    public CSharpPropertyBuilder WithProtectedProperty(string type, string name)
    {
        var propertyBlock = new PropertyBlock { Indentation = $"{_classBlock.Indentation}    ", AccessModifier = "protected", Type = type, Name = name };

        _classBlock.Properties.Add(propertyBlock);

        return new CSharpPropertyBuilder(this, propertyBlock);
    }

    public CSharpPropertyBuilder WithPublicProperty(string type, string name)
    {
        var propertyBlock = new PropertyBlock { Indentation = $"{_classBlock.Indentation}    ", AccessModifier = "public", Type = type, Name = name };

        _classBlock.Properties.Add(propertyBlock);

        return new CSharpPropertyBuilder(this, propertyBlock);
    }

    public CSharpMethodBuilder WithVirtualMethod(string returnType, string name)
    {
        return WithMethod(returnType, name, isVirtual: true);
    }

    public CSharpMethodBuilder WithMethodOverride(string returnType, string name)
    {
        return WithMethod(returnType, name, isOverride: true);
    }

    public CSharpMethodBuilder WithMethod(string returnType, string name, bool isVirtual = false, bool isOverride = false)
    {
        var methodBlock = new MethodBlock
        {
            Indentation = $"{_classBlock.Indentation}    ",
            IsVirtual = isVirtual,
            IsOverride = isOverride,
            ReturnType = returnType,
            Name = name
        };

        var methodBuilder = new CSharpMethodBuilder(this, methodBlock);

        _classBlock.Methods.Add(methodBlock);

        return methodBuilder;
    }

    public CSharpNamespaceBuilder End()
    {
        return _namespaceBuilder;
    }
}

internal class CSharpPropertyBuilder
{
    private readonly CSharpClassBuilder _builder;
    private readonly PropertyBlock _propertyBlock;

    public CSharpPropertyBuilder(CSharpClassBuilder builder, PropertyBlock propertyBlock)
    {
        _builder = builder;
        _propertyBlock = propertyBlock;
    }

    public CSharpPropertyBuilder WithInitializer(string initializer)
    {
        _propertyBlock.Initializer = initializer;

        return this;
    }
    public CSharpPropertyBuilder WithConst()
    {
        _propertyBlock.IsConst = true;

        return this;
    }
    public CSharpPropertyBuilder WithField()
    {
        _propertyBlock.IsField = true;

        return this;
    }
}

internal class CSharpMethodBuilder
{
    private readonly CSharpClassBuilder _builder;
    private readonly MethodBlock _methodBlock;

    public CSharpMethodBuilder(CSharpClassBuilder builder, MethodBlock methodBlock)
    {
        _builder = builder;
        _methodBlock = methodBlock;
    }

    public CSharpMethodBuilder WithParameter(string parameterType, string name)
    {
        _methodBlock.Parameters.Add(new MethodParameter { Type = parameterType, Name = name });

        return this;
    }

    public CSharpMethodBuilder WithParams(string parameterType, string name)
    {
        _methodBlock.Parameters.Add(new MethodParams { Type = parameterType, Name = name });

        return this;
    }

    public CSharpMethodBuilder WithStatement(string statement)
    {
        _methodBlock.Statements.Add(new Statement { Indentation = $"{_methodBlock.Indentation}    ", LineStatement = statement });

        return this;
    }

    public CSharpMethodBuilder WithEmptyStatement()
    {
        _methodBlock.Statements.Add(new EmptyStatement { Indentation = $"{_methodBlock.Indentation}    " });

        return this;
    }

    public CSharpBlockStatementBuilder WithBlockStatement(string statement)
    {
        var blockStatement = new BlockStatement { Indentation = $"{_methodBlock.Indentation}    ", LineStatement = statement };
        var blockBuilder = new CSharpBlockStatementBuilder(this, blockStatement);

        _methodBlock.Statements.Add(blockStatement);

        return blockBuilder;
    }
}

internal class CSharpBlockStatementBuilder
{
    private readonly CSharpMethodBuilder _builder;
    private readonly BlockStatement _blockStatement;

    public CSharpBlockStatementBuilder(CSharpMethodBuilder builder, BlockStatement blockStatement)
    {
        _builder = builder;
        _blockStatement = blockStatement;
    }

    public CSharpBlockStatementBuilder WithStatement(string statement)
    {
        _blockStatement.Statements.Add(new Statement { Indentation = $"{_blockStatement.Indentation}    ", LineStatement = statement });

        return this;
    }

    public CSharpBlockStatementBuilder WithEmptyStatement()
    {
        _blockStatement.Statements.Add(new EmptyStatement { Indentation = $"{_blockStatement.Indentation}    " });

        return this;
    }

    public CSharpMethodBuilder End()
    {
        return _builder;
    }
}

internal class UsingStatement
{
    public string Namespace { get; set; }

    public string Build()
    {
        return $"using {Namespace};";
    }
}

internal class NamespaceBlock
{
    public string Name { get; set; }

    public List<ClassBlock> Classes { get; } = new List<ClassBlock>();

    public string Build()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"namespace {Name}");
        stringBuilder.AppendLine("{");

        foreach (var classBlock in Classes)
        {
            stringBuilder.AppendLine(classBlock.Build());
        }

        stringBuilder.AppendLine("}");

        return stringBuilder.ToString();
    }
}

internal class ClassBlock
{
    public bool IsPartial { get; set; }
    public bool IsSealed { get; set; }
    public string Indentation { get; set; }
    public string Name { get; set; }
    public string Base { get; set; }

    public List<PropertyBlock> Properties { get; } = new List<PropertyBlock>();
    public List<MethodBlock> Methods { get; } = new List<MethodBlock>();

    public string Build()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"{Indentation}public {(IsPartial ? "partial " : IsSealed ? "sealed " : "")}class {Name}");

        if (!string.IsNullOrEmpty(Base))
        {
            stringBuilder.AppendLine($" : {Base}");
        }

        stringBuilder.AppendLine($"{Indentation}{{");

        foreach (var propertyBlock in Properties)
        {
            stringBuilder.AppendLine(propertyBlock.Build());
        }

        stringBuilder.AppendLine();

        foreach (var methodBlock in Methods)
        {
            stringBuilder.AppendLine(methodBlock.Build());
        }

        stringBuilder.AppendLine($"{Indentation}}}");

        return stringBuilder.ToString();
    }
}

internal class PropertyBlock
{
    public string Indentation { get; set; }
    public string AccessModifier { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Initializer { get; set; }
    public bool IsConst { get; set; }
    public bool IsField { get; set; }

    public string Build()
    {
        return $"{Indentation}{AccessModifier} {(IsConst ? "const" : "")} {Type} {Name} {(IsField ? "" : "{ get; set; }")} {(Initializer != null ? $"= {Initializer};" : "")}";
    }
}

internal class MethodBlock
{
    public string Indentation { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public string ReturnType { get; set; }
    public string Name { get; set; }
    public List<MethodParameter> Parameters { get; } = new List<MethodParameter>();
    public List<Statement> Statements { get; } = new List<Statement>();

    public string Build()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"{Indentation}public {(IsVirtual ? "virtual " : "")}{(IsOverride ? "override " : "")}{ReturnType} {Name}(");

        for (int i = 0; i < Parameters.Count; i++)
        {
            stringBuilder.Append(Parameters[i].Build());

            if (i < Parameters.Count - 1)
            {
                stringBuilder.Append(", ");
            }
        }

        stringBuilder.AppendLine(")");
        stringBuilder.AppendLine($"{Indentation}{{");

        foreach (var statement in Statements)
        {
            stringBuilder.AppendLine(statement.Build());
        }

        stringBuilder.AppendLine($"{Indentation}}}");

        return stringBuilder.ToString();
    }
}

internal class MethodParameter
{
    public string Type { get; set; }
    public string Name { get; set; }

    public virtual string Build()
    {
        return $"{Type} {Name}";
    }
}

internal class MethodParams : MethodParameter
{
    public override string Build()
    {
        return $"params {Type} {Name}";
    }
}

internal class Statement
{
    public string Indentation { get; set; }
    public string LineStatement { get; set; }

    public virtual string Build()
    {
        return $"{Indentation}{LineStatement};";
    }
}

internal class EmptyStatement : Statement
{
    public override string Build()
    {
        return $"{Indentation}";
    }
}

internal class BlockStatement : Statement
{
    public List<Statement> Statements { get; } = new List<Statement>();

    public override string Build()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"{Indentation}{LineStatement}");
        stringBuilder.AppendLine($"{Indentation}{{");

        foreach (var statement in Statements)
        {
            stringBuilder.AppendLine(statement.Build());
        }

        stringBuilder.AppendLine($"{Indentation}}}");

        return stringBuilder.ToString();
    }
}
