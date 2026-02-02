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
            string typeStr = property.Type.ToDisplayString();
            string propName = property.Name;

            if (typeStr == "System.Collections.Generic.List<int>")
            {
                writeLines.Add($"writer.Write({propName}.Count);");
                writeLines.Add($"");
                writeLines.Add($"for (int i = 0; i < {propName}.Count; i++)");
                writeLines.Add($"{{");
                writeLines.Add($"    writer.Write({propName}[i]);");
                writeLines.Add($"}}");
            }
            else
            {
                writeLines.Add($"writer.Write({propName});");
            }
        }

        foreach (IPropertySymbol property in properties)
        {
            string? suffix = ReadMethodSuffix.Get(property);
            string propName = property.Name;

            if (suffix == null)
            {
                string typeStr = property.Type.ToDisplayString();

                if (typeStr == "System.Collections.Generic.List<int>")
                {
                    readLines.Add($"{propName} = new List<int>();");
                    readLines.Add($"int {propName.ToLower()}Count = reader.ReadInt();");
                    readLines.Add($"");
                    readLines.Add($"for (int i = 0; i < {propName.ToLower()}Count; i++)");
                    readLines.Add($"{{");
                    readLines.Add($"    {propName}.Add(reader.ReadInt());");
                    readLines.Add($"}}");

                    namespaces.Add("System.Collections.Generic");
                }
                else
                {
                    context.Warn(property, $"Type {property.Type} not supported. Manually implement the write and read operations in the Write and Read overrides to get rid of this warning.");
                    return null;
                }
            }
            else
            {
                readLines.Add($"{property.Name} = reader.Read{suffix}();");
            }  
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
}
