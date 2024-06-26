namespace Solaestas.tModLoader.ModBuilder.Generators;

public unsafe struct PathMember
{
	private AssetType _assetType;

	private PathStyle _style;

	private int _depth;

	private int _extension;

	private string _path;

	private int _slashCount;

	// 应该不至于有这么长的路径吧
	private fixed sbyte _slashBuffer[16];

	public PathMember(string path)
	{
		_path = path;
		_depth = 1;
		_slashBuffer[0] = -1;
		_slashCount = 1;
		_style = PathStyle.Default;
		for (int i = 0; i < path.Length; i++)
		{
			if (path[i] is '\\' or '/')
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
				"ogg" or "wav" or "mp3" => AssetType.SoundEffect,
				_ => AssetType.None,
			};
		}
		else
		{
			_assetType = AssetType.None;
			_extension = _path.Length;
		}
	}

	public readonly AssetType Asset => _assetType;

	public readonly int Depth => _depth;

	/// <summary>
	/// 文件名加上对应深度的文件夹前缀，不包括后缀名
	/// </summary>
	public readonly ReadOnlySpan<char> Name
	{
		get
		{
			int start = _slashBuffer[_slashCount - _depth] + 1;
			int length = _extension - start;
			return _path.AsSpan(start, length);
		}
	}

	/// <summary>
	/// 文件名加上对应深度的文件夹前缀，包括后缀名
	/// </summary>
	public readonly ReadOnlySpan<char> FullName
	{
		get
		{
			int start = _slashBuffer[_slashCount - _depth] + 1;
			int length = _path.Length - start;
			return _path.AsSpan(start, length);
		}
	}

	public PathStyle Style { readonly get => _style; set => _style = value; }

	public readonly string Path => _path;

	/// <summary>
	/// 文件路径，如果是Asset则不包含后缀名
	/// </summary>
	public readonly ReadOnlySpan<char> Value
	{
		get
		{
			int length = _assetType == AssetType.None ? _path.Length : _extension;
			return _path.AsSpan(0, length);
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

	public static implicit operator PathMember(string path) => new PathMember(path);

	/// <summary>
	/// 提高深度
	/// </summary>
	/// <param name="member"></param>
	/// <returns></returns>
	public static PathMember Increase(PathMember member)
	{
		if (member._depth < member._slashCount)
		{
			member._depth++;
		}
		return member;
	}

	/// <summary>
	/// 返回对应深度的文件夹名称
	/// </summary>
	/// <param name="depth"></param>
	/// <returns></returns>
	public readonly ReadOnlySpan<char> Slice(int depth)
	{
		int start = _slashBuffer[_slashCount - depth] + 1;
		int end = depth == 1 ? _extension : _slashBuffer[_slashCount - depth + 1];
		int length = end - start;
		return _path.AsSpan(start, length);
	}

	public override readonly string ToString()
	{
		return $"Path: {_path}, Depth: {_depth}, Style: {_style}, Asset: {_assetType}";
	}
}