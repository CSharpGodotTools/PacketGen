using Microsoft.CodeAnalysis;
using PacketGen.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace PacketGen;

internal class PacketReadWriteMethodsGenerator
{
    public static string? GetSource(SourceProductionContext context, INamedTypeSymbol symbol)
    {
        List<IPropertySymbol> properties = [];
        bool hasWriteReadMethods = false;

        foreach (var member in symbol.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                var attributes = property.GetAttributes();

                // Ignore properties with the [NetExclude] attribute
                if (!attributes.Any(attr => attr.AttributeClass?.Name == "NetExcludeAttribute"))
                {
                    properties.Add(property);
                }
            }

            if (member is IMethodSymbol method)
            {
                // Do not generate anything if Write or Read methods exist already
                if (method.Name == "Write" || method.Name == "Read")
                    hasWriteReadMethods = true;
            }
        }

        if (hasWriteReadMethods || properties.Count == 0)
            return null;

        var namespaceName = symbol.ContainingNamespace.ToDisplayString();
        var className = symbol.Name;

        var writeLines = new List<string>();
        var readLines = new List<string>();
        var namespaces = new HashSet<string>();

        foreach (IPropertySymbol property in properties)
        {
            GenerateWrite(context, property.Type, property.Name, writeLines, "");
        }

        foreach (IPropertySymbol property in properties)
        {
            GenerateRead(context, property.Type, property.Name, readLines, namespaces, "");
        }

