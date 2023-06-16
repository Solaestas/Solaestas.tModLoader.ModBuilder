# Solaestas' ModBuilder

This is a NuGet package for tModLoader mod makers which modifies and automates mod building procedure.

document is not finished yet

## Config List

| Config Name | Description | Default Value | Optional Values |
| --- | --- | --- | --- |
| `Solaestas-UseAssetPath` | Whether to generate asset path | `true` | `true` or `false` |
| `Solaestas-UsePublicizer` | Whether to reference BepInEx.AssemblyPublicizer.MSBuild | `true` | `true` or `false` |
| `Solaestas-UseMinimalMod` | Whether to disable other mods automatically in building | `false` | `true` or `false` |
| `Solaestas-NotMod` | Indicate that this project is not a mod. | `false` | `true` or `false` |
| `Solaestas-WhitelistMod` | white list mod which avoid to be disable via `UseMinimalMod` | `herosmod;cheatsheet;dragonlen` | `[Mod Name]` |
| `Solaestas-BuildIgnore` | `true`: read ignore files from build.txt<br>`false`: only read resource file defined in msbuild| `true` | `true` or `false` |
| `Solaestas-AssetPrefix` | Prefix of asset path | `string.Empty` | `[string]` |

## TODO

config support

docs
