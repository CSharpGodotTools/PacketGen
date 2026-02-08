using Microsoft.CodeAnalysis;
using PacketGen.Generators.PacketGeneration;
using System.Collections.Generic;

namespace PacketGen.Generators.Emitters;

internal sealed class HashGenerator : IHashGenerator
{
    public void Generate(List<string> hashLines, IPropertySymbol property, HashSet<string> namespaces)
    {
        namespaces.Add("System");
        hashLines.Add($"hashCode.Add({property.Name});");
    }
}
