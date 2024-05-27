using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace TestMod;

public class TestMod : Mod
{
	public override void Load()
	{
		// Format file table
		const int left = 30;
		const int middle = 10;
		const int right = 10;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine();
		stringBuilder.Append(' ').Append($"{"Name",-left}│")
			.Append(' ').Append($"{"Length",-middle}│")
			.Append(' ').AppendLine($"{"Compressed",-right}");
		stringBuilder.Append($"{new string('─', left + 1)}┼")
			.Append($"{new string('─', middle + 1)}┼")
			.AppendLine($"{new string('─', right + 1)}");
		foreach (var entry in File.fileTable)
		{
			stringBuilder.Append(' ').Append($"{entry.Name,-left}│")
				.Append(' ').Append($"{entry.Length,-middle}│")
				.Append(' ').AppendLine($"{entry.Length == entry.CompressedLength,-right}");
		}
		Logger.Info(stringBuilder.ToString());

		// Access private field
		Logger.Info(Main.spriteBatch.sortMode);
		Logger.Info(Main._renderTargetMaxSize);

		// Print asset path
		Logger.Info(ModAsset.TestTXT_Path);
	}
}