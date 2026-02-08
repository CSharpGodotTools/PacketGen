namespace PacketGen.Tests;

[TestFixture]
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

        GeneratorTestOptions options = new GeneratorTestBuilder<PacketGenerator>(testCode)
            .WithGeneratedFile("CPacketPlayerPosition.g.cs")
            .Build();

        GeneratorTestRunResult? result = GeneratorTestRunner<PacketGenerator>.Run(options);

        Assert.That(result, Is.Not.Null, "CPacketPlayerPosition.g.cs failed to generate");

        GeneratedFileStore fileStore = new();
        fileStore.Write(result.GeneratedFile, result.GeneratedSource);

        string source = result.GeneratedSource;

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

        GeneratorTestOptions options = new GeneratorTestBuilder<PacketGenerator>(testCode)
            .WithGeneratedFile("CPacketEmpty.g.cs")
            .Build();

        GeneratorTestRunResult? result = GeneratorTestRunner<PacketGenerator>.Run(options);

        Assert.That(result, Is.Null, "A packet with no properties should not trigger the source generator.");
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

        GeneratorTestOptions options = new GeneratorTestBuilder<PacketGenerator>(testCode)
            .WithGeneratedFile($"{className}.g.cs")
            .Build();

        GeneratorTestRunResult? result = GeneratorTestRunner<PacketGenerator>.Run(options);

        Assert.That(result, Is.Not.Null, $"{className}.g.cs failed to generate");

        GeneratedFileStore fileStore = new();
        fileStore.Write(result.GeneratedFile, result.GeneratedSource);

        GeneratedAssemblyCompiler.Compile(result, fileStore);
    }
}
