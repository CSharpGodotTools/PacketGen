using Microsoft.CodeAnalysis;
using System.Linq;

namespace PacketGen.Utilities;

internal static class DiagUtils
{
    public static void Err(this SourceProductionContext context, ISymbol symbol, string message)
    {
        Log(context, symbol, message, DiagnosticSeverity.Error);
    }

    public static void Warn(this SourceProductionContext context, ISymbol symbol, string message)
    {
        Log(context, symbol, message, DiagnosticSeverity.Warning);
    }

    public static void Info(this SourceProductionContext context, ISymbol symbol, string message)
    {
        Log(context, symbol, message, DiagnosticSeverity.Info);
    }

    private static void Log(this SourceProductionContext context, ISymbol symbol, string message, DiagnosticSeverity severity)
    {
        var descriptor = new DiagnosticDescriptor(
            "MY0001",
            "Info",
            message,
            "Diagnostics",
            severity,
            true
        );

        Location? location = symbol.Locations.FirstOrDefault();

        var diagnostic = Diagnostic.Create(descriptor, location);
        context.ReportDiagnostic(diagnostic);
    }
}
