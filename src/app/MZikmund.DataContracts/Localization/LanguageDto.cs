using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZikmund.DataContracts.Localization;

public class LanguageDto
{
	public int Id { get; set; }

	public bool IsDefault { get; set; }

	public string CultureTag { get; set; } = "";

	public string Name { get; set; } = "";
}
