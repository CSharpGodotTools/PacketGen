using Microsoft.CodeAnalysis;
using PacketGen.Generators.PacketGeneration;
using System.Collections.Generic;

namespace PacketGen.Generators.Emitters;

internal interface IWriteGenerator
{
    void Generate(GenerationContext ctx, string valueExpression, string indent);
}

internal interface IReadGenerator
{
    void Generate(GenerationContext ctx, string targetExpression, string indent);
}

internal interface IEqualityGenerator
{
    void Generate(List<string> equalsLines, IPropertySymbol property);
}

internal interface IHashGenerator
{
    void Generate(List<string> hashLines, IPropertySymbol property, HashSet<string> namespaces);
}
