using System.Diagnostics;

namespace PacketGen;

internal static class MyDebugger
{
    public static void Launch()
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
    }
}
