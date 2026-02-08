using Microsoft.CodeAnalysis;
using PacketGen.Generators.PacketGeneration;

namespace PacketGen.Generators.TypeHandlers;

internal sealed class DictionaryTypeHandler(TypeHandlerRegistry registry) : ITypeHandler
{
    public bool CanHandle(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named && named.IsGenericType && named.Name == "Dictionary";
    }

    public void EmitWrite(WriteContext ctx, string valueExpression, string indent, int depth)
    {
        string kvVar = $"kv{depth}";

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#region {valueExpression}");

        ctx.Shared.OutputLines.Add($"{indent}// {valueExpression}");
        ctx.Shared.OutputLines.Add($"{indent}writer.Write({valueExpression}.Count);");
        ctx.Shared.OutputLines.Add("");

        ctx.Shared.OutputLines.Add($"{indent}foreach (var {kvVar} in {valueExpression})");
        ctx.Shared.OutputLines.Add($"{indent}{{");

        INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Shared.Type;
        ITypeSymbol keyType = namedType.TypeArguments[0];
        ITypeSymbol valueType = namedType.TypeArguments[1];

        GenerationContext keyCtx = new(ctx.Shared.Compilation, ctx.Shared.Property, keyType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
        registry.TryEmitWrite(new WriteContext(keyCtx), $"{kvVar}.Key", indent + "    ", depth + 1);

        ctx.Shared.OutputLines.Add("");

        GenerationContext valueCtx = new(ctx.Shared.Compilation, ctx.Shared.Property, valueType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
        registry.TryEmitWrite(new WriteContext(valueCtx), $"{kvVar}.Value", indent + "    ", depth + 1);

        ctx.Shared.OutputLines.Add($"{indent}}}");

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#endregion");
    }

    public void EmitRead(ReadContext ctx, string indent, int depth, string? rootName)
    {
        rootName ??= ctx.TargetExpression;

        ctx.Shared.Namespaces.Add("System.Collections.Generic");

        INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Shared.Type;
        ITypeSymbol keyType = namedType.TypeArguments[0];
        ITypeSymbol valueType = namedType.TypeArguments[1];

        string keyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        string valueTypeName = valueType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        string countVar = depth == 0
            ? $"{char.ToLowerInvariant(rootName[0])}{rootName.Substring(1)}Count"
            : $"count{depth}";

        string loopIndex = $"i{depth}";
        string keyVar = $"key{depth}";
        string valueVar = $"value{depth}";

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#region {ctx.TargetExpression}");

        ctx.Shared.OutputLines.Add($"{indent}{ctx.TargetExpression} = new Dictionary<{keyTypeName}, {valueTypeName}>();");
        ctx.Shared.OutputLines.Add($"{indent}int {countVar} = reader.ReadInt();");
        ctx.Shared.OutputLines.Add("");

        ctx.Shared.OutputLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
        ctx.Shared.OutputLines.Add($"{indent}{{");
        ctx.Shared.OutputLines.Add($"{indent}    {keyTypeName} {keyVar};");
        ctx.Shared.OutputLines.Add($"{indent}    {valueTypeName} {valueVar};");
        ctx.Shared.OutputLines.Add("");

        GenerationContext keyCtx = new(ctx.Shared.Compilation, ctx.Shared.Property, keyType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
        registry.TryEmitRead(new ReadContext(keyCtx, keyVar), indent + "    ", depth + 1, rootName);

        ctx.Shared.OutputLines.Add("");

        GenerationContext valueCtx = new(ctx.Shared.Compilation, ctx.Shared.Property, valueType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
        registry.TryEmitRead(new ReadContext(valueCtx, valueVar), indent + "    ", depth + 1, rootName);

        ctx.Shared.OutputLines.Add("");
        ctx.Shared.OutputLines.Add($"{indent}    {ctx.TargetExpression}.Add({keyVar}, {valueVar});");
        ctx.Shared.OutputLines.Add($"{indent}}}");

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#endregion");
    }
}
