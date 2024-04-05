# Solaestas' ModBuilder

This is a NuGet package for tModLoader mod makers which modifies and automates mod building procedure.

document is not finished yet

## Feature

- Include referenced dll automatically
- Compile and Include fx files automatically
- Publicize FNA and tModLoader assembly using BepInEx.AssemblyPublicizer.MSBuild
- Generate asset path automatically (default: on)
- Disable other mods automatically in building (default: off)

## Supported Config

The following configs are all MSBuild Properties, which can be set in `csproj` file or `Directory.Build.props` file.

This is an example of TestMod.csproj.

```xml
<Project>
	<Import Project="..\tModLoader.targets" />

	<PropertyGroup>
		<AssemblyName>TestMod</AssemblyName>
		<TargetFramework>net6.0</TargetFramework>
		<PlatformTarget>AnyCPU</PlatformTarget>
		<LangVersion>latest</LangVersion>
	</PropertyGroup>

	<PropertyGroup>
		<!--Disable Other mod during building-->
		<AutoDisableMod>true</AutoDisableMod>
	</PropertyGroup>

	<ItemGroup>
		<!--Include all bmp file-->
		<ResourceFile Include="**/*.bmp" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="tModLoader.CodeAssist" Version="0.1.*" />
	</ItemGroup>
</Project>
```

| Config Name           | Description                          | Default Value                    | Optional Values   |
| --------------------- | ------------------------------------ | -------------------------------- | ----------------- |
| `EnablePathGenerator` | Generate asset path                  | `true`                           | `true` or `false` |
| `AutoDisableMod`      | Automatically disable other mods     | `false`                          | `true` or `false` |
| `EnableModBuilder`    | Indicate that this project is a mod. | `true`                           | `true` or `false` |
| `DebugMod`            | Automatically enable debug mods      | `HEROsMod;CheatSheet;DragonLens` | `[Mod Name]`      |
| `PathPrefix`          | Prefix of asset path                 | `string.Empty`                   | `[string]`        |
| `PathNamespace`       | Namespace of asset path              | `$(RootNamespace)`               | `[string]`        |
| `PathTypeName`        | Type name of asset path              | `ModAsset`                       | `[string]`        |
