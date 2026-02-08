using Microsoft.CodeAnalysis;
using System;
using PacketGen.Generators.PacketGeneration;
using PacketGen.Utilities;

namespace PacketGen.Generators.TypeHandlers;

internal sealed class ArrayTypeHandler(TypeHandlerRegistry registry) : ITypeHandler
{
    public bool CanHandle(ITypeSymbol type) => type is IArrayTypeSymbol;

    public void EmitWrite(WriteContext ctx, string valueExpression, string indent, int depth)
    {
        IArrayTypeSymbol arrayType = (IArrayTypeSymbol)ctx.Shared.Type;
        ITypeSymbol elementType = arrayType.ElementType;

        string countVar = $"{valueExpression}.Length";
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

        IArrayTypeSymbol arrayType = (IArrayTypeSymbol)ctx.Shared.Type;
        ITypeSymbol elementType = arrayType.ElementType;

        string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        TypeNamespaceHelper.AddNamespaceIfNeeded(elementType, ctx.Shared.Namespaces);

        string countVar = depth == 0
            ? $"{char.ToLowerInvariant(rootName[0])}{rootName.Substring(1)}Count"
            : $"count{depth}";

        string loopIndex = $"i{depth}";
        string elementVar = $"element{depth}";

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#region {ctx.TargetExpression}");

        string allocation = BuildArrayAllocation(arrayType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), countVar);

        ctx.Shared.OutputLines.Add($"{indent}int {countVar} = reader.ReadInt();");
        ctx.Shared.OutputLines.Add($"{indent}{ctx.TargetExpression} = new {allocation};");
        ctx.Shared.OutputLines.Add("");

        ctx.Shared.OutputLines.Add($"{indent}for (int {loopIndex} = 0; {loopIndex} < {countVar}; {loopIndex}++)");
        ctx.Shared.OutputLines.Add($"{indent}{{");

        string? elementSuffix = ReadMethodSuffix.Get(elementType);

        if (elementSuffix != null)
        {
            ctx.Shared.OutputLines.Add($"{indent}    {ctx.TargetExpression}[{loopIndex}] = reader.Read{elementSuffix}();");
        }
        else
        {
            ctx.Shared.OutputLines.Add($"{indent}    {elementTypeName} {elementVar};");

            GenerationContext nested = new(ctx.Shared.Compilation, ctx.Shared.Property, elementType, ctx.Shared.OutputLines, ctx.Shared.Namespaces);
            registry.TryEmitRead(new ReadContext(nested, elementVar), indent + "    ", depth + 1, rootName);

            ctx.Shared.OutputLines.Add("");
            ctx.Shared.OutputLines.Add($"{indent}    {ctx.TargetExpression}[{loopIndex}] = {elementVar};");
        }

        ctx.Shared.OutputLines.Add($"{indent}}}");

        if (depth == 0)
            ctx.Shared.OutputLines.Add($"{indent}#endregion");
    }

    private static string BuildArrayAllocation(string arrayTypeName, string countVar)
    {
        int index = arrayTypeName.IndexOf("[]", StringComparison.Ordinal);

        if (index < 0)
            return $"{arrayTypeName}[{countVar}]";

        string prefix = arrayTypeName.Substring(0, index);
        string suffix = arrayTypeName.Substring(index + 2);
        return prefix + $"[{countVar}]" + suffix;
    }
}

