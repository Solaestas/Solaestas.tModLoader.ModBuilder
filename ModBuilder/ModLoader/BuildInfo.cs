using System;

namespace Solaestas.tModLoader.ModBuilder.ModLoader;

public class BuildInfo
{
	public enum BuildPurpose
	{
		Dev, // Personal Builds

		Preview, // Monthly preview builds from CI that modders develop against for compatibility

		Stable, // The 'stable' builds from CI that players are expected to play on.
	}

	public string BuildIdentifier;

	public Version tMLVersion;

	public Version StableVersion;

	public BuildPurpose Purpose;

	public string BranchName;

	public string CommitSHA;

	/// <summary>
	/// local time, for display purposes.
	/// </summary>
	public DateTime BuildDate;

	public bool IsStable => Purpose == BuildPurpose.Stable;

	public bool IsPreview => Purpose == BuildPurpose.Preview;

	public bool IsDev => Purpose == BuildPurpose.Dev;

	public string VersionedName;

	public string VersionTag;

	public string VersionedNameDevFriendly;

	public BuildInfo(BuildConfig config)
	{
		BuildIdentifier = config.BuildIdentifier;
		var parts = BuildIdentifier[(BuildIdentifier.IndexOf('+') + 1)..].Split('|');
		int i = 0;

		tMLVersion = new Version(parts[i++]);
		StableVersion = new Version(parts[i++]);
		BranchName = parts[i++];
		Enum.TryParse(parts[i++], true, out Purpose);
		CommitSHA = parts[i++];
		BuildDate = DateTime.FromBinary(long.Parse(parts[i++])).ToLocalTime();

		// Version name for players
		VersionedName = $"tModLoader v{tMLVersion}";

		if (!string.IsNullOrEmpty(BranchName) && BranchName != "unknown"
			&& BranchName != "1.4-stable" && BranchName != "1.4-preview" && BranchName != "1.4")
		{
			VersionedName += $" {BranchName}";
		}

		if (Purpose != BuildPurpose.Stable)
		{
			VersionedName += $" {Purpose}";
		}

		// Version Tag for ???
		VersionTag = VersionedName["tModLoader ".Length..].Replace(' ', '-').ToLower();

		// Version name for modders
		VersionedNameDevFriendly = VersionedName;

		if (CommitSHA != "unknown")
		{
			VersionedNameDevFriendly += $" {CommitSHA[..8]}";
		}

		VersionedNameDevFriendly += $", built {BuildDate:g}";
	}
}