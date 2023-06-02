// See https://aka.ms/new-console-template for more information
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Tasks;
using Newtonsoft.Json;
using ShaderBuilder;

var inputFiles = args[0].Split(';');    // 输入文件路径
var intermediateDirectory = args[1];
var outputDir = args[2];                // 输出文件夹
var targetPlatform = args.ElementAtOrDefault(3);
var targetProfile = args.ElementAtOrDefault(4);
var buildConfiguration = args.ElementAtOrDefault(5);

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
foreach (var file in inputFiles)
{
	var output = @$"{outputDir}{Path.ChangeExtension(file, ".xnb")}";
	helper.LogMessage(MessageImportance.High, $"Building {file} -> {output}");
	try
	{
		var input = importer.Import(file, importContext);
		var content = processor.Process(input, processorContext);
		var info = new FileInfo(output);
		info.Directory.Create();
		using var stream = info.Create();
		contentCompiler.Compile(stream, content, settings.TargetPlatform, settings.TargetProfile, false, outputDir, outputDir);
	}
	catch (Exception ex)
	{
		helper.LogErrorFromException(ex);
	}
}

return 0;

namespace ShaderBuilder
{
	public class BuildEngine : IBuildEngine
	{
		private static readonly JsonSerializerSettings settings = new()
		{
			Converters = { new BuildEventArgsConverter() },
			Formatting = Formatting.None, // Must be None
		};

		public bool ContinueOnError => false;

		public int LineNumberOfTaskNode => 0;

		public int ColumnNumberOfTaskNode => 0;

		public string ProjectFileOfTaskNode => string.Empty;

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
		{
			throw new NotImplementedException("Not Implemented");
		}

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
			Console.WriteLine(JsonConvert.SerializeObject(e, settings));
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
			Console.WriteLine(JsonConvert.SerializeObject(e, settings));
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
			Console.WriteLine(JsonConvert.SerializeObject(e, settings));
		}
	}
}