using System.Configuration;
using Microsoft.Build.Utilities;
using Solaestas.tModLoader.ModBuilder;

namespace TestProject;

[TestClass]
public class UnitTest
{
	private static string TestModDirectory => Path.GetFullPath("../../../../TestMod/");

	private const string BuildConfigPath = "Resources/BuildConfig.json";

	private Mock<IBuildEngine> buildEngine = default!;

	private List<BuildErrorEventArgs> errors = default!;

	private List<BuildMessageEventArgs> messages = default!;

	[TestInitialize()]
	public void Startup()
	{
		buildEngine = new Mock<IBuildEngine>();
		errors = [];
		messages = [];
		buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
			.Callback<BuildErrorEventArgs>(e => errors.Add(e));
		buildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
			.Callback<BuildMessageEventArgs>(e => messages.Add(e));
	}

	[TestMethod]
	public void TestBuildMod()
	{
		var engine = buildEngine.Object;

		var assetFile = new List<TaskItem>();
		var outputPath = Path.Combine(TestModDirectory, "bin", "Debug", "net6.0");
		var assetPath = Path.Combine(outputPath, "Assets");
		foreach (var asset in Directory.EnumerateFiles(assetPath, "*", SearchOption.AllDirectories))
		{
			assetFile.Add(new TaskItem(Path.GetFullPath(asset)));
		}
		var task = new BuildMod()
		{
			BuildEngine = engine,
			AssetFiles = assetFile.ToArray(),
			ModName = "TestMod",
			ModDirectory = "Mods/",
			BuildIgnore = true,
			ConfigPath = BuildConfigPath,
			OutputDirectory = outputPath,
			ModSourceDirectory = TestModDirectory,
		};
		task.Execute();
		foreach (var error in errors)
		{
			Console.Error.WriteLine(error.Message);
		}

		foreach (var message in messages)
		{
			Console.WriteLine(message.Message);
		}
	}
}