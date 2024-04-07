using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public class ReadConfig : Task
{
	[Output]
	public string ModDirectory { get; set; } = default!;

	[Output]
	public TmlVersoin TmlVersion { get; set; }

	[Output]
	public string BuildIdentifier { get; set; } = default!;

	[Required]
	public string ConfigPath { get; set; } = default!;

	public override bool Execute()
	{
		using var reader = new StreamReader(ConfigPath);
		var config = JsonConvert.DeserializeObject<BuildConfig>(reader.ReadToEnd()) ?? throw new Exception("Build Config not found");
		ModDirectory = config.ModDirectory;
		TmlVersion = config.TmlVersion;
		BuildIdentifier = config.BuildIdentifier;
		return true;
	}
}