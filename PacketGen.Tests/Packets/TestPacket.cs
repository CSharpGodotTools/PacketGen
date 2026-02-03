using Framework.Netcode;

namespace Framework.Netcode.Tests;

partial class TestPacket : ServerPacket
{
    public uint Id { get; set; }
}
