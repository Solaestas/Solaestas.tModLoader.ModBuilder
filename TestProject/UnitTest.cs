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
}