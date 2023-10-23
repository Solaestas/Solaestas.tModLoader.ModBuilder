using Terraria;
using Terraria.ModLoader;

namespace TestMod;

public class TestSystem : ModSystem
{
	public override void OnWorldLoad()
	{
		// Access private field
		Main.NewText(Main.spriteBatch.sortMode);
		Main.NewText(Main._renderTargetMaxSize);
		// Print asset path
		Main.NewText(ModAsset.TestTXTPath);
	}
}