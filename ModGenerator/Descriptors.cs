using Microsoft.CodeAnalysis;

namespace Solaestas.tModLoader.ModBuilder.Generators;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪", Justification = "<挂起>")]
public static class Descriptors
{
	private const string Category = nameof(ModBuilder);

	/// <summary>
	/// 文件名重复
	/// </summary>
	public static readonly DiagnosticDescriptor MB0001 = new(
		nameof(MB0001),
		new LocalizableResourceString(nameof(LogText.MB0001Title), LogText.ResourceManager, typeof(LogText)),
		new LocalizableResourceString(nameof(LogText.MB0001Message), LogText.ResourceManager, typeof(LogText)),
		Category,
		DiagnosticSeverity.Warning,
		true
	);

	/// <summary>
	/// 文件名有特殊字符
	/// </summary>
	public static readonly DiagnosticDescriptor MB0002 = new(
		nameof(MB0002),
		new LocalizableResourceString(nameof(LogText.MB0002Title), LogText.ResourceManager, typeof(LogText)),
		new LocalizableResourceString(nameof(LogText.MB0002Message), LogText.ResourceManager, typeof(LogText)),
		Category,
		DiagnosticSeverity.Warning,
		true
	);

	/// <summary>
	/// 缺少Property
	/// </summary>
	public static readonly DiagnosticDescriptor MB0003 = new(
		nameof(MB0003),
		new LocalizableResourceString(nameof(LogText.MB0003Title), LogText.ResourceManager, typeof(LogText)),
		new LocalizableResourceString(nameof(LogText.MB0003Message), LogText.ResourceManager, typeof(LogText)),
		Category,
		DiagnosticSeverity.Error,
		true
	);

	/// <summary>
	/// 缺少ItemMetadata
	/// </summary>
	public static readonly DiagnosticDescriptor MB0004 = new(
		nameof(MB0004),
		new LocalizableResourceString(nameof(LogText.MB0004Title), LogText.ResourceManager, typeof(LogText)),
		new LocalizableResourceString(nameof(LogText.MB0004Message), LogText.ResourceManager, typeof(LogText)),
		Category,
		DiagnosticSeverity.Error,
		true
	);
}