        var usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));
        var indent8 = "        ";
        var sourceCode = $$"""
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
    }
}

""";

        return sourceCode;
    }

    private static void GenerateWrite(
        SourceProductionContext context,
        ITypeSymbol type,
        string valueExpression,
        List<string> writeLines,
        string indent,
        int depth = 0)
    {
        string? suffix = ReadMethodSuffix.Get(type);

        // Primitive
        if (suffix != null)
        {
            writeLines.Add($"{indent}writer.Write({valueExpression});");
            return;
        }

        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            context.Info($"Type {type.ToDisplayString()} is not supported. Implement Write manually.");
            return;
        }

        // List<T>
        if (namedType.Name == "List")
        {
            ITypeSymbol elementType = namedType.TypeArguments[0];

            string countVar = $"{valueExpression}.Count";
            string loopIndex = $"i{depth}";
            string elementAccess = $"{valueExpression}[{loopIndex}]";

            if (depth == 0)
                writeLines.Add($"{indent}#region {valueExpression}");

            writeLines.Add($"{indent}writer.Write({countVar});");
            writeLines.Add("");

            writeLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
            writeLines.Add($"{indent}{{");

            string? elementSuffix = ReadMethodSuffix.Get(elementType);

            if (elementSuffix != null)
            {
                writeLines.Add($"{indent}    writer.Write({elementAccess});");
            }
            else
            {
                GenerateWrite(
                    context,
                    elementType,
                    elementAccess,
                    writeLines,
                    indent + "    ",
                    depth + 1);
            }

            writeLines.Add($"{indent}}}");

            if (depth == 0)
                writeLines.Add($"{indent}#endregion");

            return;
        }

        // Dictionary<TKey, TValue>
        if (namedType.Name == "Dictionary")
        {
            ITypeSymbol keyType = namedType.TypeArguments[0];
            ITypeSymbol valueType = namedType.TypeArguments[1];

            string kvVar = $"kv{depth}";

            if (depth == 0)
                writeLines.Add($"{indent}#region {valueExpression}");

            writeLines.Add($"{indent}// {valueExpression}");
            writeLines.Add($"{indent}writer.Write({valueExpression}.Count);");
            writeLines.Add("");

            writeLines.Add($"{indent}foreach (var {kvVar} in {valueExpression})");
            writeLines.Add($"{indent}{{");

            GenerateWrite(
                context,
                keyType,
                $"{kvVar}.Key",
                writeLines,
                indent + "    ",
                depth + 1);

            writeLines.Add("");

            GenerateWrite(
                context,
                valueType,
                $"{kvVar}.Value",
                writeLines,
                indent + "    ",
                depth + 1);

            writeLines.Add($"{indent}}}");

            if (depth == 0)
                writeLines.Add($"{indent}#endregion");

            return;
        }

        context.Info($"Type {type.ToDisplayString()} is not supported. Implement Write manually.");
    }

    private static void GenerateRead(
        SourceProductionContext context,
        ITypeSymbol type,
        string targetExpression,
        List<string> readLines,
        HashSet<string> namespaces,
        string indent,
        int depth = 0,
        string? rootName = null)
    {
        rootName ??= targetExpression;

        string? suffix = ReadMethodSuffix.Get(type);

        // Primitive
        if (suffix != null)
        {
            readLines.Add($"{indent}{targetExpression} = reader.Read{suffix}();");
            return;
        }

        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            context.Info($"Type {type.ToDisplayString()} is not supported. Implement Read manually.");
            return;
        }

        // List<T>
        if (namedType.Name == "List")
        {
            namespaces.Add("System.Collections.Generic");

            ITypeSymbol elementType = namedType.TypeArguments[0];

            // MinimallyQualifiedFormat outputs List<int> instead of System.Collections.Generic.List<int>
            string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            string countVar = depth == 0
                    ? $"{char.ToLowerInvariant(rootName[0])}{rootName.Substring(1)}Count"
                    : $"count{depth}";

            string loopIndex = $"i{depth}";
            string elementVar = $"element{depth}";

            if (depth == 0)
                readLines.Add($"{indent}#region {targetExpression}");

            // Create the list
            readLines.Add($"{indent}{targetExpression} = new List<{elementTypeName}>();");

            // Keep track of the list count
            readLines.Add($"{indent}int {countVar} = reader.ReadInt();");
            readLines.Add("");

            // Loop
            readLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
            readLines.Add($"{indent}{{");

            string? elementSuffix = ReadMethodSuffix.Get(elementType);

            if (elementSuffix != null)
            {
                // Primitive type
                readLines.Add($"{indent}    {targetExpression}.Add(reader.Read{elementSuffix}());");
            }
            else
            {
                // Generic type
                readLines.Add($"{indent}    {elementTypeName} {elementVar} = new {elementTypeName}();");

                GenerateRead(
                    context,
                    elementType,
                    elementVar,
                    readLines,
                    namespaces,
                    indent + "    ",
                    depth + 1,
                    rootName);

                readLines.Add("");
                readLines.Add($"{indent}    {targetExpression}.Add({elementVar});");
            }

            readLines.Add($"{indent}}}");

            if (depth == 0)
                readLines.Add($"{indent}#endregion");

            return;
        }

        // Dictionary<TKey, TValue>
        if (namedType.Name == "Dictionary")
        {
            namespaces.Add("System.Collections.Generic");

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
                readLines.Add($"{indent}#region {targetExpression}");

            // Create the dictionary
            readLines.Add($"{indent}{targetExpression} = new Dictionary<{keyTypeName}, {valueTypeName}>();");

            // Keep track of the dictionary count (assume int is preferred)
            readLines.Add($"{indent}int {countVar} = reader.ReadInt();");
            readLines.Add("");

            // Loop
            readLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
            readLines.Add($"{indent}{{");
            readLines.Add($"{indent}    {keyTypeName} {keyVar};"); // e.g. string key0;
            readLines.Add($"{indent}    {valueTypeName} {valueVar};"); // e.g. int value0;
            readLines.Add("");

            // Read key0
            GenerateRead(
                context,
                keyType,
                keyVar,
                readLines,
                namespaces,
                indent + "    ",
                depth + 1,
                rootName);

            readLines.Add("");

            // Read value0
            GenerateRead(
                context,
                valueType,
                valueVar,
                readLines,
                namespaces,
                indent + "    ",
                depth + 1,
                rootName);

            readLines.Add("");
            // Add the key0 and value0 to the dictionary
            readLines.Add($"{indent}    {targetExpression}.Add({keyVar}, {valueVar});");
            readLines.Add($"{indent}}}");

            if (depth == 0)
                readLines.Add($"{indent}#endregion");

            return;
        }

        context.Info($"Type {type.ToDisplayString()} is not supported. Implement Read manually.");
    }
}
