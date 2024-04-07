using System.Diagnostics;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Solaestas.tModLoader.ModBuilder.ModLoader;

namespace Solaestas.tModLoader.ModBuilder;

public class BuildMod : Microsoft.Build.Utilities.Task
{
	[Required]
	public string BuildIdentifier { get; set; } = default!;

	[Required]
	public string ModDirectory { get; set; } = default!;

	/// <summary>
	/// 模组名，默认为文件夹名 <br /> 模组名应该与ILoadable的命名空间和程序集名称相同
	/// </summary>
	[Required]
	public string ModName { get; set; } = default!;

	/// <summary>
	/// 模组源码文件夹
	/// </summary>
	[Required]
	public string ModSourceDirectory { get; set; } = default!;

	/// <summary>
	/// 项目输出文件夹
	/// </summary>
	[Required]
	public string OutputDirectory { get; set; } = default!;

	/// <summary>
	/// 无预处理的资源文件，直接从源码中复制
	/// </summary>
	[Required]
	public ITaskItem[] ResourceFiles { get; set; } = default!;

	[Required]
	public string TmlVersion { get; set; } = default!;

	public override bool Execute()
	{
		_ = Enum.TryParse<TmlVersoin>(TmlVersion, out var tmlVersion);
		var info = new BuildInfo(BuildIdentifier, tmlVersion);
		string modPath = Path.Combine(ModDirectory, $"{ModName}.tmod");
		Log.LogMessage(MessageImportance.High, LogText.BuildMod, ModName, modPath);
		var sw = Stopwatch.StartNew();

		var property = BuildProperties.ReadBuildFile(ModSourceDirectory, info);
		var tmod = new TmodFile(modPath, ModName, property.Version);

		Log.LogMessage(MessageImportance.Normal, LogText.Source, ModSourceDirectory);
		Log.LogMessage(MessageImportance.Normal, LogText.Output, OutputDirectory);

		// Add dll and pdb
		var dllref = new HashSet<string>(property.DllReferences);
		var modref = new HashSet<string>(property.ModReferences.Select(mod => mod.Mod));

		// Add Resources
		Parallel.ForEach(ResourceFiles, file =>
		{
			if (!bool.TryParse(file.GetMetadata("Pack"), out var pack) || !pack)
			{
				return;
			}
			var identity = file.GetMetadata("ModPath");
			Log.LogMessage(MessageImportance.Normal, LogText.AddResource, identity, file.ItemSpec);
			tmod.AddFile(identity, file.ItemSpec);
		});

		// Add dll
		foreach (var file in Directory.EnumerateFiles(OutputDirectory, "*.dll", SearchOption.TopDirectoryOnly))
		{
			var name = Path.GetFileNameWithoutExtension(file);

			if (name == ModName)
			{
				Log.LogMessage(MessageImportance.Normal, LogText.AddAssembly, name, file);
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

			Log.LogMessage(MessageImportance.Normal, LogText.AddReference, name, file);
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
		Log.LogMessage(MessageImportance.High, LogText.BuildSuccess, sw.Elapsed);

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