using Microsoft.CodeAnalysis;
using PacketGen.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PacketGen;

internal class PacketGenerators
{
    public static string? GetSource(Compilation compilation, INamedTypeSymbol symbol)
    {
        List<IPropertySymbol> properties = [];
        bool hasWriteReadMethods = false;

        foreach (ISymbol member in symbol.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                ImmutableArray<AttributeData> attributes = property.GetAttributes();

                // Ignore properties with the [NetExclude] attribute
                if (attributes.Any(attr => attr.AttributeClass?.Name == "NetExcludeAttribute"))
                    continue;

                properties.Add(property);
            }
            else if (member is IMethodSymbol method)
            {
                // Do not generate anything if Write or Read methods exist already
                if (method.Name == "Write" || method.Name == "Read")
                    hasWriteReadMethods = true;
            }
        }

        if (hasWriteReadMethods || properties.Count == 0)
            return null;

        string namespaceName = symbol.ContainingNamespace.ToDisplayString();
        string className = symbol.Name;

        List<string> writeLines = [];
        List<string> readLines = [];
        HashSet<string> namespaces = [];

        foreach (IPropertySymbol property in properties)
        {
            GenerateWrite(new GenerateWriteContext(compilation, property, property.Type, writeLines), property.Name, "");
        }

        foreach (IPropertySymbol property in properties)
        {
            GenerateRead(new GenerateReadContext(property, property.Type, property.Name, readLines, namespaces), "");
        }

