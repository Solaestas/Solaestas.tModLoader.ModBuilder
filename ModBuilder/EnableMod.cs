using System.Collections.Immutable;
using Microsoft.Build.Framework;
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
	public bool AutoDisable { get; set; }

	[Required]
	public string ModDirectory { get; set; }
	public override bool Execute()
	{
		var path = Path.Combine(ModDirectory, "enabled.json");
		var json = File.Exists(path) ? JsonConvert.DeserializeObject<HashSet<string>>(File.ReadAllText(path)): [];

		var mods = DebugMod.Split(';');
		Log.LogMessage(MessageImportance.High, "Enable Mod: {0}, {1}", BuildingMod, string.Join(", ", mods));
		if (AutoDisable)
		{
			Log.LogMessage(MessageImportance.High, "Auto Disable Other Mods");
			json.Clear();
		}
		foreach(var mod in mods)
		{
			json.Add(mod);
		}
		json.Add(BuildingMod);

		File.WriteAllText(path, JsonConvert.SerializeObject(json));
		return true;
	}
}