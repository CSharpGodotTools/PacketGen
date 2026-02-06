<div align="center">
    <h1>PacketGen</h1>
    <a href="https://github.com/CSharpGodotTools/PacketGen/actions/workflows/build-and-test.yml"><img src="https://img.shields.io/github/actions/workflow/status/CSharpGodotTools/PacketGen/build-and-test.yml?label=.NET&style=flat&color=000000&labelColor=1a1a1a" alt=".NET Build & Test" /></a>
    <a href="https://github.com/CSharpGodotTools/PacketGen/stargazers"><img src="https://img.shields.io/github/stars/CSharpGodotTools/PacketGen?style=flat&labelColor=1a1a1a&color=000000" alt="GitHub stars" /></a>
    <a href="https://github.com/CSharpGodotTools/PacketGen/network"><img src="https://img.shields.io/github/forks/CSharpGodotTools/PacketGen?style=flat&labelColor=1a1a1a&color=000000" alt="GitHub forks" /></a>
    <a href="https://github.com/CSharpGodotTools/PacketGen/blob/main/LICENSE"><img src="https://img.shields.io/github/license/CSharpGodotTools/PacketGen?style=flat&labelColor=1a1a1a&color=000000" alt="License" /></a>
    <a href="https://github.com/CSharpGodotTools/PacketGen/commits/main"><img src="https://img.shields.io/github/last-commit/CSharpGodotTools/PacketGen?style=flat&labelColor=1a1a1a&color=000000" alt="Last commit" /></a>
    <a href="https://github.com/CSharpGodotTools/PacketGen/graphs/contributors"><img src="https://img.shields.io/github/contributors/CSharpGodotTools/PacketGen?style=flat&labelColor=1a1a1a&color=000000" alt="Contributors" /></a>
    <a href="https://github.com/CSharpGodotTools/PacketGen/watchers"><img src="https://img.shields.io/github/watchers/CSharpGodotTools/PacketGen?style=flat&labelColor=1a1a1a&color=000000" alt="Watchers" /></a>
    <a href="https://discord.gg/j8HQZZ76r8"><img src="https://img.shields.io/discord/955956101554266132?label=discord&style=flat&color=000000&labelColor=1a1a1a" alt="Discord" /></a>
    <br><br>
    <p>Source generator for the netcode packet scripts in https://github.com/CSharpGodotTools/Template</p>
</div>
<br>

Input:
```cs
public partial class CPacketPlayerInfo : ClientPacket
{
    public string Username { get; set; }
    public Vector2 Position { get; set; }
}
```

Output:
```cs
public partial class CPacketPlayerInfo
{
    public override void Write(PacketWriter writer)
    {
        writer.Write(Username);
        writer.Write(Position);
    }

    public override void Read(PacketReader reader)
    {
        Username = reader.ReadString();
        Position = reader.ReadVector2();
    }
}
```

Input:
```cs
public class TestPacket : ClientPacket {}

[PacketRegistry(typeof(ushort))]
public partial class PacketRegistry
{
}
```

Output:
```cs
public partial class PacketRegistry
{
    public static readonly Dictionary<Type, PacketInfo<ClientPacket>> ClientPacketInfo;
    public static readonly Dictionary<ushort, Type> ClientPacketTypes;
    public static readonly Dictionary<Type, PacketInfo<ServerPacket>> ServerPacketInfo;
    public static readonly Dictionary<ushort, Type> ServerPacketTypes;

    static PacketRegistry()
    {
        ClientPacketInfo = new Dictionary<Type, PacketInfo<ClientPacket>>()
        {
            
            {
                typeof(TestPacket),
                new PacketInfo<ClientPacket>
                {
                    Opcode = 0,
                    Instance = new TestPacket()
                }
            }
        };

        ClientPacketTypes = ClientPacketInfo.ToDictionary(kvp => kvp.Value.Opcode, kvp => kvp.Key);

        ServerPacketInfo = new Dictionary<Type, PacketInfo<ServerPacket>>()
        {
            
        };

        ServerPacketTypes = ServerPacketInfo.ToDictionary(kvp => kvp.Value.Opcode, kvp => kvp.Key);
    }
}

public sealed class PacketInfo<T>
{
    public ushort Opcode;
    public T Instance;
}
```

## Installing as Local NuGet Package
Copy the `.nupkg` from `bin\Debug` to main project.

Add a file named `NuGet.config` to the main projects root folder with the following contents:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalNugets" value="Framework/Libraries" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

The name for the key `LocalNugets` can be changed to anything you'd like. Change the `Framework/Libraries` path to point to the `.nupkg` package file.

Add the following to the main projects `.csproj` file:

```xml
<PackageReference Include="PacketGen" Version="*" />
```

Replace `*` with the explicit latest version or keep it as is.

## Contributing
All files are generated to `PacketGen.Tests\bin\Debug\net10.0\_Generated`.

All tests are in `PacketGen.Tests\Tests`.

All generators are in `PacketGen\Generators`.
