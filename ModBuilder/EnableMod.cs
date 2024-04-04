using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public class EnableMod : Task
{
	[Required]
	public required string BuildingMod { get; set; }

	/// <summary>
	/// 用于调试Mod的工具Mod，如Hero，CheatSheet，用;分开.
	/// </summary>
	public required string DebugMod { get; set; }

	/// <summary>
	/// 是否禁用其他Mod.
	/// </summary>
	public bool MinimalMod { get; set; }

	[Required]
	public string ModDirectory { get; set; }
	public override bool Execute()
	{
		var path = Path.Combine(ModDirectory, "enabled.json");
		var json = File.Exists(path) ? JArray.Parse(File.ReadAllText(path)) : [];

		using var writer = File.CreateText(path);
		using var jsonWriter = new JsonTextWriter(writer);
		if (MinimalMod)
		{
			json = [.. DebugMod.Split(';'), BuildingMod];
		}
		else
		{
			if (!json.Contains(BuildingMod))
			{
				json.Add(BuildingMod);
			}

			foreach (var mod in DebugMod.Split(';'))
			{
				if (!json.Contains(mod))
				{
					json.Add(mod);
				}
			}
		}

		json.WriteTo(new JsonTextWriter(writer));
		return true;
	}
}