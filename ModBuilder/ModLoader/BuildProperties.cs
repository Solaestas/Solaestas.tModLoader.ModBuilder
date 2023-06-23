using System.Text.RegularExpressions;

namespace Solaestas.tModLoader.ModBuilder.ModLoader;

public class BuildProperties
{
	public string Author = string.Empty;

	// This .tmod was built against a beta release, preventing publishing.
	public bool Beta = false;

	public string[] BuildIgnores = Array.Empty<string>();

	public Version BuildVersion = new();

	public string Description = string.Empty;

	public string DisplayName = string.Empty;

	public string[] DllReferences = Array.Empty<string>();

	public string EacPath = string.Empty;

	public bool HideCode = false;

	public bool HideResources = false;

	public string Homepage = string.Empty;

	public bool IncludeSource = false;

	public ModReference[] ModReferences = Array.Empty<ModReference>();

	public bool NoCompile = false;

	public bool PlayableOnPreview = true;

	public ModSide Side;

	// this mod will load after any mods in this list sortAfter includes (mod|weak)References that
	// are not in sortBefore
	public string[] SortAfter = Array.Empty<string>();

	// this mod will load before any mods in this list
	public string[] SortBefore = Array.Empty<string>();

	public Version Version = new(1, 0);

	public ModReference[] WeakReferences = Array.Empty<ModReference>();

