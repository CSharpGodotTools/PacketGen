using Microsoft.CodeAnalysis;
using System.Linq;

namespace PacketGen.Utilities;

internal static class DiagUtils
{
    public static void Err(this SourceProductionContext context, ISymbol symbol, string? message)
    {
        Log(context, symbol, message, DiagnosticSeverity.Error);
    }

    public static void Warn(this SourceProductionContext context, ISymbol symbol, string? message)
    {
        Info(context, symbol, message);
    }

    public static void Info(this SourceProductionContext context, string? message)
    {
        Info(context, null, message);
    }

    public static void Info(this SourceProductionContext context, ISymbol? symbol, string? message)
    {
        // DiagnosticSeverity.Info can be hidden in the main project if the location is Location.None
        // So lets just use DiagnosticSeverity.Warning every time for debugging!
        Log(context, symbol, message, DiagnosticSeverity.Warning);
    }

    private static void Log(this SourceProductionContext context, ISymbol? symbol, string? message, DiagnosticSeverity severity)
    {
        var descriptor = new DiagnosticDescriptor(
            "MY0001",
            "Info",
            message ?? "null",
            "Diagnostics",
            severity,
            true
        );

        Location? location = Location.None;

        if (symbol != null)
        {
            location = symbol.Locations.FirstOrDefault() ?? Location.None;
        }

        var diagnostic = Diagnostic.Create(descriptor, location);
        context.ReportDiagnostic(diagnostic);
    }
}
