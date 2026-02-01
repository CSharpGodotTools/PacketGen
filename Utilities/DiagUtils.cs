using Microsoft.CodeAnalysis;

namespace PacketGen.Utilities;

internal static class DiagUtils
{
    public static void LogInfo(this SourceProductionContext context, Location? location, string message)
    {
        var descriptor = new DiagnosticDescriptor(
            "MY0001",
            "Info",
            message,
            "Info",
            DiagnosticSeverity.Info,
            true
        );

        if (location == null)
            return;

        var diagnostic = Diagnostic.Create(descriptor, location);
        context.ReportDiagnostic(diagnostic);
    }
}
