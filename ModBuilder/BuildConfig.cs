namespace Solaestas.tModLoader.ModBuilder;

public record class BuildConfig(
	string ModDirectory,
	TmlVersoin TmlVersion,
	string BuildIdentifier);