	public static BuildProperties ReadBuildFile(string modDir, BuildInfo info)
	{
		string propertiesFile = Path.Combine(modDir, "build.txt");
		string descriptionfile = Path.Combine(modDir, "description.txt");
		var properties = new BuildProperties();
		if (!File.Exists(propertiesFile))
		{
			return properties;
		}

		properties.BuildVersion = info.tMLVersion;

		if (File.Exists(descriptionfile))
		{
			properties.Description = File.ReadAllText(descriptionfile);
		}

		foreach (string line in File.ReadAllLines(propertiesFile))
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			int split = line.IndexOf('=');
			string property = line[..split].Trim();
			string value = line[(split + 1)..].Trim();
			if (value.Length == 0)
			{
				continue;
			}

			switch (property)
			{
				case "dllReferences":
					properties.DllReferences = ReadList(value).ToArray();
					break;

				case "modReferences":
					properties.ModReferences = ReadList(value).Select(ModReference.Parse).ToArray();
					break;

				case "weakReferences":
					properties.WeakReferences = ReadList(value).Select(ModReference.Parse).ToArray();
					break;

				case "sortBefore":
					properties.SortBefore = ReadList(value).ToArray();
					break;

				case "sortAfter":
					properties.SortAfter = ReadList(value).ToArray();
					break;

				case "author":
					properties.Author = value;
					break;

				case "version":
					properties.Version = new Version(value);
					break;

				case "displayName":
					properties.DisplayName = value;
					break;

				case "homepage":
					properties.Homepage = value;
					break;

				case "noCompile":
					properties.NoCompile = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
					break;

				case "playableOnPreview":
					properties.PlayableOnPreview = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
					break;

				case "hideCode":
					properties.HideCode = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
					break;

				case "hideResources":
					properties.HideResources = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
					break;

				case "includeSource":
					properties.IncludeSource = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
					break;

				case "buildIgnore":
					properties.BuildIgnores = value.Split(',').Select(s => s.Trim().Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)).Where(s => s.Length > 0).ToArray();
					break;

				case "side":
					if (!Enum.TryParse(value, true, out properties.Side))
					{
						throw new Exception("side is not one of (Both, Client, Server, NoSync): " + value);
					}

					break;
			}
		}

		var refs = properties.RefNames(true).ToList();
		if (refs.Count != refs.Distinct().Count())
		{
			throw new Exception("Duplicate mod/weak reference");
		}

		// add (mod|weak)References that are not in sortBefore to sortAfter
		properties.SortAfter = properties.RefNames(true).Where(dep => !properties.SortBefore.Contains(dep))
			.Concat(properties.SortAfter).Distinct().ToArray();

		return properties;
	}

	public bool IgnoreFile(string resource)
	{
		return BuildIgnores.Any(fileMask => FitsMask(resource, fileMask));
	}

	public IEnumerable<string> RefNames(bool includeWeak)
	{
		return Refs(includeWeak).Select(dep => dep.Mod);
	}

	public IEnumerable<ModReference> Refs(bool includeWeak)
	{
		return includeWeak ? ModReferences.Concat(WeakReferences) : ModReferences;
	}

	public byte[] ToBytes()
	{
		byte[] data;
		using (var memoryStream = new MemoryStream())
		{
			using (var writer = new BinaryWriter(memoryStream))
			{
				if (DllReferences.Length > 0)
				{
					writer.Write("dllReferences");
					WriteList(DllReferences, writer);
				}

				if (ModReferences.Length > 0)
				{
					writer.Write("modReferences");
					WriteList(ModReferences, writer);
				}

				if (WeakReferences.Length > 0)
				{
					writer.Write("weakReferences");
					WriteList(WeakReferences, writer);
				}

				if (SortAfter.Length > 0)
				{
					writer.Write("sortAfter");
					WriteList(SortAfter, writer);
				}

				if (SortBefore.Length > 0)
				{
					writer.Write("sortBefore");
					WriteList(SortBefore, writer);
				}

				if (Author.Length > 0)
				{
					writer.Write("author");
					writer.Write(Author);
				}

				writer.Write("version");
				writer.Write(Version.ToString());
				if (DisplayName.Length > 0)
				{
					writer.Write("displayName");
					writer.Write(DisplayName);
				}

				if (Homepage.Length > 0)
				{
					writer.Write("homepage");
					writer.Write(Homepage);
				}

				if (Description.Length > 0)
				{
					writer.Write("description");
					writer.Write(Description);
				}

				if (NoCompile)
				{
					writer.Write("noCompile");
				}

				if (!PlayableOnPreview)
				{
					writer.Write("!playableOnPreview");
				}

				if (!HideCode)
				{
					writer.Write("!hideCode");
				}

				if (!HideResources)
				{
					writer.Write("!hideResources");
				}

				if (IncludeSource)
				{
					writer.Write("includeSource");
				}

				if (EacPath.Length > 0)
				{
					writer.Write("eacPath");
					writer.Write(EacPath);
				}

				if (Side != ModSide.Both)
				{
					writer.Write("side");
					writer.Write((byte)Side);
				}

				writer.Write("buildVersion");
				writer.Write(BuildVersion.ToString());

				writer.Write(string.Empty);
			}

			data = memoryStream.ToArray();
		}

		return data;
	}

	public struct ModReference
	{
		public string Mod;

		public Version Target;

		public ModReference(string mod, Version target)
		{
			Mod = mod;
			Target = target;
		}

		public static ModReference Parse(string spec)
		{
			var split = spec.Split('@');
			if (split.Length == 1)
			{
				return new ModReference(split[0], null);
			}

			if (split.Length > 2)
			{
				throw new Exception("Invalid mod reference: " + spec);
			}

			try
			{
				return new ModReference(split[0], new Version(split[1]));
			}
			catch
			{
				throw new Exception("Invalid mod reference: " + spec);
			}
		}

		public override string ToString()
		{
			return Target == null ? Mod : Mod + '@' + Target;
		}
	}

	private static IEnumerable<string> ReadList(string value)
	{
		return value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0);
	}

	private static void WriteList<T>(IEnumerable<T> list, BinaryWriter writer)
	{
		foreach (var item in list)
		{
			writer.Write(item.ToString());
		}

		writer.Write(string.Empty);
	}

	private bool FitsMask(string fileName, string fileMask)
	{
		string pattern =
			'^' +
			Regex.Escape(fileMask.Replace(".", "__DOT__")
							 .Replace("*", "__STAR__")
							 .Replace("?", "__QM__"))
				 .Replace("__DOT__", "[.]")
				 .Replace("__STAR__", ".*")
				 .Replace("__QM__", ".")
			+ '$';
		return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(fileName);
	}
}