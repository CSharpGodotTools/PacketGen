using Microsoft.CodeAnalysis;
using PacketGen.Generators.PacketGeneration;
using System.Collections.Generic;

namespace PacketGen.Generators.Emitters;

internal sealed class EqualityGenerator : IEqualityGenerator
{
    public void Generate(List<string> equalsLines, IPropertySymbol property)
    {
        equalsLines.Add($"{property.Name}.Equals(other.{property.Name})");
    }
}
