using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solaestas.tModLoader.ModBuilder;
public enum GameVersion
{
	[Description("稳定版本")]
	Stable,
	[Description("预览版本")]
	Preview,
	[Description("开发者版本")]
	Developer,
	[Description("1.4.3")]
	Legacy,
}
