using System;
using System.Collections.Generic;
using System.IO;
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
	public required string WhitelistMod { get; set; }

	/// <summary>
	/// 是否禁用其他Mod.
	/// </summary>
	public bool MinimalMod { get; set; }

	/// <summary>
	/// 配置文件路径
	/// </summary>
	[Required]
	public required string ConfigPath { get; set; }

	public override bool Execute()
	{
		var config = JsonConvert.DeserializeObject<BuildConfig>(File.ReadAllText(ConfigPath)) ?? throw new Exception("Build Config not found");

		var path = Path.Combine(config.ModDirectory, "enabled.json");
		var json = File.Exists(path) ? JArray.Parse(File.ReadAllText(path)) : new JArray();

		using var writer = File.CreateText(path);
		if (MinimalMod)
		{
			json = new JArray(new List<string>(WhitelistMod.Split(';'))
			{
				BuildingMod,
			}.ToArray());
		}
		else
		{
			if (!json.Contains(BuildingMod))
			{
				json.Add(BuildingMod);
			}

			if (WhitelistMod != null)
			{
				foreach (var mod in WhitelistMod)
				{
					if (!json.Contains(mod))
					{
						json.Add(mod);
					}
				}
			}
		}

		json.WriteTo(new JsonTextWriter(writer));
		return true;
	}
}