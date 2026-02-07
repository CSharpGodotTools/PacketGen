using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace PacketGen.Tests;

internal class PacketTests
{
    [Test]
    public void NetExclude_Attribute()
    {
        string testCode = $$"""
        using Godot;

        namespace TestPackets;

        {{MainProjectSource.NetExcludeAttribute}}

        public partial class CPacketPlayerPosition : ClientPacket
        {
            public uint Id { get; set; }
            public Vector2 Position { get; set; }

            [NetExclude]
            public Vector2 PrevPosition { get; set; }
        }
        """;

        GeneratorTest<PacketGenerator> test = new(testCode, "CPacketPlayerPosition.g.cs");

        GeneratorTestResult? testBuilder = test.Start();

        Assert.That(testBuilder, Is.Not.Null, "CPacketPlayerPosition.g.cs failed to generate");
            
        testBuilder.GetGeneratedSource(out string source);

        // Check if write read methods exist
        using (Assert.EnterMultipleScope())
        {
            Assert.That(source, Does.Contain("public override void Write(PacketWriter writer)"), "The Write method is missing");
            Assert.That(source, Does.Contain("public override void Read(PacketReader reader)"), "The Read method is missing");
        }

        int idWriteIndex = source.IndexOf("writer.Write(Id);");
        int positionWriteIndex = source.IndexOf("writer.Write(Position);");
        int idReadIndex = source.IndexOf("Id = reader.ReadUInt();");
        int positionReadIndex = source.IndexOf("Position = reader.ReadVector2();");

        // Check if specific write read methods exist
        using (Assert.EnterMultipleScope())
        {
            Assert.That(idWriteIndex,       Is.Not.EqualTo(-1), "Id write method does not exist");
            Assert.That(positionWriteIndex, Is.Not.EqualTo(-1), "Position write method does not exist");
            Assert.That(idReadIndex,        Is.Not.EqualTo(-1), "Id read method does not exist");
            Assert.That(positionReadIndex,  Is.Not.EqualTo(-1), "Position read method does not exist");
        }

        // Check if write read methods are in correct order
        using (Assert.EnterMultipleScope())
        {
            Assert.That(positionWriteIndex, Is.GreaterThan(idWriteIndex), "Position write method came before Id write method");
            Assert.That(idReadIndex,        Is.GreaterThan(positionWriteIndex), "Id read method came before Position write method");
            Assert.That(positionReadIndex,  Is.GreaterThan(idReadIndex), "Position read method came before Id read method");
        }

        // Check that [NetExclude] property is actually excluded
        using (Assert.EnterMultipleScope())
        {
            Assert.That(source, Does.Not.Contain("writer.Write(PrevPosition);"), "[NetExclude] PrevPosition write method exists");
            Assert.That(source, Does.Not.Contain("PrevPosition = reader.ReadVector2();"), "[NetExclude] PrevPosition read method exists");
        }
    }

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
    public void Emit_Assembly()
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
