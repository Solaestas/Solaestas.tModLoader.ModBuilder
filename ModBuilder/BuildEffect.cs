using System.Diagnostics;
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
	public required string InputFiles { get; set; }

	/// <summary>
	/// 中间文件夹
	/// </summary>
	[Required]
	public required string IntermediateDirectory { get; set; }

	/// <summary>
	/// 输出文件夹
	/// </summary>
	[Required]
	public required string OutputDirectory { get; set; }

	/// <summary>
	/// Builder路径
	/// </summary>
	[Required]
	public required string BuilderDirectory { get; set; }

	/// <summary>
	/// Support Platform : Windows, Xbox360, WindowsPhone
	/// </summary>
	public required string TargetPlatform { get; set; }

	/// <summary> Support Profile : HiDef, Reach <br> And I don't know what they mean </summary>
	public required string TargetProfile { get; set; }

	/// <summary>
	/// Configuration
	/// </summary>
	public required string BuildConfiguration { get; set; }

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

		var process = new Process()
		{
			StartInfo = new ProcessStartInfo()
			{
				FileName = filename,
				Arguments = string.Join(" ", args),
				RedirectStandardOutput = true,
				CreateNoWindow = true,
			},
		};

		process.Start();
		process.WaitForExit();
		var stdout = process.StandardOutput;
		while(!stdout.EndOfStream)
		{
			var s = stdout.ReadLine();
			var output = JsonConvert.DeserializeObject<BuildEventArgs>(s, new BuildEventArgsConverter());
			if (output is BuildMessageEventArgs msg)
			{
				BuildEngine.LogMessageEvent(msg);
			}
			else if (output is BuildWarningEventArgs warning)
			{
				BuildEngine.LogWarningEvent(warning);
			}
			else if (output is BuildErrorEventArgs error)
			{
				BuildEngine.LogErrorEvent(error);
				success = false;
			}
		}
		return success;
	}
}