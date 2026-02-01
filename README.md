# PacketGen
Source generator for the netcode packet scripts in https://github.com/CSharpGodotTools/Template

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
