namespace PacketGen.Tests;

internal class PacketRegistryTests
{
    [Test]
    public void Correct_Type()
    {
        string testCode = $$"""
        namespace Framework.Netcode;

        public partial class CTestPacket1 : ClientPacket {}
        public partial class CTestPacket2 : ClientPacket {}
        public partial class CTestPacket3 : ClientPacket {}

        public partial class STestPacket1 : ServerPacket {}
        public partial class STestPacket2 : ServerPacket {}

        {{MainProjectSource.PacketRegistryAttribute}}

        [PacketRegistry(typeof(ushort))]
        public partial class PacketRegistry
        {
        }
        """;

        GeneratorTestOptions options = new GeneratorTestBuilder<PacketGenerator>(testCode)
            .WithGeneratedFile("PacketRegistry.g.cs")
            .Build();

        GeneratorTestRunResult? result = new GeneratorTestRunner<PacketGenerator>().Run(options);

        Assert.That(result, Is.Not.Null, "PacketRegistry.g.cs failed to generate");

        IGeneratedFileStore fileStore = new GeneratedFileStore();
        fileStore.Write(result.GeneratedFile, result.GeneratedSource);

        string source = result.GeneratedSource;

        Assert.That(source, Does.Contain("ushort"));

        new GeneratedAssemblyCompiler().Compile(result, fileStore);
    }
}
