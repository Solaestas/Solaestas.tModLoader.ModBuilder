using Terraria;
using Terraria.ModLoader;

namespace TestMod;

public class TestSystem : ModSystem
{
	public override void OnWorldLoad()
	{
		Main.NewText(ModAsset.TestTXTPath);
	}
}