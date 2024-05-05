using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Tasks;
using ShaderBuilder;

var inputFiles = args[0].Split(';');
var intermediateDirectory = args[1];
var outputDir = args[2];
var targetPlatform = args[3];
var targetProfile = args[4];
var buildConfiguration = args[5];

var settings = new BuildCoordinatorSettings()
{
	TargetPlatform = BuildTaskUtils.ConvertToPlatformEnum(targetPlatform),
	TargetProfile = BuildTaskUtils.ConvertToProfileEnum(targetProfile),
	BuildConfiguration = buildConfiguration,
	CompressContent = false,
	RootDirectory = null,
	LoggerRootDirectory = null,
	IntermediateDirectory = intermediateDirectory,
	OutputDirectory = outputDir,
	RebuildAll = false,
};

var engine = new BuildEngine();
var helper = new TaskLoggingHelper(engine, "BuildEffect");
var logger = new MSBuildLogger(helper);
var timestampCache = new TimestampCache();

var buildCoordinator = new BuildCoordinator(logger, settings, timestampCache);
var importer = new EffectImporter();
var processor = new EffectProcessor();
var buildItem = new BuildItem()
{
	BuildRequest = new BuildRequest(),
};
var importContext = new XnaContentImporterContext(buildCoordinator, buildItem, logger);
var processorContext = new XnaContentProcessorContext(buildCoordinator, buildItem, logger, settings.TargetPlatform, settings.TargetProfile, settings.BuildConfiguration);
var contentCompiler = new ContentCompiler();
try
{
	Parallel.ForEach(inputFiles, file =>
	{
		var fileInfo = new FileInfo(file);
		var output = @$"{outputDir}{Path.ChangeExtension(file, ".xnb")}";
		if (File.GetLastWriteTimeUtc(file) < File.GetLastWriteTimeUtc(output))
		{
			helper.LogMessage(MessageImportance.Normal, "Skip {0}", file);
			return;
		}
		helper.LogMessage(MessageImportance.Normal, "Building {0} -> {1}", file, output);
		var input = importer.Import(file, importContext);
		var content = processor.Process(input, processorContext);
		var info = new FileInfo(output);
		info.Directory.Create();
		int retry = 0;
	Retry:
		try
		{
			using var stream = info.Create();
			contentCompiler.Compile(stream, content, settings.TargetPlatform, settings.TargetProfile, false, outputDir, outputDir);
		}
		catch (IOException ex)
		{
			if (++retry > 10)
			{
				throw ex;
			}
			helper.LogWarningFromException(ex);
			helper.LogMessage("Retrying...");
			Thread.Sleep(TimeSpan.FromMilliseconds(10));
			goto Retry;
		}
	});
}
catch (Exception ex)
{
	helper.LogErrorFromException(ex.InnerException);
	return -1;
}

return 0;