using System.Diagnostics;

namespace PacketGen.Tests;

internal static class GeneratedFiles
{
    public static void Preview(string fileName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = GetPath(fileName),
            UseShellExecute = true
        });
    }

    public static void Output(string fileName, string source)
    {
        string path = GetPath(fileName);

        File.WriteAllText(path, source);
    }

    public static string GetGenDir()
    {
        string baseDir = TestContext.CurrentContext.TestDirectory;

        return Path.Combine(baseDir, "_Generated");
    }

    private static string GetPath(string fileName)
    {
        string genDir = GetGenDir();
        Directory.CreateDirectory(genDir);

        return Path.Combine(genDir, fileName);
    }
}
