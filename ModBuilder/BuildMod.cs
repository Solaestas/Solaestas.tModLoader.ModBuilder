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

	[Required]
	public ITaskItem Asset { get; set; } = default!;

	[Required]
	public ITaskItem[] ModReference { get; set; } = default!;

	[Required]
	public ITaskItem DebugSymbol { get; set; } = default!;

	[Required]
	public ITaskItem ModAssembly { get; set; } = default!;

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

		// Add dll and pdb
		var dllref = new HashSet<string>(property.DllReferences);
		var modref = new HashSet<string>(property.ModReferences.Select(mod => mod.Mod));

		// Add dll and pdb
		tmod.AddFile($"{ModName}.dll", ModAssembly.ItemSpec);
		Log.LogMessage(MessageImportance.Normal, LogText.AddAssembly, $"{ModName}.dll", ModAssembly.ItemSpec);
		tmod.AddFile($"{ModName}.pdb", DebugSymbol.ItemSpec);
		property.EacPath = DebugSymbol.ItemSpec;
		Log.LogMessage(MessageImportance.Normal, LogText.AddDebugSymbol, $"{ModName}.pdb", DebugSymbol.ItemSpec);
		foreach (var lib in ModReference)
		{
			var filename = lib.GetMetadata("Filename");
			if (modref.Contains(filename))
			{
				continue;
			}
			dllref.Add(filename);
			string path = $"lib\\{filename}.dll";
			tmod.AddFile(path, lib.ItemSpec);
			Log.LogMessage(MessageImportance.Normal, LogText.AddReference, path, lib.ItemSpec);
		}

		// Add Info
		property.DllReferences = [.. dllref];
		tmod.AddFile("Info", property.ToBytes());

		using var assetFile = File.OpenRead(Asset.ItemSpec);
		tmod.Save(info, assetFile);
		Log.LogMessage(MessageImportance.High, LogText.BuildSuccess, sw.Elapsed);

		return true;
	}
}