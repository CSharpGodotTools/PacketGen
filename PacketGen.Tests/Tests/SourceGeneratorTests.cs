using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace PacketGen.Tests;

public class SourceGeneratorTests
{
    [Fact]
    public async Task GeneratesReadWriteForListProperty()
    {
        const string netcodeStubs = """
            using System.Collections.Generic;

            namespace Framework.Netcode;

            public abstract class GamePacket
            {
                public virtual void Write(PacketWriter writer) { }
                public virtual void Read(PacketReader reader) { }
            }

            public class ClientPacket : GamePacket { }
            public class ServerPacket : GamePacket { }

            public class PacketWriter
            {
                public void Write(int value) { }
            }

            public class PacketReader
            {
                public int ReadInt() => 0;
            }

            public class PacketInfo<T> where T : GamePacket
            {
                public byte Opcode { get; set; }
                public T Instance { get; set; }
            }

            public static partial class PacketRegistry { }
            """;

        const string packetSource = """
            using System.Collections.Generic;
            using Framework.Netcode;

            namespace Framework.Netcode.Tests;

            public partial class TestPacket : ServerPacket
            {
                public List<int> MyList { get; set; }
            }
            """;

        const string expectedGenerated = """
            using System.Collections.Generic;

            namespace Framework.Netcode.Tests;

            public partial class TestPacket
            {
                public override void Write(PacketWriter writer)
                {
                    #region MyList
                    writer.Write(MyList.Count);

                    for (int i0 = 0; i0 < MyList.Count; i0++)
                    {
                        writer.Write(MyList[i0]);
                    }
                    #endregion
                }

                public override void Read(PacketReader reader)
                {
                    #region MyList
                    MyList = new List<int>();
                    int myListCount = reader.ReadInt();

                    for (int i0 = 0; i0 < myListCount; i0++)
                    {
                        MyList.Add(reader.ReadInt());
                    }
                    #endregion
                }
            }

            """;

        var test = new CSharpSourceGeneratorTest<PacketGen.Program, XUnitVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
            {
                Sources = { netcodeStubs, packetSource },
                GeneratedSources =
                {
                    (typeof(PacketGen.Program), "TestPacket.g.cs", expectedGenerated),
                },
            },
        };

        await test.RunAsync();
    }
}
