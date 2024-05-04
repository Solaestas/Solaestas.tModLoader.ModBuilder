using System.Diagnostics;
using Microsoft.Build.Framework;
using Solaestas.tModLoader.ModBuilder.ModLoader;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public class PackAsset : Task
{
	[Required]
	public ITaskItem[] Assets { get; set; } = default!;

	[Required]
	public string OutputPath { get; set; } = default!;

	public override bool Execute()
	{
		var tmod = new TmodFile(string.Empty, string.Empty, new Version());
		Log.LogMessage(MessageImportance.High, LogText.PackAsset);
		var sw = Stopwatch.StartNew();

		// Add Resources
		Parallel.ForEach(Assets, file =>
		{
			if (!bool.TryParse(file.GetMetadata("Pack"), out var pack) || !pack)
			{
				return;
			}
			var identity = file.GetMetadata("ModPath");
			Log.LogMessage(MessageImportance.Normal, LogText.AddResource, identity, file.ItemSpec);
			tmod.AddFile(identity, file.ItemSpec);
		});

		using var file = File.Create(OutputPath);
		tmod.WriteFileTable(file);
		Log.LogMessage(MessageImportance.High, LogText.PackSuccess, sw.Elapsed);
		return true;
	}
}