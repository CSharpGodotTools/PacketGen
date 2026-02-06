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

        TestAdapter<PacketGenerator> test = new(testCode, "CPacketEmpty.g.cs");

        test.Start();
    }

    [Test]
    public void Emit_Assembly_Test()
    {
        string className = "CSimplePacket";
        string testCode = $$"""
        namespace TestPackets;

        {{MainProjectSource.PacketStubs}}

        // Test Code
        public partial class {{className}} : ClientPacket
        {
            public int Id { get; set; }
        }
        """;

        TestAdapter<PacketGenerator> test = new(testCode, $"{className}.g.cs");

        TestAdapterBuilder? testBuilder = test.Start();

        Assert.That(testBuilder, Is.Not.Null, $"{className}.g.cs failed to generate");
    }
}
