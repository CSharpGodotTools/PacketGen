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

        foreach (IPropertySymbol property in properties)
        {
            writeLines.Add($"writer.Write({property.Name});");
        }

        foreach (IPropertySymbol property in properties)
        {
            string suffix = Utils.GetReadMethodSuffix(property);

            readLines.Add($"{property.Name} = reader.Read{suffix}();");
        }

        var indent8 = "        ";
        var sourceCode = $$"""
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
