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

        const string expectedPacketGenerated = """
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

        const string expectedRegistryGenerated = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Framework.Netcode.Tests;

            namespace Framework.Netcode;

            public static partial class PacketRegistry
            {
                public static readonly Dictionary<Type, PacketInfo<ClientPacket>> ClientPacketInfo;
                public static readonly Dictionary<byte, Type> ClientPacketTypes;
                public static readonly Dictionary<Type, PacketInfo<ServerPacket>> ServerPacketInfo;
                public static readonly Dictionary<byte, Type> ServerPacketTypes;

                static PacketRegistry()
                {
                    ClientPacketInfo = new Dictionary<Type, PacketInfo<ClientPacket>>()
                    {
                        
                    };

                    ClientPacketTypes = ClientPacketInfo.ToDictionary(kvp => kvp.Value.Opcode, kvp => kvp.Key);

                    ServerPacketInfo = new Dictionary<Type, PacketInfo<ServerPacket>>()
                    {
                        
                        {
                            typeof(TestPacket),
                            new PacketInfo<ServerPacket>
                            {
                                Opcode = 0,
                                Instance = new TestPacket()
                            }
                        }
                    };

                    ServerPacketTypes = ServerPacketInfo.ToDictionary(kvp => kvp.Value.Opcode, kvp => kvp.Key);
                }
            }

            """;

        var test = new CSharpIncrementalGeneratorTest<PacketGen.Program, XUnitVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            TestState =
            {
                Sources = { netcodeStubs, packetSource },
                GeneratedSources =
                {
                    (typeof(PacketGen.Program), "TestPacket.g.cs", expectedPacketGenerated),
                    (typeof(PacketGen.Program), "PacketRegistry.g.cs", expectedRegistryGenerated),
                },
            },
        };

        await test.RunAsync();
    }
}
