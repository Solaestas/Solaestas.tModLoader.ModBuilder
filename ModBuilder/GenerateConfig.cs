using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public class GenerateConfig : Task
{
	/// <summary>
	/// Config文件保存的路径
	/// </summary>
	[Required]
	public string ConfigPath { get; set; } = default!;

	/// <summary>
	/// tModLoader游戏本体dll文件的路径
	/// </summary>
	[Required]
	public string DllPath { get; set; } = default!;

	public override bool Execute()
	{
		// Get the array of runtime assemblies.
		string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

		// Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.

		// Create PathAssemblyResolver that can resolve assemblies using the created list.
		var resolver = new PathAssemblyResolver([DllPath, .. runtimeAssemblies]);
		using var mlc = new MetadataLoadContext(resolver);
		var asm = mlc.LoadFromAssemblyName("tModLoader");
		var identifier = (string)asm.GetCustomAttributesData()
			.First(c => c.AttributeType.Name == nameof(AssemblyInformationalVersionAttribute))
			.ConstructorArguments[0].Value!;

		TmlVersoin version = identifier.Contains("dev") ? TmlVersoin.Developer
			: identifier.Contains("preview") ? TmlVersoin.Preview
			: identifier.Contains("stable") ? TmlVersoin.Stable
			: TmlVersoin.Legacy;

		Log.LogMessage(MessageImportance.High, LogText.DetectVersion, version.ToString());

		var ModDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			"My Games",
			"Terraria",
			version switch
			{
				TmlVersoin.Legacy => "tModLoader-1.4.3",
				TmlVersoin.Stable => "tModLoader",
				TmlVersoin.Preview => "tModLoader-preview",
				TmlVersoin.Developer => "tModLoader-dev",
				_ => throw new Exception("How to get here?"),
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