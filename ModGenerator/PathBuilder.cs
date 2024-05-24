using System.Text;

namespace Solaestas.tModLoader.ModBuilder.Generators;

public class PathBuilder
{
	private char[] nameBuffer = new char[64];

	private int nameLength = 0;

	private char[] valueBuffer = new char[128];

	private int valueLength = 0;

	public void Append(StringBuilder builder, in PathMember member, string prefix)
	{
		Span<char> buffer;
		ReadOnlySpan<char> name;
		switch (member.Style)
		{
			case PathStyle.Default:
				name = member.Name;
				nameLength = name.Length;
				buffer = nameBuffer.AsSpan(0, nameLength);
				name.CopyTo(buffer);
				break;
			case PathStyle.FullName:
				name = member.FullName;
				nameLength = name.Length;
				buffer = nameBuffer.AsSpan(0, nameLength);
				name.CopyTo(buffer);
				break;
			case PathStyle.Reduce:
				var identity = member.Slice(member.Depth);
				name = member.Slice(1);
				nameLength = identity.Length + name.Length + 1;
				buffer = nameBuffer.AsSpan(0, nameLength);
				identity.CopyTo(buffer);
				buffer[identity.Length] = '_';
				name.CopyTo(buffer[(identity.Length + 1)..]);
				break;
			default:
				throw new ArgumentException("Unknown PathStyle");
		}
		Replace(buffer, ['\\', '/', '.', ' '], '_');

		var value = member.Value;
		prefix.CopyTo(0, valueBuffer, 0, prefix.Length);
		buffer = valueBuffer.AsSpan(prefix.Length, value.Length);
		value.CopyTo(buffer);
		Replace(buffer, '\\', '/');
		valueLength = prefix.Length + value.Length;

		AppendInternal(builder, member);
	}

	private static void Replace(Span<char> buffer, Span<char> oldChars, char newChar)
	{
		for (var i = 0; i < buffer.Length; i++)
		{
			for (var j = 0; j < oldChars.Length; j++)
			{
				if (buffer[i] == oldChars[j])
				{
					buffer[i] = newChar;
				}
			}
		}
	}

	private static void Replace(Span<char> buffer, char oldChar, char newChar)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i] == oldChar)
			{
				buffer[i] = newChar;
			}
		}
	}

	private void AppendInternal(StringBuilder builder, in PathMember member)
	{
		builder.Append("\tpublic const string ")
			.Append(nameBuffer, 0, nameLength)
			.Append("_Path = \"")
			.Append(valueBuffer, 0, valueLength)
			.AppendLine("\";");

		var assetType = member.Asset;
		if (assetType != AssetType.None)
		{
			builder.Append("\t/// <summary> ")
				.Append(valueBuffer, 0, valueLength)
				.AppendLine(" </summary>");

			string decl = $"\tpublic static Asset<{assetType}> ";
			string expr = $" => _repo.Request<{assetType}>(";
			builder.Append(decl)
				.Append(nameBuffer, 0, nameLength)
				.Append(expr)
				.Append(nameBuffer, 0, nameLength)
				.AppendLine("_Path, AssetRequestMode.ImmediateLoad);");

			builder.Append("\t/// <summary> ")
				.Append(valueBuffer, 0, valueLength)
				.AppendLine(" </summary>");

			builder.Append(decl)
				.Append(nameBuffer, 0, nameLength)
				.Append("_Async")
				.Append(expr)
				.Append(nameBuffer, 0, nameLength)
				.AppendLine("_Path, AssetRequestMode.AsyncLoad);");
		}
	}
}