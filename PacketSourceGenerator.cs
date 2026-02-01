using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        var nameList = new List<string>();

        foreach (var syntax in list)
        {
            var symbol = compilation
                .GetSemanticModel(syntax.SyntaxTree)
                .GetDeclaredSymbol(syntax) as INamedTypeSymbol;

            if (symbol == null)
                continue;

            if (symbol.BaseType == null)
                continue;

            if (symbol.BaseType.Name == "ServerPacket" || symbol.BaseType.Name == "ClientPacket")
            {
                nameList.Add($"\"{symbol.ToDisplayString()}\"");
            }
        }

        var sourceCode = $$"""
using Godot;

namespace GeneratedCode;

public class HelloWorld
{
    /*
    {{string.Join("\n    ", nameList)}}
    */
}
""";

        context.AddSource("HelloWorld.g.cs", sourceCode);
    }
}
