# PacketGen
Source generator for the netcode packet scripts in https://github.com/CSharpGodotTools/Template

## What gets Generated
Example 1:
```cs
public partial class CPacketPlayerInfo : ClientPacket
{
    public string Username { get; set; }
    public Vector2 Position { get; set; }
}
```

Source gen outputs:
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

Example 2:
```cs
public partial class CPacketTest : ClientPacket
{
    public int Id { get; set; }

    public string Name { get; set; }

    public Dictionary<string, int> Scores { get; set; }

    public Dictionary<string, List<int>> Test { get; set; }

    public Dictionary<string, List<List<int>>> Deep { get; set; }
}
```

Source gen outputs:
```cs
public partial class CPacketTest
{
    public override void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
        #region Scores
        // Scores
        writer.Write(Scores.Count);
        
        foreach (var kv0 in Scores)
        {
            writer.Write(kv0.Key);
        
            writer.Write(kv0.Value);
        }
        #endregion
        #region Test
        // Test
        writer.Write(Test.Count);
        
        foreach (var kv0 in Test)
        {
            writer.Write(kv0.Key);
        
            writer.Write(kv0.Value.Count);
        
            for (int i1 = 0; i1 < kv0.Value.Count; i1++)
            {
                writer.Write(kv0.Value[i1]);
            }
        }
        #endregion
        #region Deep
        // Deep
        writer.Write(Deep.Count);
        
        foreach (var kv0 in Deep)
        {
            writer.Write(kv0.Key);
        
            writer.Write(kv0.Value.Count);
        
            for (int i1 = 0; i1 < kv0.Value.Count; i1++)
            {
                writer.Write(kv0.Value[i1].Count);
        
                for (int i2 = 0; i2 < kv0.Value[i1].Count; i2++)
                {
                    writer.Write(kv0.Value[i1][i2]);
                }
            }
        }
        #endregion
    }

    public override void Read(PacketReader reader)
    {
        Id = reader.ReadInt();
        Name = reader.ReadString();
        #region Scores
        Scores = new Dictionary<string, int>();
        int scoresCount = reader.ReadInt();
        
        for (int i0 = 0; i0 < scoresCount; i0++)
        {
            string key0;
            int value0;
        
            key0 = reader.ReadString();
        
            value0 = reader.ReadInt();
        
            Scores.Add(key0, value0);
        }
        #endregion
        #region Test
        Test = new Dictionary<string, List<int>>();
        int testCount = reader.ReadInt();
        
        for (int i0 = 0; i0 < testCount; i0++)
        {
            string key0;
            List<int> value0;
        
            key0 = reader.ReadString();
        
            value0 = new List<int>();
            int count1 = reader.ReadInt();
        
            for (int i1 = 0; i1 < count1; i1++)
            {
                value0.Add(reader.ReadInt());
            }
        
            Test.Add(key0, value0);
        }
        #endregion
        #region Deep
        Deep = new Dictionary<string, List<List<int>>>();
        int deepCount = reader.ReadInt();
        
        for (int i0 = 0; i0 < deepCount; i0++)
        {
            string key0;
            List<List<int>> value0;
        
            key0 = reader.ReadString();
        
            value0 = new List<List<int>>();
            int count1 = reader.ReadInt();
        
            for (int i1 = 0; i1 < count1; i1++)
            {
                List<int> element1 = new List<int>();
                element1 = new List<int>();
                int count2 = reader.ReadInt();
        
                for (int i2 = 0; i2 < count2; i2++)
                {
                    element1.Add(reader.ReadInt());
                }
        
                value0.Add(element1);
            }
        
            Deep.Add(key0, value0);
        }
        #endregion
    }
}
```

## Installing as Dll
This is the simplest way to install but requires manually copying after each build.

Build the source gen project. Dll is built to `bin\Debug\netstandard2.0`.

Copy the dll to somewhere in the main project.

Add the following to the `.csproj`. Replace the path with the correct path to the dll.

```xml
<Analyzer Include="Framework/Libraries/PacketGen.dll" />
```

## Installing as Project Reference
This is the fastest way to debug the source gen without needing to manually copy a file after builds.

1. Right click 'Template' solution, click 'Add > Existing Project', navigate to this source gen project.
2. Right click 'Template' project, click 'Add > Project Reference...' and select the source gen project.

The following should have appeared in `Template.csproj`

```xml
<ProjectReference Include="..\PacketGen\PacketGen.csproj" >
```

Change it so it looks like the following.

```xml
<ProjectReference Include="..\PacketGen\PacketGen.csproj" 
                  OutputItemType="Analyzer"/>
```

Build the source gen project then build the main project.

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

The `*` means use the latest package version if multiple are present. You can switch this to be an explicit version if needed.

## Installing as Online NuGet Package
WIP

## Troubleshooting
If the Analyzers in VS2022 are not showing anything being generated but the source gen is still generating the correct scripts then this is a VS2022 bug and restarting VS2022 should fix this.

Use `context.ReportDiagnostic(...)` or generate code comments to debug the source gen.
