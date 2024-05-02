using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solaestas.tModLoader.ModBuilder.Generators;

public unsafe struct PathMember
{
	private static char[] nameBuffer = new char[64];

	private AssetType _assetType;

	private int _depth;

	private int _extension;

	private string _path;

	// 应该不至于有这么长的路径吧
	private fixed sbyte _slashBuffer[8];

	private int _slashCount;

	public PathMember(string path)
	{
		_path = path;
		_depth = 1;
		_slashBuffer[0] = -1;
		_slashCount = 1;
		for (int i = 0; i < path.Length; i++)
		{
			if (path[i] == '\\')
			{
				_slashBuffer[_slashCount++] = (sbyte)i;
			}
		}
		_slashBuffer[_slashCount] = (sbyte)path.Length;

		_extension = _path.LastIndexOf('.');
		if (_extension != -1)
		{
			_assetType = _path.AsSpan(_extension + 1) switch
			{
				"png" => AssetType.Texture2D,
				"xnb" => AssetType.Effect,
				_ => AssetType.None,
			};
		}
		else
		{
			_assetType = AssetType.None;
			_extension = _path.Length;
		}
	}

	public readonly int Depth => _depth;

	public readonly ReadOnlySpan<char> Name
	{
		get
		{
			int start = _slashBuffer[_slashCount - _depth] + 1;
			int length = _extension - start;
			return _path.AsSpan(start, length);
		}
	}

	public readonly ReadOnlySpan<char> Value
	{
		get
		{
			int length = _assetType == AssetType.None ? _path.Length : _extension;
			return _path.AsSpan(0, length);
		}
	}

	public readonly string Path => _path;

	public static implicit operator PathMember(string path) => new PathMember(path);

	public static PathMember Increase(PathMember member)
	{
		if (member._depth < member._slashCount)
		{
			member._depth++;
		}
		return member;
	}

	public readonly void Append(StringBuilder builder, string prefix)
	{
		// public const string $
		builder.Append("\tpublic const string ");
		var span = Name;
		int start = builder.Length;
		int nameLength = span.Length;
		fixed (char* ptr = span)
		{
			// public const string Dir\Name$
			builder.Append(ptr, span.Length);
		}

		// public const string Dir_Name$
		builder.Replace('\\', '_', start, nameLength);
		builder.Replace('/', '_', start, nameLength);
		builder.CopyTo(start, nameBuffer, 0, nameLength);

		// public const string Dir_Name_Path = "$
		builder.Append("_Path = \"");

		// public const string Dir_Name = "{prefix}$
		builder.Append(prefix);

		start = builder.Length;
		span = Value;
		fixed (char* ptr = span)
		{
			// public const string Dir_Name = "{prefix}Root\Dir\Name$
			builder.Append(ptr, span.Length);
		}

		// public const string Dir_Name = "{prefix}Root/Dir/Name$
		builder.Replace('\\', '/', start, span.Length);

		// public const string Dir_Name = "{prefix}Root/Dir/Name";$
		builder.AppendLine("\";");

		if (_assetType != AssetType.None)
		{
			fixed (char* ptr = span)
			{
				builder.Append("\t/// <summary> ")
				.Append(prefix)
				.Append(ptr, span.Length)
				.AppendLine(" </summary>");
			}

			// public static Asset<Texture2D> $
			builder.Append($"\tpublic static Asset<{_assetType}> ");

			// public static Asset<Texture2D> Dir_Name$
			builder.Append(nameBuffer, 0, nameLength);

			// public static Asset<Texture2D> Dir_Name = _repo.Request<Texture2D>($
			builder.Append($" = _repo.Request<{_assetType}>(");

			// public static Asset<Texture2D> Dir_Name = _repo.Request<Texture2D>(Dir_Name$
			builder.Append(nameBuffer, 0, nameLength);

			// public static Asset<Texture2D> Dir_Name = _repo.Request<Texture2D>(Dir_Name_Path);$
			builder.AppendLine("_Path);");
		}
	}

	public static bool HasOverlap(List<PathMember> list)
	{
		var depth = list[0].Depth;
		for (int i = 1; i < list.Count; i++)
		{
			var member = list[i];
			if (member.Depth != depth)
			{
				return false;
			}
		}
		return true;
	}

	public readonly void AppendReduceOverlap(StringBuilder builder, string prefix)
	{
		// public const string $
		builder.Append("\tpublic const string ");
		var identity = Slice(_depth);
		var shared = Slice(1);
		int start = builder.Length;
		int nameLength = identity.Length + shared.Length + 1;
		fixed (char* ptr = identity, p1 = shared)
		{
			// public const string Dir_Name$
			builder.Append(ptr, identity.Length)
			.Append('_')
			.Append(p1, shared.Length);
		}

		builder.CopyTo(start, nameBuffer, 0, nameLength);

		// public const string Dir_Name = "{prefix}$
		builder.Append("_Path = \"").Append(prefix);

		start = builder.Length;
		var span = Value;
		fixed (char* ptr = span)
		{
			// public const string Dir_Name = "{prefix}Root\Dir\Name$
			builder.Append(ptr, span.Length);
		}

		// public const string Dir_Name = "{prefix}Root/Dir/Name$
		builder.Replace('\\', '/', start, span.Length);

		// public const string Dir_Name = "{prefix}Root/Dir/Name";$
		builder.AppendLine("\";");

		if (_assetType != AssetType.None)
		{
			fixed (char* ptr = span)
			{
				builder.Append("\t/// <summary> ")
				.Append(ptr, span.Length)
				.AppendLine(" </summary>");
			}

			// public static Asset<Texture2D> $
			builder.Append($"\tpublic static Asset<{_assetType}> ");

			// public static Asset<Texture2D> Dir_Name$
			builder.Append(nameBuffer, 0, nameLength);

			// public static Asset<Texture2D> Dir_Name = _repo.Request<Texture2D>($
			builder.Append($" = _repo.Request<{_assetType}>(");

			// public static Asset<Texture2D> Dir_Name = _repo.Request<Texture2D>(Dir_Name$
			builder.Append(nameBuffer, 0, nameLength);

			// public static Asset<Texture2D> Dir_Name = _repo.Request<Texture2D>(Dir_Name_Path);$
			builder.AppendLine("_Path);");
		}
	}

	public readonly ReadOnlySpan<char> Slice(int depth)
	{
		int start = _slashBuffer[_slashCount - depth] + 1;
		int end = depth == 1 ? _extension : _slashBuffer[_slashCount - depth + 1];
		int length = end - start;
		return _path.AsSpan(start, length);
	}
}