using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;
public class ReadConfig : Task
{
	[Output]
	public string ModDirectory { get; set; }

	[Output]
	public TmlVersoin TmlVersion { get; set; }

	[Output]
	public string BuildIdentifier { get; set; }

	[Required]
	public string ConfigPath { get; set; }
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
