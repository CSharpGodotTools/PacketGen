using Microsoft.CodeAnalysis;
using System;
using System.Reflection.Metadata;

namespace PacketGen.Utilities;

// See Framework.Netcode.PacketReader for all read methods
internal class ReadMethodSuffix
{
    public static string? Get(SpecialType type)
    {
        return type switch
        {
            SpecialType.System_Byte => "Byte",
            SpecialType.System_SByte => "SByte",
            SpecialType.System_Char => "Char",
            SpecialType.System_String => "String",
            SpecialType.System_Boolean => "Bool",
            SpecialType.System_Int16 => "Short",
            SpecialType.System_UInt16 => "UShort",
            SpecialType.System_Int32 => "Int",
            SpecialType.System_UInt32 => "UInt",
            SpecialType.System_Single => "Float",
            SpecialType.System_Double => "Double",
            SpecialType.System_Int64 => "Long",
            SpecialType.System_UInt64 => "ULong",
            _ => null
        };
    }

    public static string? Get(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString() switch
        {
            "byte[]" => "Bytes",
            "Godot.Vector2" => "Vector2",
            "Godot.Vector3" => "Vector3",
            _ => null
        };
    }
}
