using Microsoft.CodeAnalysis;
using System.ComponentModel;
using System.Linq;

namespace PacketGen;

internal static class Logger
{
    private static SourceProductionContext _context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Init(SourceProductionContext context) => _context = context;

    public static void Err(ISymbol symbol, string? message)
    {
        Log(symbol, message, DiagnosticSeverity.Error);
    }

    public static void Warn(ISymbol symbol, string? message)
    {
        Info(symbol, message);
    }

    public static void Info(string? message)
    {
        Info(null, message);
    }

    public static void Info(ISymbol? symbol, string? message)
    {
        // DiagnosticSeverity.Info can be hidden in the main project if the location is Location.None
        // So lets just use DiagnosticSeverity.Warning every time for debugging!
        Log(symbol, message, DiagnosticSeverity.Warning);
    }

    private static void Log(ISymbol? symbol, string? message, DiagnosticSeverity severity)
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
        _context.ReportDiagnostic(diagnostic);
    }
}
