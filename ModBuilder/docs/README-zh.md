# Solaestas' ModBuilder

这是一个用于 tModLoader 模组制作者的 NuGet 包，用于修改和自动化模组构建过程。

文档尚未完善

## 功能

- 自动添加引用 dll
- 自动编译fx文件
- 使用`BepInEx.AssemblyPublicizer.MSBuild`公有化tModLoader和FNA程序集
- 自动生成资源路径（默认开启）
- 自动禁用其他末端组（默认关闭）

## 可配置项

所有配置项都是 MSBuild 属性，可以在 `csproj` 文件或 `Directory.Build.props` 文件中设置。

下面为一个示例Mod的 `csproj` 文件：

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
		<!--启用自动禁用其他Mod-->
		<AutoDisableMod>true</AutoDisableMod>
		<!--修改自动路径的类目-->
		<PathTypeName>ModPath</PathTypeName>
	</PropertyGroup>

	<ItemGroup>
		<!--将所有bmp文件打包进Mod-->
		<ResourceFile Include="**/*.bmp" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="tModLoader.CodeAssist" Version="0.1.*" />
	</ItemGroup>
</Project>
```

| Config Name           | Description              | Default Value                    | Optional Values   |
| --------------------- | ------------------------ | -------------------------------- | ----------------- |
| `EnablePathGenerator` | 自动生成对资源路径的引用     | `true`                           | `true` or `false` |
| `AutoDisableMod`      | 自动禁用其他Mod          | `false`                          | `true` or `false` |
| `EnableModBuilder`    | 为这个项目生成Mod文件    | `true`                           | `true` or `false` |
| `DebugMod`            | 自动启用指定Mod          | `HEROsMod;CheatSheet;DragonLens` | `[Mod Name]`      |
| `PathPrefix`          | 自动路径的前缀           | `string.Empty`                   | `[string]`        | 
| `PathNamespace`       | 自动路径的命名空间       | `$(RootNamespace)`               | `[string]`        |
| `PathTypeName`        | 自动路径的类名           | `ModAsset`                       | `[string]`        |
