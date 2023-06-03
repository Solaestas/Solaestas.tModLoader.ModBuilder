using System.Diagnostics;
using CliWrap;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using ShaderBuilder;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public class BuildEffect : Task
{
	/// <summary>
	/// 输入文件
	/// </summary>
	[Required]
	public string InputFiles { get; set; }

	/// <summary>
	/// 中间文件夹
	/// </summary>
	[Required]
	public string IntermediateDirectory { get; set; }

	/// <summary>
	/// 输出文件夹
	/// </summary>
	[Required]
	public string OutputDirectory { get; set; }

	/// <summary>
	/// Builder路径
	/// </summary>
	[Required]
	public string BuilderDirectory { get; set; }

	/// <summary>
	/// Support Platform : Windows, Xbox360, WindowsPhone
	/// </summary>
	public string TargetPlatform { get; set; }

	/// <summary> Support Profile : HiDef, Reach <br> And I don't know what they mean </summary>
	public string TargetProfile { get; set; }

	/// <summary>
	/// Configuration
	/// </summary>
	public string BuildConfiguration { get; set; }

	public override bool Execute()
	{
		bool success = true;
		Log.LogMessage(MessageImportance.High, "Building Effects...");
		var filename = $"{BuilderDirectory}ShaderBuilder.exe";
		var args = new List<string>()
		{
			InputFiles,
			IntermediateDirectory,
			OutputDirectory,
			TargetPlatform,
			TargetProfile,
			BuildConfiguration,
		};

		var cli = Cli.Wrap(filename).WithArguments(args)
		| PipeTarget.ToDelegate(s =>
		{
			var args = JsonConvert.DeserializeObject<BuildEventArgs>(s, new BuildEventArgsConverter());
			if (args is BuildMessageEventArgs msg)
			{
				BuildEngine.LogMessageEvent(msg);
			}
			else if (args is BuildWarningEventArgs warning)
			{
				BuildEngine.LogWarningEvent(warning);
			}
			else if (args is BuildErrorEventArgs error)
			{
				BuildEngine.LogErrorEvent(error);
				success = false;
			}
		});
		cli.ExecuteAsync().Task.Wait();
		return success;
	}
}