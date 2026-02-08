using Microsoft.CodeAnalysis;
using PacketGen.Generators.PacketGeneration;
using System.Collections.Generic;

namespace PacketGen.Generators.Emitters;

internal sealed class HashGenerator : IHashGenerator
{
    public void Generate(List<string> hashLines, IPropertySymbol property, HashSet<string> namespaces)
    {
        string propHash = property.Type.IsReferenceType || property.NullableAnnotation == NullableAnnotation.Annotated
            ? $"({property.Name} != null ? {property.Name}.GetHashCode() : 0)"
            : $"{property.Name}.GetHashCode()";

        hashLines.Add($"hash = (hash * 397) ^ {propHash};");
    }
}
