#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
using System.Collections;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Solaestas.tModLoader.ModBuilder.ModLoader;

// warning class is not threadsafe
public class TmodFile(string path, string name, Version version) : IEnumerable<TmodFile.FileEntry>
{
	public class FileEntry(string name, int offset, int length, int compressedLength, byte[]? cachedBytes = null)
	{
		public string Name { get; } = name;

		// from the start of the file
		public int Offset { get; set; } = offset;

		public int Length { get; } = length;

		public int CompressedLength { get; } = compressedLength;

		// intended to be readonly, but unfortunately no ReadOnlySpan on .NET 4.5
		public byte[]? CachedBytes { get; set; } = cachedBytes;

		public bool IsCompressed => Length != CompressedLength;
	}

	public const uint MIN_COMPRESS_SIZE = 1 << 10; // 1KB

	public const uint MAX_CACHE_SIZE = 1 << 17; // 128KB

	public const float COMPRESSION_TRADEOFF = 0.9f;

	private static string Sanitize(string path)
	{
		return path.Replace('\\', '/');
	}

	public readonly string Path = path;

	private FileStream? fileStream;

	public IDictionary<string, FileEntry> files = new ConcurrentDictionary<string, FileEntry>();

	private FileEntry[] fileTable;

	public Version TModLoaderVersion { get; private set; }

	public string Name { get; private set; } = name;

	public Version Version { get; private set; } = version;

	public byte[] Hash { get; private set; }

	public byte[] Signature { get; private set; } = new byte[256];

	public void AddFile(string fileName, string filePath)
	{
		fileName = Sanitize(fileName);
		using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
		int size = (int)stream.Length;

		var data = new byte[size];
		stream.Read(data, 0, size);
		if (size > MIN_COMPRESS_SIZE && ShouldCompress(fileName))
		{
			using var ms = new MemoryStream(size);
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
		files[fileName] = new FileEntry(fileName, -1, size, data.Length, data);
	}

	private bool ShouldCompress(string fileName)
	{
		var ext = System.IO.Path.GetExtension(fileName);
		return ext is not ".png" and not ".mp3" and not ".ogg";
	}

	public void AddFile(string fileName, byte[] data)
	{
		fileName = Sanitize(fileName);
		int size = data.Length;
		files[fileName] = new FileEntry(fileName, -1, size, size, data);
	}

	public int Count => fileTable.Length;

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<FileEntry> GetEnumerator()
	{
		foreach (var entry in fileTable)
		{
			yield return entry;
		}
	}

	public void Save(BuildInfo info, FileStream assetPack)
	{
		if (fileStream != null)
		{
			throw new IOException($"File already open: {Path}");
		}

		// write the general TMOD header and data blob TMOD ascii identifier tModLoader version hash
		// signature data length signed data
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
		using (fileStream = File.Create(Path))
		using (var writer = new BinaryWriter(fileStream))
		{
			writer.Write(Encoding.ASCII.GetBytes("TMOD"));
			writer.Write((TModLoaderVersion = info.tMLVersion).ToString());

			int hashPos = (int)fileStream.Position;
			writer.Write(new byte[20 + 256 + 4]); // hash, sig, data length

			int dataPos = (int)fileStream.Position;
			writer.Write(Name);
			writer.Write(Version.ToString());

			using var reader = new BinaryReader(assetPack);
			int length = reader.ReadInt32();
			int tableSize = reader.ReadInt32();
			int dataSize = reader.ReadInt32();

			fileTable = [.. files.Values];
			writer.Write(length + fileTable.Length);
			assetPack.CopyTo(fileStream, tableSize);
			foreach (var f in fileTable)
			{
				writer.Write(f.Name);
				writer.Write(f.Length);
				writer.Write(f.CompressedLength);
			}

			assetPack.CopyTo(fileStream, dataSize);
			foreach (var f in fileTable)
			{
				writer.Write(f.CachedBytes);
			}

			// update hash
			fileStream.Position = dataPos;
			Hash = SHA1.Create().ComputeHash(fileStream);

			fileStream.Position = hashPos;
			writer.Write(Hash);

			// skip signature
			fileStream.Seek(256, SeekOrigin.Current);

			// write data length
			writer.Write((int)(fileStream.Length - dataPos));
		}

		fileStream = null;
	}

	public void WriteFileTable(FileStream file)
	{
		using var writer = new BinaryWriter(file);
		fileTable = [.. files.Values];
		writer.Write(fileTable.Length);

		// Table size and data size
		int sizePos = (int)file.Position;
		writer.Write(default(int));
		writer.Write(default(int));

		foreach (var f in fileTable)
		{
			writer.Write(f.Name);
			writer.Write(f.Length);
			writer.Write(f.CompressedLength);
		}

		int tableSize = (int)file.Position - sizePos - sizeof(int) * 2;

		// write compressed files and update offsets
		foreach (var f in fileTable)
		{
			writer.Write(f.CachedBytes);
		}

		int dataSize = (int)file.Position - tableSize;

		file.Position = sizePos;
		writer.Write(tableSize);
		writer.Write(dataSize);
	}
}
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。