# PacketGen
Source generator for ENet multiplayer framework in Template.

## Install
Build the source gen project. Dll is built to `bin\Debug\netstandard2.0\PacketGen.dll`.

Copy this dll to be somewhere in the main project.

Add the following to the `.csproj`. Replace the path with the correct path to the dll.

```xml
<Analyzer Include="Framework/Libraries/PacketGen.dll" />
```

## Debugging
Copying dll's is time consuming so this guide will remove that step. Assuming VS2022 is being used.

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

Use `context.ReportDiagnostic(...)` or generate code comments to debug the source gen.

Build the source gen project then build the main project.

Repeat as needed.
