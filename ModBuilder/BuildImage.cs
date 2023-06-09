using System.IO.Compression;
using Microsoft.Build.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Solaestas.tModLoader.ModBuilder;

public class BuildImage : Microsoft.Build.Utilities.Task
{
	/// <summary>
	/// 输入文件
	/// </summary>
	[Required]
	public required ITaskItem[] InputFiles { get; set; }

	/// <summary>
	/// 输出文件夹
	/// </summary>
	[Required]
	public required string OutputDirectory { get; set; }

	public const uint MIN_COMPRESS_SIZE = 1 << 10; // 1KB

	public const float COMPRESSION_TRADEOFF = 0.9f;

	public override bool Execute()
	{
		Log.LogMessage(MessageImportance.High, "Building Images...");
		foreach (var file in InputFiles)
		{
			var relativeDir = file.GetMetadata("RelativeDir");
			var filename = file.GetMetadata("Filename");
			var identity = file.GetMetadata("Identity");

			string dir = Path.Combine(OutputDirectory, relativeDir);
			string filePath = Path.Combine(dir, $"{filename}.rawimg");
			Log.LogMessage(MessageImportance.Low, "Building {0} -> {1}", identity, filePath);

			Directory.CreateDirectory(dir);
			using var output = File.Create(filePath);
			using var input = File.OpenRead(file.ItemSpec);
			using var ms = new MemoryStream();
			ImageIO.ToRaw(input, ms);
			var data = ms.ToArray();

			// 原文件长度
			int size = data.Length;

			// 提前压缩
			if (size > MIN_COMPRESS_SIZE)
			{
				ms.SetLength(0);
				using (var ds = new DeflateStream(ms, CompressionMode.Compress))
				{
					ds.Write(data, 0, size);
				}

				var compressed = ms.ToArray();
				if (compressed.Length < size * COMPRESSION_TRADEOFF)
				{
					data = compressed;
				}
			}
			var sizeBytes = BitConverter.GetBytes(size);
			output.Write(sizeBytes, 0, sizeBytes.Length);
			output.Write(data, 0, data.Length);
		}
		return true;
	}
}

/// <summary>
/// Copy From tModLoader
/// </summary>
public static class ImageIO
{
	public static void ToRaw(Stream source, Stream destination)
	{
		var image = Image.Load<Rgba32>(source);
		var writer = new BinaryWriter(destination);

		// 不知道为啥要写一个1进去
		writer.Write(1);
		int width = image.Width;
		int height = image.Height;
		writer.Write(width);
		writer.Write(height);

		image.ProcessPixelRows(accessor =>
		{
			for (int i = 0; i < accessor.Height; i++)
			{
				foreach (var color in accessor.GetRowSpan(i))
				{
					if (color.A == 0)
					{
						// 直接写入四字节的int 0
						writer.Write(0);
						continue;
					}

					writer.Write(color.R);
					writer.Write(color.G);
					writer.Write(color.B);
					writer.Write(color.A);
				}
			}
		});
	}
}