        string usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));
        string indent8 = "        ";
        string sourceCode = $$"""
{{usings}}
namespace {{namespaceName}};

public partial class {{className}}
{
    public override void Write(PacketWriter writer)
    {
{{string.Join("\n", writeLines.Select(line => indent8 + line))}}
    }

    public override void Read(PacketReader reader)
    {
{{string.Join("\n", readLines.Select(line => indent8 + line))}}
    }s
}

""";

        return sourceCode;
    }

    private static void GenerateWrite(GenerateWriteContext ctx,
        string valueExpression,
        string indent,
        int depth = 0)
    {
        string? suffix = ReadMethodSuffix.Get(ctx.Type);

        // Primitive
        if (suffix != null)
        {
            ctx.WriteLines.Add($"{indent}writer.Write({valueExpression});");
            return;
        }

        // INamedTypeSymbol gives access to properties we need to check
        // - IsGenericType: e.g. List<T> or Dictionary<TKey, TValue>
        // - IsUnboundGenericType: e.g. List<int> 
        if (ctx.Type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return;

        // List<T>
        if (namedType.Name == "List")
        {
            ITypeSymbol elementType = namedType.TypeArguments[0];

            string countVar = $"{valueExpression}.Count";
            string loopIndex = $"i{depth}";
            string elementAccess = $"{valueExpression}[{loopIndex}]";

            if (depth == 0)
                ctx.WriteLines.Add($"{indent}#region {valueExpression}");

            ctx.WriteLines.Add($"{indent}writer.Write({countVar});");
            ctx.WriteLines.Add("");

            ctx.WriteLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
            ctx.WriteLines.Add($"{indent}{{");

            string? elementSuffix = ReadMethodSuffix.Get(elementType);

            if (elementSuffix != null)
            {
                ctx.WriteLines.Add($"{indent}    writer.Write({elementAccess});");
            }
            else
            {
                GenerateWrite(ctx, valueExpression, indent + "    ", depth + 1);
            }

            ctx.WriteLines.Add($"{indent}}}");

            if (depth == 0)
                ctx.WriteLines.Add($"{indent}#endregion");

            return;
        }

        // Dictionary<TKey, TValue>
        if (namedType.Name == "Dictionary")
        {
            ITypeSymbol keyType = namedType.TypeArguments[0];
            ITypeSymbol valueType = namedType.TypeArguments[1];

            string kvVar = $"kv{depth}";

            if (depth == 0)
                ctx.WriteLines.Add($"{indent}#region {valueExpression}");

            ctx.WriteLines.Add($"{indent}// {valueExpression}");
            ctx.WriteLines.Add($"{indent}writer.Write({valueExpression}.Count);");
            ctx.WriteLines.Add("");

            ctx.WriteLines.Add($"{indent}foreach (var {kvVar} in {valueExpression})");
            ctx.WriteLines.Add($"{indent}{{");

            GenerateWrite(ctx, $"{kvVar}.Key", indent + "    ", depth + 1);

            ctx.WriteLines.Add("");

            GenerateWrite(ctx, $"{kvVar}.Value", indent + "    ", depth + 1);

            ctx.WriteLines.Add($"{indent}}}");

            if (depth == 0)
                ctx.WriteLines.Add($"{indent}#endregion");

            return;
        }
    }

    private static void GenerateRead(GenerateReadContext ctx,
        string indent,
        int depth = 0,
        string? rootName = null)
    {
        rootName ??= ctx.TargetExpression;

        string? suffix = ReadMethodSuffix.Get(ctx.Type);

        // Primitive
        if (suffix != null)
        {
            ctx.ReadLines.Add($"{indent}{ctx.TargetExpression} = reader.Read{suffix}();");
            return;
        }

        if (ctx.Type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return;

        // List<T>
        if (namedType.Name == "List")
        {
            ctx.Namespaces.Add("System.Collections.Generic");

            ITypeSymbol elementType = namedType.TypeArguments[0];

            // MinimallyQualifiedFormat outputs List<int> instead of System.Collections.Generic.List<int>
            string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            string countVar = depth == 0
                    ? $"{char.ToLowerInvariant(rootName[0])}{rootName.Substring(1)}Count"
                    : $"count{depth}";

            string loopIndex = $"i{depth}";
            string elementVar = $"element{depth}";

            if (depth == 0)
                ctx.ReadLines.Add($"{indent}#region {ctx.TargetExpression}");

            // Create the list
            ctx.ReadLines.Add($"{indent}{ctx.TargetExpression} = new List<{elementTypeName}>();");

            // Keep track of the list count
            ctx.ReadLines.Add($"{indent}int {countVar} = reader.ReadInt();");
            ctx.ReadLines.Add("");

            // Loop
            ctx.ReadLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
            ctx.ReadLines.Add($"{indent}{{");

            string? elementSuffix = ReadMethodSuffix.Get(elementType);

            if (elementSuffix != null)
            {
                // Primitive type
                ctx.ReadLines.Add($"{indent}    {ctx.TargetExpression}.Add(reader.Read{elementSuffix}());");
            }
            else
            {
                // Generic type
                ctx.ReadLines.Add($"{indent}    {elementTypeName} {elementVar} = new {elementTypeName}();");

                GenerateRead(ctx, indent + "    ", depth + 1, rootName);

                ctx.ReadLines.Add("");
                ctx.ReadLines.Add($"{indent}    {ctx.TargetExpression}.Add({elementVar});");
            }

            ctx.ReadLines.Add($"{indent}}}");

            if (depth == 0)
                ctx.ReadLines.Add($"{indent}#endregion");

            return;
        }

        // Dictionary<TKey, TValue>
        if (namedType.Name == "Dictionary")
        {
            ctx.Namespaces.Add("System.Collections.Generic");

            ITypeSymbol keyType = namedType.TypeArguments[0];
            ITypeSymbol valueType = namedType.TypeArguments[1];

            // MinimallyQualifiedFormat outputs List<int> instead of System.Collections.Generic.List<int>
            string keyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            string valueTypeName = valueType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            string countVar = depth == 0
                    ? $"{char.ToLowerInvariant(rootName[0])}{rootName.Substring(1)}Count"
                    : $"count{depth}";

            // Prevent duplicate variable names by using depth as suffix
            string loopIndex = $"i{depth}";
            string keyVar = $"key{depth}";
            string valueVar = $"value{depth}";

            if (depth == 0)
                ctx.ReadLines.Add($"{indent}#region {ctx.TargetExpression}");

            // Create the dictionary
            ctx.ReadLines.Add($"{indent}{ctx.TargetExpression} = new Dictionary<{keyTypeName}, {valueTypeName}>();");

            // Keep track of the dictionary count (assume int is preferred)
            ctx.ReadLines.Add($"{indent}int {countVar} = reader.ReadInt();");
            ctx.ReadLines.Add("");

            // Loop
            ctx.ReadLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
            ctx.ReadLines.Add($"{indent}{{");
            ctx.ReadLines.Add($"{indent}    {keyTypeName} {keyVar};"); // e.g. string key0;
            ctx.ReadLines.Add($"{indent}    {valueTypeName} {valueVar};"); // e.g. int value0;
            ctx.ReadLines.Add("");

            // Read key0
            GenerateRead(ctx, indent + "    ", depth + 1, rootName);

            ctx.ReadLines.Add("");

            // Read value0
            GenerateRead(ctx, indent + "    ", depth + 1, rootName);

            ctx.ReadLines.Add("");
            // Add the key0 and value0 to the dictionary
            ctx.ReadLines.Add($"{indent}    {ctx.TargetExpression}.Add({keyVar}, {valueVar});");
            ctx.ReadLines.Add($"{indent}}}");

            if (depth == 0)
                ctx.ReadLines.Add($"{indent}#endregion");

            return;
        }

        Logger.Info(ctx.Property, $"Type {ctx.Type.ToDisplayString()} is not supported. Implement Write and Read manually.");
    }

    private sealed class GenerateWriteContext(
        Compilation compilation,
        IPropertySymbol property,
        ITypeSymbol type,
        List<string> writeLines)
    {
        public Compilation Compilation { get; } = compilation;
        public IPropertySymbol Property { get; } = property;
        public ITypeSymbol Type { get; } = type;
        public List<string> WriteLines { get; } = writeLines;
    }

    private sealed class GenerateReadContext(
        IPropertySymbol property,
        ITypeSymbol type,
        string targetExpression,
        List<string> readLines,
        HashSet<string> namespaces)
    {
        public IPropertySymbol Property { get; } = property;
        public ITypeSymbol Type { get; } = type;
        public string TargetExpression { get; } = targetExpression;
        public List<string> ReadLines { get; } = readLines;
        public HashSet<string> Namespaces { get; } = namespaces;
    }
}
