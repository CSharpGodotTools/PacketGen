using System.Collections;
using System.Reflection;

namespace PacketGen.Tests;

internal class PacketRegistryTests
{
    [Test]
    public void PacketRegistry_Exists_And_Contains_Correct_Opcode()
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

        GeneratorTest<PacketGenerator> test = new(testCode, "PacketRegistry.g.cs");

        GeneratorTestResult? testBuilder = test.Start();

        Assert.That(testBuilder, Is.Not.Null, "PacketRegistry.g.cs failed to generate");

        testBuilder
            .GetGeneratedSource(out string source);

        Assert.That(source, Does.Contain("ushort"));
    }
}
