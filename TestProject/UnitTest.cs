using System.Text;
using Solaestas.tModLoader.ModBuilder.Generators;

namespace TestProject;

[TestClass]
public class UnitTest
{
	[TestMethod]
	public void TestTest()
	{
		Assert.AreEqual(1 + 1, 2);
		Console.WriteLine(Terraria.ModLoader.ModLoader.versionedName);
	}

	[TestMethod]
	public void ValidTest()
	{
		Assert.IsTrue(PathGenerator.CheckValid(@"BaseMod\Test"));
		Assert.IsTrue(PathGenerator.CheckValid(@"BaseMod/Slash"));
		Assert.IsTrue(PathGenerator.CheckValid(@"BaseMod_Test.png"));
		Assert.IsTrue(PathGenerator.CheckValid(@"BaseMod\Test Sword"));
		Assert.IsFalse(PathGenerator.CheckValid(@"BaseMod\测试武器"));
	}

	[TestMethod]
	public void DefaultPathTest()
	{
		PathMember[] paths =
		[
			"Mod/A.png",
			"Mod/B.xnb",
			"Mod/Items/C.json",
			"Mod/Items/D",
		];

		var sb = new StringBuilder();
		var builder = new PathBuilder(sb, "Test", string.Empty);
		foreach (var path in paths)
		{
			builder.Append(path);
		}

		Console.WriteLine(sb.ToString());
	}

	[TestMethod]
	public void FullNameTest()
	{
		List<PathMember> paths = [];
		Dictionary<string, List<PathMember>> conflict = new()
		{
			["A"] = ["Mod/A.json", "Mod/A.atlas", "Mod/A.png", "Mod/A.xnb"],
			["B"] = ["Mod/Items/B.xnb", "Mod/Items/B.png", "Mod/Projectiles/B.png"],
			["C"] = ["Mod/X/C.png", "Mod/Y/C.png", "Mod/Z/C.png", "Mod/Z/C.json", "Mod/Z/C.atlas"],
		};

		PathGenerator.TryFullName(paths, conflict);

		// 全部使用全名
		Assert.IsFalse(conflict.ContainsKey("A"));

		// 全部不使用全名
		Assert.IsTrue(conflict.ContainsKey("B"));
		Assert.AreEqual(3, conflict["B"].Count);

		// 部分使用全名
		Assert.IsTrue(conflict.ContainsKey("C"));
		Assert.AreEqual(3, conflict["C"].Count);

		var sb = new StringBuilder();
		var builder = new PathBuilder(sb, "Test", string.Empty);
		foreach (var path in paths)
		{
			builder.Append(path);
		}

		Console.WriteLine(sb.ToString());
	}

	[TestMethod]
	public void AddDepthTest()
	{
		List<PathMember> paths = [];
		Dictionary<string, List<PathMember>> conflict = new()
		{
			["A"] = ["Mod/X/A.png", "Mod/Y/A.png", "Mod/Z/A.png"],
		};

		PathGenerator.TryAddDepth(paths, conflict);

		Assert.IsFalse(conflict.ContainsKey("A"));
		Assert.AreEqual("X/A", paths[0].Name.ToString());
		Assert.AreEqual("Y/A", paths[1].Name.ToString());
		Assert.AreEqual("Z/A", paths[2].Name.ToString());

		var sb = new StringBuilder();
		var builder = new PathBuilder(sb, "Test", string.Empty);
		foreach (var path in paths)
		{
			builder.Append(path);
		}

		Console.WriteLine(sb.ToString());
	}

	[TestMethod]
	public void SoundTest()
	{
		PathMember path = "Mod/A.ogg";

		var sb = new StringBuilder();
		var builder = new PathBuilder(sb, "Test", string.Empty);

		builder.Append(path);

		Console.WriteLine(sb.ToString());
	}
}