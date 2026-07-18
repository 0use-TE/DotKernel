using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotKernel.Generators;

[Generator]
public sealed class PluginSourceGenerator : IIncrementalGenerator
{
    private const string KernelPluginAttribute = "DotKernel.KernelPluginAttribute";
    private const string KernelPromptClassAttribute = "DotKernel.KernelPromptClassAttribute";
    private const string KernelFunctionAttribute = "DotKernel.KernelFunctionAttribute";
    private const string KernelPromptAttribute = "DotKernel.KernelPromptAttribute";
    private const string PromptVariableAttribute = "DotKernel.PromptVariableAttribute";
    private const string KernelDescriptionAttribute = "DotKernel.KernelDescriptionAttribute";
    private const string KernelFilterAttribute = "DotKernel.KernelFilterAttribute";
    private const string KernelPropertyAttribute = "DotKernel.KernelPropertyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var plugins = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KernelPluginAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetPluginModel(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var promptClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KernelPromptClassAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetPromptClassModel(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var filters = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KernelFilterAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetFilterModel(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(plugins, static (spc, model) => EmitPlugin(spc, model));
        context.RegisterSourceOutput(promptClasses, static (spc, model) => EmitPromptClass(spc, model));
        context.RegisterSourceOutput(filters, static (spc, model) => EmitFilter(spc, model));
    }

    private static PluginModel? GetPluginModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var pluginName = GetPluginName(typeSymbol, context.Attributes);
        var functions = GetFunctions(typeSymbol, pluginName);
        var prompts = GetPrompts(typeSymbol, pluginName);
        var variables = GetVariables(typeSymbol);
        var contextProperties = GetContextProperties(typeSymbol);

        if (functions.IsEmpty && prompts.IsEmpty && contextProperties.IsEmpty)
        {
            return null;
        }

        return new PluginModel(
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.Name,
            GetNamespace(typeSymbol),
            pluginName,
            functions,
            prompts,
            variables,
            contextProperties,
            IsPartial(typeSymbol));
    }

    private static PromptClassModel? GetPromptClassModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (typeSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == KernelPluginAttribute))
        {
            return null;
        }

        var pluginName = GetPromptClassName(typeSymbol, context.Attributes);
        var prompts = GetPrompts(typeSymbol, pluginName);
        var variables = GetVariables(typeSymbol);

        if (prompts.IsEmpty)
        {
            return null;
        }

        return new PromptClassModel(
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.Name,
            GetNamespace(typeSymbol),
            pluginName,
            prompts,
            variables,
            IsPartial(typeSymbol));
    }

    private static FilterModel? GetFilterModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var priority = 0;
        foreach (var arg in context.Attributes.SelectMany(a => a.NamedArguments))
        {
            if (arg.Key == "Priority" && arg.Value.Value is int p)
            {
                priority = p;
            }
        }

        return new FilterModel(
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.Name,
            GetNamespace(typeSymbol),
            priority,
            IsPartial(typeSymbol));
    }

    private static string? GetNamespace(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingNamespace is null || typeSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            return null;
        }

        var ns = typeSymbol.ContainingNamespace.ToDisplayString();
        return string.IsNullOrEmpty(ns) ? null : ns;
    }

    private static void AppendFileHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
    }

    private static void AppendNamespace(StringBuilder sb, string? namespaceName)
    {
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }
    }

    private static string GetPluginName(INamedTypeSymbol typeSymbol, ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
            {
                return name;
            }
        }

        return typeSymbol.Name;
    }

    private static string GetPromptClassName(INamedTypeSymbol typeSymbol, ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
            {
                return name;
            }
        }

        return typeSymbol.Name;
    }

    private static ImmutableArray<FunctionModel> GetFunctions(INamedTypeSymbol typeSymbol, string pluginName)
    {
        var list = ImmutableArray.CreateBuilder<FunctionModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
            {
                continue;
            }

            var fnAttr = method.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == KernelFunctionAttribute);
            if (fnAttr is null)
            {
                continue;
            }

            var functionName = fnAttr.ConstructorArguments.Length > 0 && fnAttr.ConstructorArguments[0].Value is string n && !string.IsNullOrWhiteSpace(n)
                ? n
                : ToCamelCase(method.Name);

            var description = GetDescription(method);
            var parameters = method.Parameters
                .Where(p => !IsInjectedParameter(p))
                .Select(p => new ParameterModel(
                    p.Name,
                    MapJsonType(p.Type),
                    GetDescription(p),
                    !p.IsOptional && !p.HasExplicitDefaultValue))
                .ToImmutableArray();

            list.Add(new FunctionModel(
                method.Name,
                functionName,
                description,
                parameters,
                method.IsStatic,
                IsAsyncMethod(method),
                GetReturnKind(method.ReturnType),
                method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        return list.ToImmutable();
    }

    private static ImmutableArray<PromptModel> GetPrompts(INamedTypeSymbol typeSymbol, string pluginName)
    {
        var list = ImmutableArray.CreateBuilder<PromptModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            var promptAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == KernelPromptAttribute);
            if (promptAttr is null)
            {
                continue;
            }

            if (promptAttr.ConstructorArguments.Length == 0 || promptAttr.ConstructorArguments[0].Value is not string promptName)
            {
                continue;
            }

            var description = GetDescription(property);
            var role = "DotKernel.PromptRole.User";
            foreach (var named in promptAttr.NamedArguments)
            {
                if (named.Key == "Description" && named.Value.Value is string d)
                {
                    description = d;
                }
                else if (named.Key == "Role" && named.Value.Value is int roleValue)
                {
                    role = roleValue switch
                    {
                        0 => "DotKernel.PromptRole.System",
                        2 => "DotKernel.PromptRole.Assistant",
                        _ => "DotKernel.PromptRole.User",
                    };
                }
            }

            list.Add(new PromptModel(
                property.Name,
                promptName,
                description,
                role,
                property.IsStatic));
        }

        return list.ToImmutable();
    }

    private static ImmutableArray<VariableModel> GetVariables(INamedTypeSymbol typeSymbol)
    {
        var list = ImmutableArray.CreateBuilder<VariableModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            var varAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == PromptVariableAttribute);
            if (varAttr is null)
            {
                continue;
            }

            string? description = null;
            string? defaultValue = null;
            var required = true;

            foreach (var named in varAttr.NamedArguments)
            {
                switch (named.Key)
                {
                    case "Description" when named.Value.Value is string d:
                        description = d;
                        break;
                    case "Default" when named.Value.Value is string dv:
                        defaultValue = dv;
                        break;
                    case "Required" when named.Value.Value is bool r:
                        required = r;
                        break;
                }
            }

            list.Add(new VariableModel(property.Name, description, defaultValue, required));
        }

        return list.ToImmutable();
    }

    private static ImmutableArray<ContextPropertyModel> GetContextProperties(INamedTypeSymbol typeSymbol)
    {
        var list = ImmutableArray.CreateBuilder<ContextPropertyModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property || property.IsIndexer || property.GetMethod is null)
            {
                continue;
            }

            var attr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == KernelPropertyAttribute);
            if (attr is null)
            {
                continue;
            }

            string? name = null;
            string? description = null;

            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string n)
            {
                name = n;
            }

            if (attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is string d)
            {
                description = d;
            }

            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "Description" && named.Value.Value is string nd)
                {
                    description = nd;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = property.Name;
            }

            list.Add(new ContextPropertyModel(property.Name, name!, description, property.IsStatic));
        }

        return list.ToImmutable();
    }

    private static void EmitPlugin(SourceProductionContext context, PluginModel model)
    {
        if (!model.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DK001",
                    "Plugin must be partial",
                    "Class '{0}' marked with [KernelPlugin] must be declared partial.",
                    "DotKernel",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                model.TypeName));
            return;
        }

        var sb = new StringBuilder();
        AppendFileHeader(sb);
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        AppendNamespace(sb, model.Namespace);
        sb.AppendLine($"partial class {model.ShortName} : global::DotKernel.IKernelPluginRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Register(global::DotKernel.IKernelBuilder builder)");
        sb.AppendLine("    {");

        foreach (var function in model.Functions)
        {
            var schema = BuildSchema(function.Parameters);
            sb.AppendLine("        builder.AddFunction(new global::DotKernel.KernelFunctionDescriptor");
            sb.AppendLine("        {");
            sb.AppendLine($"            PluginName = \"{Escape(model.PluginName)}\",");
            sb.AppendLine($"            FunctionName = \"{Escape(function.FunctionName)}\",");
            sb.AppendLine($"            Description = {(function.Description is null ? "null" : $"\"{Escape(function.Description)}\"")},");
            sb.AppendLine($"            ParametersSchemaJson = \"{Escape(schema)}\",");
            sb.AppendLine($"            DeclaringType = typeof({model.FullyQualifiedType}),");
            sb.AppendLine($"            Invoker = {model.ShortName}.{function.MethodName}_Invoker,");
            sb.AppendLine("        });");
        }

        foreach (var prompt in model.Prompts)
        {
            EmitPromptRegistration(sb, model.PluginName, model.FullyQualifiedType, prompt, model.Variables);
        }

        foreach (var property in model.ContextProperties)
        {
            sb.AppendLine("        builder.AddProperty(new global::DotKernel.KernelPropertyDescriptor");
            sb.AppendLine("        {");
            sb.AppendLine($"            PluginName = \"{Escape(model.PluginName)}\",");
            sb.AppendLine($"            PropertyName = \"{Escape(property.ContextName)}\",");
            sb.AppendLine($"            Description = {(property.Description is null ? "null" : $"\"{Escape(property.Description)}\"")},");
            sb.AppendLine($"            DeclaringType = typeof({model.FullyQualifiedType}),");
            if (property.IsStatic)
            {
                sb.AppendLine($"            Getter = static _ => {model.FullyQualifiedType}.{property.PropertyName},");
            }
            else
            {
                sb.AppendLine($"            Getter = static instance => (({model.FullyQualifiedType})instance!).{property.PropertyName},");
            }

            sb.AppendLine("        });");
        }

        sb.AppendLine("    }");

        foreach (var function in model.Functions)
        {
            EmitInvoker(sb, model, function);
        }

        sb.AppendLine("}");

        context.AddSource($"{model.ShortName}.DotKernel.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitPromptClass(SourceProductionContext context, PromptClassModel model)
    {
        if (!model.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DK002",
                    "Prompt class must be partial",
                    "Class '{0}' marked with [KernelPromptClass] must be declared partial.",
                    "DotKernel",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                model.TypeName));
            return;
        }

        var sb = new StringBuilder();
        AppendFileHeader(sb);
        AppendNamespace(sb, model.Namespace);
        sb.AppendLine($"partial class {model.ShortName} : global::DotKernel.IKernelPluginRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Register(global::DotKernel.IKernelBuilder builder)");
        sb.AppendLine("    {");

        foreach (var prompt in model.Prompts)
        {
            EmitPromptRegistration(sb, model.PluginName, model.FullyQualifiedType, prompt, model.Variables);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"{model.ShortName}.DotKernel.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitFilter(SourceProductionContext context, FilterModel model)
    {
        if (!model.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DK003",
                    "Filter must be partial",
                    "Class '{0}' marked with [KernelFilter] must be declared partial.",
                    "DotKernel",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                model.TypeName));
            return;
        }

        var sb = new StringBuilder();
        AppendFileHeader(sb);
        AppendNamespace(sb, model.Namespace);
        sb.AppendLine($"partial class {model.ShortName} : global::DotKernel.IKernelFilterRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Register(global::DotKernel.IKernelBuilder builder, global::DotKernel.IKernelFilter instance)");
        sb.AppendLine("    {");
        sb.AppendLine("        builder.AddFilter(instance);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"{model.ShortName}.DotKernel.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitPromptRegistration(
        StringBuilder sb,
        string pluginName,
        string fullyQualifiedType,
        PromptModel prompt,
        ImmutableArray<VariableModel> variables)
    {
        sb.AppendLine("        builder.AddPrompt(new global::DotKernel.PromptDefinition");
        sb.AppendLine("        {");
        sb.AppendLine($"            PluginName = \"{Escape(pluginName)}\",");
        sb.AppendLine($"            PromptName = \"{Escape(prompt.PromptName)}\",");
        sb.AppendLine($"            Description = {(prompt.Description is null ? "null" : $"\"{Escape(prompt.Description)}\"")},");
        sb.AppendLine($"            Role = {prompt.Role},");
        sb.AppendLine($"            DeclaringType = typeof({fullyQualifiedType}),");

        if (prompt.IsStatic)
        {
            sb.AppendLine($"            GetTemplate = static _ => {fullyQualifiedType}.{prompt.PropertyName},");
        }
        else
        {
            sb.AppendLine($"            GetTemplate = static instance => (({fullyQualifiedType})instance!).{prompt.PropertyName},");
        }
        sb.AppendLine("            Variables = new global::DotKernel.PromptVariableDefinition[]");
        sb.AppendLine("            {");

        foreach (var variable in variables)
        {
            sb.AppendLine("                new global::DotKernel.PromptVariableDefinition");
            sb.AppendLine("                {");
            sb.AppendLine($"                    Name = \"{Escape(variable.Name)}\",");
            sb.AppendLine($"                    Description = {(variable.Description is null ? "null" : $"\"{Escape(variable.Description)}\"")},");
            sb.AppendLine($"                    Default = {(variable.Default is null ? "null" : $"\"{Escape(variable.Default)}\"")},");
            sb.AppendLine($"                    Required = {(variable.Required ? "true" : "false")},");
            sb.AppendLine($"                    Getter = static instance => (({fullyQualifiedType})instance!).{variable.Name},");
            sb.AppendLine("                },");
        }

        sb.AppendLine("            },");
        sb.AppendLine("        });");
    }

    private static void EmitInvoker(StringBuilder sb, PluginModel model, FunctionModel function)
    {
        sb.AppendLine();
        sb.AppendLine($"    private static async global::System.Threading.Tasks.ValueTask<object?> {function.MethodName}_Invoker(");
        sb.AppendLine("        global::DotKernel.FunctionInvocationContext context,");
        sb.AppendLine("        global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine("    {");

        foreach (var parameter in function.Parameters)
        {
            sb.AppendLine($"        var {parameter.Name} = context.GetArgument<{MapClrType(parameter.JsonType)}>(\"{Escape(parameter.Name)}\");");
        }

        var args = string.Join(", ", function.Parameters.Select(p => p.Name).Concat(["cancellationToken"]));
        var target = function.IsStatic ? model.FullyQualifiedType : $"context.GetPlugin<{model.FullyQualifiedType}>()";

        if (function.IsAsync)
        {
            if (function.ReturnKind == ReturnKind.Void)
            {
                sb.AppendLine($"        await {target}.{function.MethodName}({args}).ConfigureAwait(false);");
                sb.AppendLine("        return null;");
            }
            else
            {
                sb.AppendLine($"        return await {target}.{function.MethodName}({args}).ConfigureAwait(false);");
            }
        }
        else
        {
            sb.AppendLine($"        return {target}.{function.MethodName}({args});");
        }

        sb.AppendLine("    }");
    }

    private static string BuildSchema(ImmutableArray<ParameterModel> parameters)
    {
        if (parameters.IsDefaultOrEmpty)
        {
            return """{"type":"object","properties":{}}""";
        }

        var props = new StringBuilder();
        var required = new List<string>();

        foreach (var p in parameters)
        {
            if (props.Length > 0)
            {
                props.Append(',');
            }

            props.Append($"\"{Escape(p.Name)}\":{{\"type\":\"{p.JsonType}\"");
            if (p.Description is not null)
            {
                props.Append($",\"description\":\"{Escape(p.Description)}\"");
            }

            props.Append('}');

            if (p.Required)
            {
                required.Add(p.Name);
            }
        }

        var requiredPart = required.Count > 0
            ? $",\"required\":[{string.Join(",", required.Select(r => $"\"{Escape(r)}\""))}]"
            : string.Empty;

        return $"{{\"type\":\"object\",\"properties\":{{{props}}}{requiredPart}}}";
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ClassDeclarationSyntax classDecl &&
                classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInjectedParameter(IParameterSymbol parameter) =>
        parameter.Type.ToDisplayString() switch
        {
            "System.Threading.CancellationToken" => true,
            "DotKernel.Kernel" => true,
            "Microsoft.Extensions.AI.IChatClient" => true,
            _ when parameter.Type.Name == "IServiceProvider" => true,
            _ => false,
        };

    private static bool IsAsyncMethod(IMethodSymbol method) =>
        method.ReturnType is INamedTypeSymbol named && IsAsyncReturnType(named, out _);

    private static ReturnKind GetReturnKind(ITypeSymbol returnType)
    {
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return ReturnKind.Void;
        }

        if (returnType is INamedTypeSymbol named && IsAsyncReturnType(named, out var hasResult))
        {
            return hasResult ? ReturnKind.TaskOfT : ReturnKind.Void;
        }

        return ReturnKind.Sync;
    }

    private static bool IsAsyncReturnType(INamedTypeSymbol named, out bool hasResult)
    {
        hasResult = named.TypeArguments.Length > 0;
        var constructed = named.ConstructedFrom;
        return constructed.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks" &&
            constructed.Name is "Task" or "ValueTask";
    }

    private static string MapJsonType(ITypeSymbol type)
    {
        return type.ToDisplayString() switch
        {
            "int" or "long" or "short" or "byte" or "float" or "double" or "decimal" => "number",
            "bool" => "boolean",
            _ => "string",
        };
    }

    private static string MapClrType(string jsonType) => jsonType switch
    {
        "number" => "double",
        "boolean" => "bool",
        _ => "string",
    };

    private static string? GetDescription(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == KernelDescriptionAttribute &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string description)
            {
                return description;
            }
        }

        return null;
    }

    private static string ToCamelCase(string name)
    {
        if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > 5)
        {
            name = name.Substring(0, name.Length - 5);
        }

        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r", "\\r")
        .Replace("\n", "\\n");

    private sealed class PluginModel
    {
        public PluginModel(
            string fullyQualifiedType,
            string shortName,
            string? namespaceName,
            string pluginName,
            ImmutableArray<FunctionModel> functions,
            ImmutableArray<PromptModel> prompts,
            ImmutableArray<VariableModel> variables,
            ImmutableArray<ContextPropertyModel> contextProperties,
            bool isPartial)
        {
            FullyQualifiedType = fullyQualifiedType;
            ShortName = shortName;
            Namespace = namespaceName;
            PluginName = pluginName;
            Functions = functions;
            Prompts = prompts;
            Variables = variables;
            ContextProperties = contextProperties;
            IsPartial = isPartial;
        }

        public string FullyQualifiedType { get; }
        public string ShortName { get; }
        public string? Namespace { get; }
        public string TypeName => ShortName;
        public string PluginName { get; }
        public ImmutableArray<FunctionModel> Functions { get; }
        public ImmutableArray<PromptModel> Prompts { get; }
        public ImmutableArray<VariableModel> Variables { get; }
        public ImmutableArray<ContextPropertyModel> ContextProperties { get; }
        public bool IsPartial { get; }
    }

    private sealed class PromptClassModel
    {
        public PromptClassModel(
            string fullyQualifiedType,
            string shortName,
            string? namespaceName,
            string pluginName,
            ImmutableArray<PromptModel> prompts,
            ImmutableArray<VariableModel> variables,
            bool isPartial)
        {
            FullyQualifiedType = fullyQualifiedType;
            ShortName = shortName;
            Namespace = namespaceName;
            PluginName = pluginName;
            Prompts = prompts;
            Variables = variables;
            IsPartial = isPartial;
        }

        public string FullyQualifiedType { get; }
        public string ShortName { get; }
        public string? Namespace { get; }
        public string TypeName => ShortName;
        public string PluginName { get; }
        public ImmutableArray<PromptModel> Prompts { get; }
        public ImmutableArray<VariableModel> Variables { get; }
        public bool IsPartial { get; }
    }

    private sealed class FilterModel
    {
        public FilterModel(string fullyQualifiedType, string shortName, string? namespaceName, int priority, bool isPartial)
        {
            FullyQualifiedType = fullyQualifiedType;
            ShortName = shortName;
            Namespace = namespaceName;
            Priority = priority;
            IsPartial = isPartial;
        }

        public string FullyQualifiedType { get; }
        public string ShortName { get; }
        public string? Namespace { get; }
        public string TypeName => ShortName;
        public int Priority { get; }
        public bool IsPartial { get; }
    }

    private sealed class FunctionModel
    {
        public FunctionModel(
            string methodName,
            string functionName,
            string? description,
            ImmutableArray<ParameterModel> parameters,
            bool isStatic,
            bool isAsync,
            ReturnKind returnKind,
            string fullyQualifiedMethod)
        {
            MethodName = methodName;
            FunctionName = functionName;
            Description = description;
            Parameters = parameters;
            IsStatic = isStatic;
            IsAsync = isAsync;
            ReturnKind = returnKind;
            FullyQualifiedMethod = fullyQualifiedMethod;
        }

        public string MethodName { get; }
        public string FunctionName { get; }
        public string? Description { get; }
        public ImmutableArray<ParameterModel> Parameters { get; }
        public bool IsStatic { get; }
        public bool IsAsync { get; }
        public ReturnKind ReturnKind { get; }
        public string FullyQualifiedMethod { get; }
    }

    private sealed class ParameterModel
    {
        public ParameterModel(string name, string jsonType, string? description, bool required)
        {
            Name = name;
            JsonType = jsonType;
            Description = description;
            Required = required;
        }

        public string Name { get; }
        public string JsonType { get; }
        public string? Description { get; }
        public bool Required { get; }
    }

    private sealed class PromptModel
    {
        public PromptModel(string propertyName, string promptName, string? description, string role, bool isStatic)
        {
            PropertyName = propertyName;
            PromptName = promptName;
            Description = description;
            Role = role;
            IsStatic = isStatic;
        }

        public string PropertyName { get; }
        public string PromptName { get; }
        public string? Description { get; }
        public string Role { get; }
        public bool IsStatic { get; }
    }

    private sealed class VariableModel
    {
        public VariableModel(string name, string? description, string? defaultValue, bool required)
        {
            Name = name;
            Description = description;
            Default = defaultValue;
            Required = required;
        }

        public string Name { get; }
        public string? Description { get; }
        public string? Default { get; }
        public bool Required { get; }
    }

    private sealed class ContextPropertyModel
    {
        public ContextPropertyModel(string propertyName, string contextName, string? description, bool isStatic)
        {
            PropertyName = propertyName;
            ContextName = contextName;
            Description = description;
            IsStatic = isStatic;
        }

        public string PropertyName { get; }
        public string ContextName { get; }
        public string? Description { get; }
        public bool IsStatic { get; }
    }

    private enum ReturnKind
    {
        Void,
        TaskOfT,
        Sync,
    }
}
