using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using static Solaestas.tModLoader.ModBuilder.Generators.Descriptors;

namespace Solaestas.tModLoader.ModBuilder.Generators;

public static class Extensions
{
	public static bool TryGetProperty(
		this ref readonly GeneratorExecutionContext context,
		string key,
		[NotNullWhen(true)] out string? value)
	{
		var option = context.AnalyzerConfigOptions.GlobalOptions;
		if (!option.TryGetValue($"build_property.{key}", out value) ||
			string.IsNullOrEmpty(value))
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MB0003,
				Location.None,
				[key]));
			return false;
		}
		return true;
	}

	public static string GetProperty(
		this ref readonly GeneratorExecutionContext context,
		string key,
		string defaultValue)
	{
		var option = context.AnalyzerConfigOptions.GlobalOptions;
		return option.TryGetValue($"build_property.{key}", out var value) ?
			string.IsNullOrEmpty(value) ? defaultValue : value : defaultValue;
	}

	public static bool TryGetMetadata(
		this ref readonly GeneratorExecutionContext context,
		AdditionalText file,
		string key,
		[NotNullWhen(true)] out string? value)
	{
		var option = context.AnalyzerConfigOptions.GetOptions(file);
		if (!option.TryGetValue($"build_metadata.AdditionalFiles.{key}", out value) ||
			string.IsNullOrEmpty(value))
		{
			context.ReportDiagnostic(Diagnostic.Create(
				MB0004,
				Location.None,
				[file.Path, key]));
			return false;
		}

		return true;
	}

	public static string GetMetadata(
			this ref readonly GeneratorExecutionContext context,
			AdditionalText file,
			string key,
			string defaultValue)
	{
		var option = context.AnalyzerConfigOptions.GetOptions(file);
		return option.TryGetValue($"build_metadata.AdditionalFiles.{key}", out var value) ?
			string.IsNullOrEmpty(value) ? defaultValue : value : defaultValue;
	}
}