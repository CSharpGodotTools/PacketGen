using Microsoft.CodeAnalysis;
using PacketGen.Generators.PacketGeneration;
using PacketGen.Utilities;

namespace PacketGen.Generators.TypeHandlers;

internal sealed class ListTypeHandler(TypeHandlerRegistry registry) : ITypeHandler
{
    public bool CanHandle(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named && named.IsGenericType && named.Name == "List";
    }

    public void EmitWrite(WriteContext ctx, string valueExpression, string indent, int depth)
    {
        INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Shared.Type;
        ITypeSymbol elementType = namedType.TypeArguments[0];

        string countVar = $"{valueExpression}.Count";
        string loopIndex = $"i{depth}";
        string elementAccess = $"{valueExpression}[{loopIndex}]";

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#region {valueExpression}");

        ctx.Shared.OutputLines.Add($"{indent}writer.Write({countVar});");
        ctx.Shared.OutputLines.Add("");

        ctx.Shared.OutputLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
        ctx.Shared.OutputLines.Add($"{indent}{{");

        string? elementSuffix = ReadMethodSuffix.Get(elementType);

        if (elementSuffix != null)
        {
            ctx.Shared.OutputLines.Add($"{indent}    writer.Write({elementAccess});");
        }
        else
        {
            GenerationContext nested = new(ctx.Shared.Compilation, ctx.Shared.Property, elementType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
            registry.TryEmitWrite(new WriteContext(nested), elementAccess, indent + "    ", depth + 1);
        }

        ctx.Shared.OutputLines.Add($"{indent}}}");

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#endregion");
    }

    public void EmitRead(ReadContext ctx, string indent, int depth, string? rootName)
    {
        rootName ??= ctx.TargetExpression;

        ctx.Shared.Namespaces.Add("System.Collections.Generic");

        INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Shared.Type;
        ITypeSymbol elementType = namedType.TypeArguments[0];

        string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        string countVar = depth == 0
            ? $"{char.ToLowerInvariant(rootName[0])}{rootName.Substring(1)}Count"
            : $"count{depth}";

        string loopIndex = $"i{depth}";
        string elementVar = $"element{depth}";

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#region {ctx.TargetExpression}");

        ctx.Shared.OutputLines.Add($"{indent}{ctx.TargetExpression} = new List<{elementTypeName}>();");
        ctx.Shared.OutputLines.Add($"{indent}int {countVar} = reader.ReadInt();");
        ctx.Shared.OutputLines.Add("");

        ctx.Shared.OutputLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
        ctx.Shared.OutputLines.Add($"{indent}{{");

        string? elementSuffix = ReadMethodSuffix.Get(elementType);

        if (elementSuffix != null)
        {
            ctx.Shared.OutputLines.Add($"{indent}    {ctx.TargetExpression}.Add(reader.Read{elementSuffix}());");
        }
        else
        {
            ctx.Shared.OutputLines.Add($"{indent}    {elementTypeName} {elementVar} = new {elementTypeName}();");

            GenerationContext nested = new(ctx.Shared.Compilation, ctx.Shared.Property, elementType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
            registry.TryEmitRead(new ReadContext(nested, elementVar), indent + "    ", depth + 1, rootName);

            ctx.Shared.OutputLines.Add("");
            ctx.Shared.OutputLines.Add($"{indent}    {ctx.TargetExpression}.Add({elementVar});");
        }

        ctx.Shared.OutputLines.Add($"{indent}}}");

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#endregion");
    }
}
