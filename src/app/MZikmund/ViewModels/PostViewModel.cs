using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZikmund.ViewModels;

public class PostViewModel : PageViewModel
{
	public PostViewModel()
	{

	}

	public string HtmlPreview { get; private set; }
}
