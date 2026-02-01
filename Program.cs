using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PacketGen;

[Generator]
internal class Program : IIncrementalGenerator
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

        var clientSymbols = new List<INamedTypeSymbol>();
        var serverSymbols = new List<INamedTypeSymbol>();

        foreach (var syntax in list)
        {
            if (compilation
                .GetSemanticModel(syntax.SyntaxTree)
                .GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol)
                continue;

            if (symbol.BaseType == null)
                continue;

            if (symbol.BaseType.Name == "ClientPacket")
            {
                clientSymbols.Add(symbol);

                string? sourceCode = PacketReadWriteMethodsGenerator.GetSource(context, symbol);

                if (sourceCode != null)
                    // For example: CPacketPlayerInfo.g.cs
                    context.AddSource($"{symbol.Name}.g.cs", sourceCode);
            }

            if (symbol.BaseType.Name == "ServerPacket")
            {
                serverSymbols.Add(symbol);

                string? sourceCode = PacketReadWriteMethodsGenerator.GetSource(context, symbol);

                if (sourceCode != null)
                    // For example: SPacketPlayerPositions.g.cs
                    context.AddSource($"{symbol.Name}.g.cs", sourceCode);
            }
        }

        var source = PacketRegistryGenerator.GetSource(clientSymbols, serverSymbols);
        context.AddSource("PacketRegistry.g.cs", source);
    }
}
