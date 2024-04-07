using System;
using System.Collections;
using Microsoft.Build.Framework;
using Newtonsoft.Json;

namespace ShaderBuilder;

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