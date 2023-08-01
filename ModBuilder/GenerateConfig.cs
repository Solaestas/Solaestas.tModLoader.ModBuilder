using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public class GenerateConfig : Task
{
	/// <summary>
	/// tModLoader游戏本体dll文件的路径
	/// </summary>
	[Required]
	public required string GamePath { get; set; }

	/// <summary>
	/// 输出Mod文件夹的路径
	/// </summary>
	[Output]
	public string ModDirectory { get; set; } = string.Empty;

	/// <summary>
	/// Config文件保存的路径
	/// </summary>
	[Required]
	public required string ConfigPath { get; set; }

	public override bool Execute()
	{
		// Get the array of runtime assemblies.
		string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

		// Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
		var paths = new List<string>(runtimeAssemblies)
		{
			GamePath,
		};

		// Create PathAssemblyResolver that can resolve assemblies using the created list.
		var resolver = new PathAssemblyResolver(paths);
		using var mlc = new MetadataLoadContext(resolver);
		var asm = mlc.LoadFromAssemblyName("tModLoader");
		var identifier = (string)asm!.GetCustomAttributesData()
			.First(c => c.AttributeType.Name == nameof(AssemblyInformationalVersionAttribute))
			.ConstructorArguments[0].Value!;

		GameVersion version = identifier.Contains("dev") ? GameVersion.Developer
			: identifier.Contains("preview") ? GameVersion.Preview
			: identifier.Contains("stable") ? GameVersion.Stable
			: GameVersion.Legacy;

		Log.LogMessage(MessageImportance.High, "tModLoader Version: {0}", version.ToString());

		ModDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			"My Games",
			"Terraria",
			version switch
			{
				GameVersion.Legacy => "tModLoader-1.4.3",
				GameVersion.Stable => "tModLoader",
				GameVersion.Preview => "tModLoader-preview",
				GameVersion.Developer => "tModLoader-dev",
				_ => throw new Exception("How to get here?")
			},
			"Mods\\");

		if (!Directory.Exists(ModDirectory))
		{
			Directory.CreateDirectory(ModDirectory);
		}

		var json = JsonConvert.SerializeObject(new BuildConfig(ModDirectory, version, identifier));

		File.WriteAllText(ConfigPath, json);
		return true;
	}
}
