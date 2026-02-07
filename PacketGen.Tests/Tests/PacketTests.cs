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

        GeneratorTestResult? testResult = test.Start();

        Assert.That(testResult, Is.Null, "A packet with no properties should not trigger the source generator.");
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

        testBuilder.CompileGeneratedAssembly(source);
    }
}
