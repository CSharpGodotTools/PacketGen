using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PacketGen;

[Generator]
public class PacketSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Watch the YT video to see why this code is needed
        // https://www.youtube.com/watch?v=Yf8t7GqA6zA
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
            ).Where(m => m is not null);

        var compliation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compliation, Execute);
    }

    private void Execute(SourceProductionContext context, (Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right) tuple)
    {
        var (compilation, list) = tuple;

        foreach (var syntax in list)
        {
            if (compilation
                .GetSemanticModel(syntax.SyntaxTree)
                .GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol)
                continue;

            if (symbol.BaseType == null)
                continue;

            if (symbol.BaseType.Name == "ServerPacket" || symbol.BaseType.Name == "ClientPacket")
            {
                List<IPropertySymbol> properties = [];

                foreach (var member in symbol.GetMembers())
                {
                    if (member is IPropertySymbol property)
                    {
                        properties.Add(property);
                    }
                }

                if (properties.Count > 0)
                {
                    string sourceCode = GenerateSourceStr(symbol, properties);

                    context.AddSource($"{symbol.Name}.g.cs", sourceCode);
                }
            }
        }
    }

    private string GenerateSourceStr(INamedTypeSymbol symbol, List<IPropertySymbol> properties)
    {
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
            string? readMethodSuffix = property.Type.SpecialType switch
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

            readLines.Add($"{property.Name} = reader.Read{readMethodSuffix}();");
        }

        var sourceCode = $$"""
using Godot;

namespace {{namespaceName}};

public partial class {{className}}
{
    public override void Write(PacketWriter writer)
    {
{{string.Join("\n", writeLines.Select(line => "        " + line))}}
    }

    public override void Read(PacketReader reader)
    {
{{string.Join("\n", readLines.Select(line => "        " + line))}}
    }
}

""";

        return sourceCode;
    }
}
