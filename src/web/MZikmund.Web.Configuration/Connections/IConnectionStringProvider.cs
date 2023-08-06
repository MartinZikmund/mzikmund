using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZikmund.Web.Configuration.Connections;

public interface IConnectionStringProvider
{
	string? Database { get; }

	string? SendGrid { get; }

	string? AzureBlobStorage { get; }
}
