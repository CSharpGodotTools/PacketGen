using Microsoft.CodeAnalysis;
using PacketGen.Generators.Emitters;
using PacketGen.Generators.PacketGeneration;
using PacketGen.Generators.TypeHandlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PacketGen.Generators;

internal sealed class PacketGenerationOrchestrator
{
    public string? GenerateSource(Compilation compilation, INamedTypeSymbol symbol)
    {
        PacketGenerationModel model = PacketAnalysis.Analyze(symbol);

        if (model.HasWriteReadMethods || model.Properties.Length == 0)
            return null;

        HashSet<string> namespaces = [];
        List<string> writeLines = [];
        List<string> readLines = [];
        List<string> equalsLines = [];
        List<string> hashLines = [];

        TypeHandlerRegistry registry = BuildRegistry();

        IWriteGenerator writeGenerator = new WriteGenerator(registry);
        IReadGenerator readGenerator = new ReadGenerator(registry);
        IEqualityGenerator equalityGenerator = new EqualityGenerator();
        IHashGenerator hashGenerator = new HashGenerator();

        foreach (IPropertySymbol property in model.Properties)
        {
            GenerationContext shared = new(compilation, property, property.Type, writeLines, namespaces);
            writeGenerator.Generate(shared, property.Name, "");

            GenerationContext readShared = new(compilation, property, property.Type, readLines, namespaces);
            readGenerator.Generate(readShared, property.Name, "");

            equalityGenerator.Generate(equalsLines, property);
            hashGenerator.Generate(hashLines, property, namespaces);
        }

        string usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));
        string indent8 = "        ";
        string indent12 = "            ";

        string sourceCode = $$"""
{{usings}}
namespace {{model.NamespaceName}};

public partial class {{model.ClassName}}
{
    public override void Write(PacketWriter writer)
    {
{{string.Join("\n", writeLines.Select(line => indent8 + line))}}
    }

    public override void Read(PacketReader reader)
    {
{{string.Join("\n", readLines.Select(line => indent8 + line))}}
    }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not {{model.ClassName}} other)
            return false;

        return 
{{string.Join("\n", equalsLines.Select((line, i) => indent12 + line + (i == equalsLines.Count - 1 ? ";" : " &&")))} }
    }

    public override int GetHashCode()
    {
        int hash = 17;

{{string.Join("\n", hashLines.Select(line => indent8 + line))}}

        return hash;
    }
}

""";

        return sourceCode;
    }

    private static TypeHandlerRegistry BuildRegistry()
    {
        TypeHandlerRegistry registry = new();

        PrimitiveTypeHandler primitives = new();
        ArrayTypeHandler arrays = new(registry);
        ListTypeHandler lists = new(registry);
        DictionaryTypeHandler dictionaries = new(registry);

        registry.SetHandlers(new ITypeHandler[]
        {
            primitives,
            arrays,
            lists,
            dictionaries
        });

        return registry;
    }
}
