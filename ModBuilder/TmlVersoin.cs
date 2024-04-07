using System.ComponentModel;

namespace Solaestas.tModLoader.ModBuilder;

public enum TmlVersoin : int
{
	[Description("稳定版本")]
	Stable = 0,
	[Description("预览版本")]
	Preview = 1,
	[Description("开发者版本")]
	Developer = 2,
	[Description("1.4.3")]
	Legacy = 3,
}