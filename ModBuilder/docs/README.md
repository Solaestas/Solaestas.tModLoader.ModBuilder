# Solaestas' ModBuilder

This is a NuGet package for tModLoader mod makers which modifies and automates mod building procedure.

document is not finished yet

## Feature

- Include referenced dll automatically
- Generate cache file for png
- Compile and Include fx files automatically
- Publicize FNA and tModLoader assembly using BepInEx.AssemblyPublicizer.MSBuild
- Generate asset path automatically (Optional)
- Disable other mods automatically in building (Optional)

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
		<!--Disable Publicizer-->
		<Solaestas-UsePublicizer>false</Solaestas-UsePublicizer>
		<!--Disable Other mod during building-->
		<Solaestas-UseMinimalMod>true</Solaestas-UseMinimalMod>
		<!--Only include files assigned by ResourceFile-->
		<Solaestas-BuildIgnore>false</Solaestas-BuildIgnore>
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

| Config Name               | Description                                                                                     | Default Value                   | Optional Values   |
| ------------------------- | ----------------------------------------------------------------------------------------------- | ------------------------------- | ----------------- |
| `Solaestas-UseAssetPath`  | Whether to generate asset path                                                                  | `true`                          | `true` or `false` |
| `Solaestas-UseMinimalMod` | Whether to disable other mods automatically in building                                         | `false`                         | `true` or `false` |
| `Solaestas-NotMod`        | Indicate that this project is not a mod.                                                        | `false`                         | `true` or `false` |
| `Solaestas-WhitelistMod`  | white list mod which avoid to be disable via `UseMinimalMod`                                    | `herosmod;cheatsheet;dragonlen` | `[Mod Name]`      |
| `Solaestas-BuildIgnore`   | `true`: read ignore files from build.txt<br>`false`: only read resource file defined in msbuild | `true`                          | `true` or `false` |
| `Solaestas-AssetPrefix`   | Prefix of asset path                                                                            | `string.Empty`                  | `[string]`        |
