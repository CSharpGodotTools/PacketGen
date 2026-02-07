using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PacketGen.Tests.Tests;

internal class SimpleTests
{
    [Test]
    public void Empty_Packet()
    {
        string testCode = $$"""
        namespace TestPackets;

        public partial class CPacketEmpty : ClientPacket
        {
        }
        """;

        GeneratorTest<PacketGenerator> test = new(testCode, "CPacketEmpty.g.cs");

        test.Start();
    }

    [Test]
    public void Emit_Assembly_Test()
    {
        string className = "CSimplePacket";
        string testCode = $$"""
        namespace TestPackets;

        // Test Code
        public partial class {{className}} : ClientPacket
        {
            public int Id { get; set; }
        }
        """;

        GeneratorTest<PacketGenerator> test = new(testCode, $"{className}.g.cs");

        GeneratorTestResult? testBuilder = test.Start();

        Assert.That(testBuilder, Is.Not.Null, $"{className}.g.cs failed to generate");

        testBuilder.GetGeneratedSource(out string source);

        var result = testBuilder.CompileGeneratedAssembly(source);

        if (!result.Success)
        {
            var sb = new StringBuilder();

            sb.AppendLine("========= Errors =========\n");

            int sevWidth = 8;    // e.g. "Warning"
            int idWidth = 7;     // e.g. "CS0123"
            int locWidth = 18;   // adjust for typical file/line span

            static string Pad(string s, int w) => s.Length >= w ? s : s + new string(' ', w - s.Length);

            foreach (var d in result.Diagnostics)
            {
                string sev = Pad(d.Severity.ToString(), sevWidth);
                string id = Pad(d.Id, idWidth);

                string loc = d.Location == Location.None
                    ? Pad("NoLocation", locWidth)
                    : Pad(d.Location.GetLineSpan().ToString(), locWidth);

                string msg = d.GetMessage().Replace("\r\n", " ").Replace("\n", " ");

                sb.AppendLine($"{sev} {id} {loc} : {msg}");
            }

            sb.AppendLine();
            sb.AppendLine("========= References =========\n");

            foreach (string @ref in result.ReferencePaths)
            {
                sb.AppendLine(@ref);
            }

            sb.AppendLine();
            sb.AppendLine("========= Generated source =========\n");
            sb.AppendLine(source);

            if (result.AssemblyException is not null)
            {
                sb.AppendLine("Assembly load exception: " + result.AssemblyException);
            }

            GeneratedFiles.OutputErrors($"{className}_Errors.txt", sb.ToString());

            Assert.That(false, $"Test assembly failed with errors, see {className}_Errors.txt");
        }
    }
}
