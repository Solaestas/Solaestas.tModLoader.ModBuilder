using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Solaestas.tModLoader.ModBuilder.ModLoader;

namespace Solaestas.tModLoader.ModBuilder;

public class BuildMod : Microsoft.Build.Utilities.Task
{
	/// <summary>
	/// 模组源码文件夹
	/// </summary>
	[Required]
	public required string ModSourceDirectory { get; set; }

	/// <summary>
	/// 项目输出文件夹
	/// </summary>
	[Required]
	public required string OutputDirectory { get; set; }

	/// <summary>
	/// 模组文件夹文件夹
	/// </summary>
	[Required]
	public required string ModDirectory { get; set; }

	/// <summary>
	/// 模组名，默认为文件夹名 <br/> 模组名应该与ILoadable的命名空间和程序集名称相同
	/// </summary>
	public required string ModName { get; set; }

	/// <summary>
	/// Debug or Release, 决定是否包含PDB
	/// </summary>
	public required string Configuration { get; set; }

	/// <summary>
	/// 无预处理的资源文件，直接从源码中复制
	/// </summary>
	[Required]
	public required ITaskItem[] ResourceFiles { get; set; }

	/// <summary>
	/// 需要经过预处理的特殊资源文件
	/// </summary>
	[Required]
	public required ITaskItem[] AssetFiles { get; set; }

	/// <summary>
	/// 是否使用BuildIgnore来决定是否包含资源
	/// </summary>
	public bool IgnoreBuildFile { get; set; }

	[Required]
	public required string ConfigPath { get; set; }

	public override bool Execute()
	{
		var config = JsonConvert.DeserializeObject<BuildConfig>(File.ReadAllText(ConfigPath)) ?? throw new Exception("Build Config not found");
		var info = new BuildInfo(config);
		Log.LogMessage(MessageImportance.High, "Building Mod...");
		Log.LogMessage(MessageImportance.High, $"Building {ModName} -> {Path.Combine(ModDirectory, $"{ModName}.tmod")}");
		var sw = Stopwatch.StartNew();

		var property = BuildProperties.ReadBuildFile(ModSourceDirectory);
		var tmod = new TmodFile(Path.Combine(ModDirectory, $"{ModName}.tmod"), ModName, property.Version);

		var assetDirectory = Path.Combine(OutputDirectory, "Assets") + Path.DirectorySeparatorChar;

		// Add dll and pdb
		var set = new HashSet<string>(property.DllReferences);
		var modref = new HashSet<string>(property.ModReferences.Select(mod => mod.Mod));
		IEnumerable<KeyValuePair<string, TmodFile.FileEntry>> CreateInfo()
		{
			property.DllReferences = set.ToArray();
			yield return tmod.AddFile("Info", property.ToBytes());
		}
		tmod.files =
			AssetFiles.Select(file => tmod.AddFile(file.ItemSpec.Replace(assetDirectory, string.Empty), file.ItemSpec))
			.Concat(IgnoreBuildFile ?
			ResourceFiles.Select(file => tmod.AddFile(file.GetMetadata("ModPath"), file.ItemSpec)) :
			Directory.GetFiles(ModSourceDirectory, "*", SearchOption.AllDirectories)
			.Select(s => (Identity: s.Replace(ModSourceDirectory, string.Empty), FullPath: s))
			.Where(s => !property.IgnoreFile(s.Identity) && !IgnoreFile(s.Identity))
			.Select(file => tmod.AddFile(file.Identity, file.FullPath))).Concat(
			Directory.GetFiles(OutputDirectory, "*", SearchOption.TopDirectoryOnly)
			.Where(s => !modref.Contains(Path.GetFileNameWithoutExtension(s)))
			.Select(file =>
			{
				var ext = Path.GetExtension(file);
				var name = Path.GetFileNameWithoutExtension(file);

				if (name == ModName)
				{
					if (ext is ".dll")
					{
						return tmod.AddFile(name + ext, file);
					}
					else if (ext is ".pdb")
					{
						property.EacPath = file;
						return tmod.AddFile(name + ext, file);
					}
				}

				if (ext == ".dll")
				{
					if (!set.Contains(name))
					{
						set.Add(name);
					}

					return tmod.AddFile($"lib/{name}.dll", file);
				}

				return default;
			}).Where(v => v.Key != default))
			.Concat(CreateInfo())
			.Distinct(new CCC())
			.ToImmutableDictionary(v => v.Key, v => v.Value);

		tmod.Save(info);
		Log.LogMessage(MessageImportance.High, $"Building Success, costs {sw.Elapsed}");

		return true;
	}

	public class CCC : EqualityComparer<KeyValuePair<string, TmodFile.FileEntry>>
	{
		public override bool Equals(KeyValuePair<string, TmodFile.FileEntry> x, KeyValuePair<string, TmodFile.FileEntry> y)
		{
			return x.Key == y.Key;
		}

		public override int GetHashCode(KeyValuePair<string, TmodFile.FileEntry> obj)
		{
			return obj.Key.GetHashCode();
		}
	}

	public bool IgnoreFile(string path)
	{
		var ext = Path.GetExtension(path);

		if (path[0] == '.')
		{
			return true;
		}

		return path[0] == '.' || path.StartsWith("bin", StringComparison.Ordinal) || path.StartsWith("obj", StringComparison.Ordinal) || path.StartsWith("Properties", StringComparison.Ordinal)
			? true
			: path == "icon.png" ? false : ext is ".png" or ".cs" or ".md" or ".txt" or ".cache" or ".fx";
	}
}