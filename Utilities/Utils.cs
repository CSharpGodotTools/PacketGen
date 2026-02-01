using Microsoft.CodeAnalysis;
using System;

namespace PacketGen.Utilities;

internal class Utils
{
    public static string GetReadMethodSuffix(IPropertySymbol property)
    {
        string? readMethodSuffix = property.Type.SpecialType switch
        {
            // See Framework.Netcode.PacketReader for all read methods
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

        if (readMethodSuffix == null)
        {
            var typeName = property.Type.ToDisplayString();

            readMethodSuffix = typeName switch
            {
                "byte[]" => "Bytes",
                "Godot.Vector2" => "Vector2",
                "Godot.Vector3" => "Vector3",
                _ => throw new NotSupportedException($"Type {property.Type} not supported")
            };
        }

        return readMethodSuffix;
    }
}
