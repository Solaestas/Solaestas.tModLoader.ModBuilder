using System.Diagnostics;
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
	[Required]
	public bool BuildIgnore { get; set; }

	[Required]
	public string ConfigPath { get; set; }

	public override bool Execute()
	{
		var config = JsonConvert.DeserializeObject<BuildConfig>(File.ReadAllText(ConfigPath)) ?? throw new Exception("Build Config not found");
		var info = new BuildInfo(config);
		Log.LogMessage(MessageImportance.High, $"Building {ModName} -> {Path.Combine(ModDirectory, $"{ModName}.tmod")}");
		var sw = Stopwatch.StartNew();

		var property = BuildProperties.ReadBuildFile(ModSourceDirectory, info);
		var tmod = new TmodFile(Path.Combine(ModDirectory, $"{ModName}.tmod"), ModName, property.Version);

		var assetDirectory = Path.Combine(OutputDirectory, "Assets") + Path.DirectorySeparatorChar;
		Log.LogMessage(MessageImportance.Normal, "Source Path: {0}", ModSourceDirectory);
		Log.LogMessage(MessageImportance.Normal, "Output Path: {0}", OutputDirectory);
		Log.LogMessage(MessageImportance.Normal, "Asset Path: {0}", assetDirectory);

		// Asset路径前缀长度
		var prefixLength = assetDirectory.Length;

		// Add dll and pdb
		var dllref = new HashSet<string>(property.DllReferences);
		var modref = new HashSet<string>(property.ModReferences.Select(mod => mod.Mod));

		// Add Assets
		Parallel.ForEach(AssetFiles, file =>
		{
			var identity = file.ItemSpec[prefixLength..];
			if (property.IgnoreFile(identity))
			{
				return;
			}
			bool compressed = tmod.AddFile(identity, file.ItemSpec);
		});

		prefixLength = ModSourceDirectory.Length;

		// Add Resources
		if (BuildIgnore)
		{
			Log.LogMessage(MessageImportance.Normal, "Read BuildIgnore from build.txt");
			var files = Directory.EnumerateFiles(ModSourceDirectory, "*", SearchOption.AllDirectories);
			Parallel.ForEach(files, file =>
			{
				var identity = file[prefixLength..];
				if (!property.IgnoreFile(identity) && !IgnoreFile(identity, property))
				{
					bool compressed = tmod.AddFile(identity, file);
				}
			});
		}
		else
		{
			Log.LogMessage(MessageImportance.Normal, "Read resources from MSBuild");
			Parallel.ForEach(ResourceFiles, file =>
			{
				var identity = file.GetMetadata("Path");
				if (string.IsNullOrEmpty(identity))
				{
					identity = file.GetMetadata("Identity");
				}
				bool compressed = tmod.AddFile(identity, file.ItemSpec);
			});
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

			tmod.AddFile($"lib\\{name}.dll", file);
		}

		// Add pdb
		var pdbPath = Path.Combine(OutputDirectory, $"{ModName}.pdb");
		if (File.Exists(pdbPath))
		{
			tmod.AddFile($"{ModName}.pdb", pdbPath);
			property.EacPath = pdbPath;
		}

		// Add Info
		property.DllReferences = [.. dllref];
		tmod.AddFile("Info", property.ToBytes());

		tmod.Save(info);
		Log.LogMessage(MessageImportance.High, $"Building Success, costs {sw.Elapsed}");

		return true;
	}

	public bool IgnoreFile(string path, BuildProperties properties)
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

		if (path is "icon.png")
		{
			return false;
		}

		if (path is "build.txt" or "description.txt" or "description_workshop.txt" or "LICENSE.txt")
		{
			return true;
		}

		if (!properties.IncludeSource && ext is ".cs" or ".fx" or ".csproj" or ".sln")
		{
			return true;
		}

		if (ext is ".png" or ".md" or ".cache")
		{
			return true;
		}

		return false;
	}
}