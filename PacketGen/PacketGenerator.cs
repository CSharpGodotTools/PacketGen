using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace PacketGen;

[Generator(LanguageNames.CSharp)]
public sealed class PacketGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> packetSymbols = GetPacketSymbols(context);

        var clientPackets = packetSymbols.Where(static s => s.BaseType!.Name == "ClientPacket");
        var serverPackets = packetSymbols.Where(static s => s.BaseType!.Name == "ServerPacket");

        var compilation = context.CompilationProvider;

        // Per-packet script source generation
        GenerateSourceForPacketScripts(context, clientPackets, compilation);
        GenerateSourceForPacketScripts(context, serverPackets, compilation);

        // Discover [PacketRegistry] attributes
        IncrementalValuesProvider<INamedTypeSymbol> registryClass = FindPacketRegistryAttributes(context);

        // Registry generation (gated by attribute)
        var registryInput = registryClass
            .Combine(clientPackets.Collect())
            .Combine(serverPackets.Collect());

        GeneratePacketRegistryClass(context, registryInput);
    }

    private static IncrementalValuesProvider<INamedTypeSymbol> GetPacketSymbols(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.BaseList is not null,
                transform: static (ctx, _) =>
                {
                    var syntax = (ClassDeclarationSyntax)ctx.Node;
                    return ctx.SemanticModel.GetDeclaredSymbol(syntax) as INamedTypeSymbol;
                })
            .Where(static symbol =>
                symbol is not null &&
                symbol.BaseType is not null &&
                (symbol.BaseType.Name == "ClientPacket" || symbol.BaseType.Name == "ServerPacket"))
            .Select(static (symbol, _) => symbol!);
    }

    private static void GenerateSourceForPacketScripts(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<INamedTypeSymbol> clientPackets, IncrementalValueProvider<Compilation> compilation)
    {
        context.RegisterSourceOutput(
            clientPackets.Combine(compilation),
            static (spc, pair) =>
            {
                INamedTypeSymbol symbol = pair.Left;
                Compilation compilationValue = pair.Right;

                string? source =
                    PacketGenerators.GetSource(compilationValue, symbol);

                if (source is not null)
                    spc.AddSource($"{symbol.Name}.g.cs", source);
            });
    }

    /// <summary>
    /// Generates the PacketRegistry.g.cs script.
    /// </summary>
    private static void GeneratePacketRegistryClass(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<((INamedTypeSymbol Left, ImmutableArray<INamedTypeSymbol> Right) Left, ImmutableArray<INamedTypeSymbol> Right)> registryInput)
    {
        context.RegisterSourceOutput(registryInput,
            static (spc, triple) =>
            {
                INamedTypeSymbol registrySymbol = triple.Left.Left;
                ImmutableArray<INamedTypeSymbol> clients = triple.Left.Right;
                ImmutableArray<INamedTypeSymbol> servers = triple.Right;

                string opcodePacketTypeName = GetPacketSizeTypeName(registrySymbol);

                string source = PacketRegistryGenerator.GetSource(registrySymbol, opcodePacketTypeName,
                    [.. clients],
                    [.. servers]);

                spc.AddSource($"{registrySymbol.Name}.g.cs", source);
            });
    }

    /// <summary>
    /// Finds all [PacketRegistry] attributes in the assembly.
    /// </summary>
    private static IncrementalValuesProvider<INamedTypeSymbol> FindPacketRegistryAttributes(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) =>
                node is ClassDeclarationSyntax,
            transform: static (ctx, _) =>
            {
                ClassDeclarationSyntax syntax = (ClassDeclarationSyntax)ctx.Node;

                if (ctx.SemanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol)
                    return null;

                foreach (AttributeData attribute in symbol.GetAttributes())
                {
                    if (attribute.AttributeClass?.Name == "PacketRegistryAttribute")
                        return symbol;
                }

                return null;
            })
            .Where(static s => s is not null)
            .Select(static (s, _) => s!);
    }

    /// <summary>
    /// Gets the type that defines the packet size from the [PacketRegistry] attribute.
    /// For example if it's [PacketRegistry(typeof(ushort))] then "ushort" would be returned.
    /// </summary>
    private static string GetPacketSizeTypeName(INamedTypeSymbol registrySymbol)
    {
        foreach (AttributeData attribute in registrySymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != "PacketRegistryAttribute")
                continue;

            if (attribute.ConstructorArguments.Length == 1)
            {
                TypedConstant arg = attribute.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol typeSymbol)
                    return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }

        return "byte";
    }
}
