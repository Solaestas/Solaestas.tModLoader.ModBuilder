using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Task = Microsoft.Build.Utilities.Task;

namespace Solaestas.tModLoader.ModBuilder;

public record class PathField(string Filename, string Identity)
{
	public string Field { get; set; } = Filename;
	private bool _conflicted = false;
	public string[] Directories { get; private set; }

	public void Conflict()
	{
		if (_conflicted)
		{
			return;
		}

		_conflicted = true;

		Directories = new string[Identity.Count(c => c == Path.DirectorySeparatorChar)];
		int i = -1;
		foreach (var str in Identity.Split(Path.DirectorySeparatorChar))
		{
			if (++i >= Directories.Length)
			{
				return;
			}

			Directories[i] = str;
		}
	}

	public void FullQualified(StringBuilder sb)
	{
		sb.Clear();
		for (int i = 0; i < Directories.Length; i++)
		{
			sb.Append(Directories[i]).Append('_');
		}
		sb.Append(Filename);
		Field = sb.ToString();
	}
}

public class GeneratePath : Task
{
	[Required]
	public ITaskItem[] AssetFiles { get; set; }

	[Required]
	public string OutputDirectory { get; set; }

	[Required]
	public string Namespace { get; set; }

	public string AssetPrefix { get; set; } = string.Empty;

	public string ClassName { get; set; } = "ModAsset";

	public const string CacheFile = "PathGenerateCache";

	public override unsafe bool Execute()
	{
		string cachePath = Path.Combine(OutputDirectory, CacheFile);
		bool needRebuild = false;

		// Load Cache
		if (File.Exists(cachePath))
		{
			using var cacheStream = File.OpenText(cachePath);
			foreach (var file in AssetFiles)
			{
				if (cacheStream.EndOfStream)
				{
					needRebuild = true;
					break;
				}
				var line = cacheStream.ReadLine();
				if (line != file.ItemSpec)
				{
					needRebuild = true;
					break;
				}
			}
		}

		// Save Cache
		using var writer = File.CreateText(cachePath);
		foreach(var file in AssetFiles)
		{
			writer.WriteLine(file.ItemSpec);
		}

		if (!needRebuild)
		{
			return true;
		}

		var sb = new StringBuilder();
		int index = Namespace.IndexOf('.');
		string modName = index == -1 ? Namespace : Namespace[..index];
		var fields = new List<PathField>();
		foreach (var file in AssetFiles)
		{
			var filename = file.GetMetadata("Filename");
			var identity = file.GetMetadata("Identity");
			if (int.TryParse(filename, out var num))
			{
				index = identity.LastIndexOf(Path.DirectorySeparatorChar);
				filename = sb.Clear().Append(identity, index + 1, identity.Length - index - 1).Append('_').Append(filename).ToString();
				identity = index == -1 ? string.Empty : identity[..index];
			}
			fields.Add(new PathField(filename, identity));
		}

		var usedField = new HashSet<string>();
		foreach (var conflict in fields.GroupBy(f => f.Field))
		{
			if (conflict.Count() <= 1)
			{
				continue;
			}

			using var it = conflict.GetEnumerator();
			it.MoveNext();

			var previous = it.Current;
			bool flag = false;
			previous.Conflict();
			if (previous.Directories.Length <= 1)
			{
				previous.FullQualified(sb);
				usedField.Add(previous.Field);
			}

			while (it.MoveNext())
			{
				var current = it.Current;
				current.Conflict();

				if (current.Directories.Length <= 1)
				{
					current.FullQualified(sb);
					usedField.Add(current.Field);
					previous = current;
					continue;
				}

				string m = previous.Directories[^1], n = current.Directories[^1];
				if (m != n)
				{
					if (!flag)
					{
						flag = true;
						previous.Field = sb.Clear().Append(m).Append('_').Append(previous.Filename).ToString();
						usedField.Add(previous.Field);
					}
					current.Field = sb.Clear().Append(n).Append('_').Append(current.Filename).ToString();
					if (usedField.Add(current.Field))
					{
						previous = current;
						continue;
					}
				}

				if (previous.Directories.Length > 1)
				{
					m = previous.Directories[^2];
					n = current.Directories[^2];
					if (m != n)
					{
						if (!flag)
						{
							flag = true;
							previous.Field = sb.Clear().Append(m).Append('_').Append(previous.Filename).ToString();
							usedField.Add(previous.Field);
						}
						current.Field = sb.Clear().Append(n).Append('_').Append(current.Filename).ToString();
						if (usedField.Add(current.Field))
						{
							previous = current;
							continue;
						}
					}
				}

				if (usedField.Contains(current.Field))
				{
					current.FullQualified(sb);
				}

				previous = current;
			}
			usedField.Clear();
		}

		sb.Clear();
		foreach (var field in fields)
		{
			var identity = field.Identity;
			string fieldName = Validate(field.Field);
			if (GetFileType(identity) is string type)
			{
				if (string.IsNullOrEmpty(AssetPrefix))
				{
					sb.Append("\tpublic const string ").Append(fieldName).Append("Path = @\"");
				}
				else
				{
					sb.Append("\tpublic const string ").Append(fieldName).Append("Path = @\"").Append(AssetPrefix).Append('/');
				}
				if (field.Directories != null)
				{
					for (int i = 0; i < field.Directories.Length; i++)
					{
						sb.Append(field.Directories[i]).Append('/');
					}
					sb.Append(field.Filename);
				}
				else
				{
					index = field.Identity.LastIndexOf('.');
					sb.Append(field.Identity.Replace(Path.DirectorySeparatorChar, '/'), 0, index);
				}
				sb.Append("\";").AppendLine();

				sb.Append("\tpublic static Asset<").Append(type).Append("> ").Append(fieldName)
					.Append(" => _repo.Request<").Append(type).Append(">(").Append(fieldName)
					.Append("Path, AssetRequestMode.ImmediateLoad);").AppendLine();
			}
			else
			{
				sb.Append("\tpublic const string ").Append(fieldName).Append("Path = @\"").Append(identity).Append("\";").AppendLine();
			}
		}

		File.WriteAllText(
			Path.Combine(OutputDirectory, "ModAsset.g.cs"),
			$$"""
			using Microsoft.Xna.Framework.Graphics;
			using ReLogic.Content;
			using Terraria.ModLoader;

			namespace {{Namespace}};

			public static class {{ClassName}}
			{
				private static AssetRepository _repo;
				static ModAsset()
				{
					_repo = ModLoader.GetMod("{{modName}}").Assets;
				}
			{{sb}}
			}
			""");
		return true;
	}

	public string Validate(string field)
	{
		if (char.IsNumber(field[0]))
		{
			field = '_' + field;
		}

		return Regex.Replace(field, @"[ \.\-()]", "_", RegexOptions.Compiled);
	}

	public string GetFileType(string file)
	{
		return Path.GetExtension(file) switch
		{
			".png" => "Texture2D",
			".fx" => "Effect",
			_ => null
		};
	}
}