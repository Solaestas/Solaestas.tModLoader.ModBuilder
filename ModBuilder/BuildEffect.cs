using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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
	public string InputFiles { get; set; } = default!;

	/// <summary>
	/// 中间文件夹
	/// </summary>
	[Required]
	public string IntermediateDirectory { get; set; } = default!;

	/// <summary>
	/// 输出文件夹
	/// </summary>
	[Required]
	public string OutputDirectory { get; set; } = default!;

	/// <summary>
	/// Builder路径
	/// </summary>
	[Required]
	public string BuilderDirectory { get; set; } = default!;

	/// <summary>
	/// Support Platform : Windows, Xbox360, WindowsPhone
	/// </summary>
	[Required]
	public string TargetPlatform { get; set; } = default!;

	/// <summary>
	/// Support Profile : HiDef, Reach <br /> And I don't know what they mean
	/// </summary>
	[Required]
	public string TargetProfile { get; set; } = default!;

	/// <summary>
	/// Configuration
	/// </summary>
	[Required]
	public string BuildConfiguration { get; set; } = default!;

	[Output]
	public TaskItem[] EffectOutputs { get; set; } = default!;

	public override bool Execute()
	{
		Log.LogMessage(MessageImportance.High, LogText.BuildEffect);
		var filename = $"{BuilderDirectory}ShaderBuilder.exe";
		var args = new List<string>()
		{
			string.Join(";", InputFiles),
			IntermediateDirectory,
			OutputDirectory,
			TargetPlatform,
			TargetProfile,
			BuildConfiguration,
		};

		return ExecuteBuilder().Result;

		async Task<bool> ExecuteBuilder()
		{
			var info = new ProcessStartInfo()
			{
				FileName = filename,
				Arguments = string.Join(" ", args),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			using var proc = Process.Start(info);

			var outputCloseEvent = new TaskCompletionSource<bool>();
			bool hasError = false;
			proc.OutputDataReceived += (s, e) =>
			{
				if (e.Data == null)
				{
					outputCloseEvent.SetResult(true);
					return;
				}
				var args = JsonConvert.DeserializeObject<BuildEventArgs>(e.Data, new BuildEventArgsConverter());
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
					hasError = true;
				}
			};
			var errorCloseEvent = new TaskCompletionSource<bool>();
			var errorMessages = new StringBuilder();
			proc.ErrorDataReceived += (s, e) =>
			{
				if (e.Data == null)
				{
					errorCloseEvent.SetResult(true);
					return;
				}
				errorMessages.AppendLine(e.Data);
			};

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			var waitForExit = System.Threading.Tasks.Task.Run(proc.WaitForExit);
			var timeout = System.Threading.Tasks.Task.Delay(TimeSpan.FromMinutes(1));
			var procTask = System.Threading.Tasks.Task.WhenAll(
				waitForExit,
				outputCloseEvent.Task,
				errorCloseEvent.Task);

			if(await System.Threading.Tasks.Task.WhenAny(procTask, timeout) == timeout)
			{
				proc.Kill();
				Log.LogError(LogText.BuildEffectTimeout);
				return false;
			}

			if(errorMessages.Length > 0)
			{
				Log.LogError(errorMessages.ToString());
				return false;
			}

			hasError |= proc.ExitCode != 0;

			return !hasError;
		}
	}
}