using Framework.Netcode;

namespace Framework.Netcode.Tests;

partial class TestPacket : ServerPacket
{
    public List<int> MyList { get; set; }
}
