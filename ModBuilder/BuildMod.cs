using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Solaestas.tModLoader.ModBuilder.ModLoader;

namespace Solaestas.tModLoader.ModBuilder;

public enum ResourceStyle
{
	Blacklist,

	Whitelist,
}

public class BuildMod : Microsoft.Build.Utilities.Task
{
	/// <summary>
	/// 模组源码文件夹
	/// </summary>
	[Required]
	public string ModSourceDirectory { get; set; }

	/// <summary>
	/// 项目输出文件夹
	/// </summary>
	[Required]
	public string OutputDirectory { get; set; }

	/// <summary>
	/// 模组文件夹文件夹
	/// </summary>
	[Required]
	public string ModDirectory { get; set; }

	/// <summary>
	/// 模组名，默认为文件夹名 <br /> 模组名应该与ILoadable的命名空间和程序集名称相同
	/// </summary>
	public string ModName { get; set; }

	/// <summary>
	/// Debug or Release, 决定是否包含PDB
	/// </summary>
	public string Configuration { get; set; }

	/// <summary>
	/// 无预处理的资源文件，直接从源码中复制
	/// </summary>
	[Required]
	public ITaskItem[] ResourceFiles { get; set; }

	/// <summary>
	/// 需要经过预处理的特殊资源文件
	/// </summary>
	[Required]
	public ITaskItem[] AssetFiles { get; set; }

	/// <summary>
	/// 是否使用BuildIgnore来决定是否包含资源
	/// </summary>
	public ResourceStyle ResourceStyle { get; set; }

	[Required]
	public string ConfigPath { get; set; }

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
		var prefixLength = assetDirectory.Length + 1;

		// Add dll and pdb
		var dllref = new HashSet<string>(property.DllReferences);
		var modref = new HashSet<string>(property.ModReferences.Select(mod => mod.Mod));

		tmod.files = new Dictionary<string, TmodFile.FileEntry>();

		// Add Assets
		foreach (var file in AssetFiles)
		{
			var identity = file.ItemSpec[prefixLength..];
			tmod.AddFile(identity, file.ItemSpec);
		}

		// Add Resources
		if (ResourceStyle == ResourceStyle.Blacklist)
		{
			var files = Directory.EnumerateFiles(ModSourceDirectory, "*", SearchOption.AllDirectories);
			foreach (var file in files)
			{
				var identity = file[prefixLength..];
				if (!property.IgnoreFile(identity) && !IgnoreFile(identity))
				{
					tmod.AddFile(identity, file);
				}
			}
		}
		else if (ResourceStyle == ResourceStyle.Whitelist)
		{
			foreach (var file in ResourceFiles)
			{
				var identity = file.GetMetadata("Path");
				if(string.IsNullOrEmpty(identity))
				{
					identity = file.GetMetadata("Identity");
				}
				tmod.AddFile(identity, file.ItemSpec);
			}
		}
		else
		{
			throw new Exception("Unknown resource style");
		}

		// Add dll
		foreach (var file in Directory.EnumerateFiles(OutputDirectory, "*.dll", SearchOption.TopDirectoryOnly))
		{
			var name = Path.GetFileNameWithoutExtension(file);

			if (name == ModName)
			{
				tmod.AddFile($"{name}.dll", file);
				continue;
			}

			if (modref.Contains(name))
			{
				continue;
			}

			if (!dllref.Contains(name))
			{
				dllref.Add(name);
			}

			tmod.AddFile($"lib/{name}.dll", file);
		}

		// Add pdb
		var pdbPath = Path.Combine(OutputDirectory, $"{ModName}.pdb");
		if (File.Exists(pdbPath))
		{
			tmod.AddFile($"{ModName}.pdb", pdbPath);
			property.EacPath = pdbPath;
		}

		// Add Info
		property.DllReferences = dllref.ToArray();
		tmod.AddFile("Info", property.ToBytes());

		tmod.Save(info);
		Log.LogMessage(MessageImportance.High, $"Building Success, costs {sw.Elapsed}");

		return true;
	}

	public bool IgnoreFile(string path)
	{
		var ext = Path.GetExtension(path);

		if (path[0] == '.')
		{
			return true;
		}

		if (path.StartsWith("bin", StringComparison.Ordinal)
			|| path.StartsWith("obj", StringComparison.Ordinal)
			|| path.StartsWith("Properties", StringComparison.Ordinal))
		{
			return true;
		}

		if (path is "icon.png" or "build.txt" or "description.txt")
		{
			return true;
		}

		if (ext is ".png" or ".cs" or ".md" or ".cache" or ".fx")
		{
			return true;
		}

		return false;
	}
}