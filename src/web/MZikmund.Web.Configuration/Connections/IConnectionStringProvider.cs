using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZikmund.Web.Configuration.Connections;

public interface IConnectionStringProvider
{
	string? DatabaseConnection { get; }

	string? SendGrid { get; }

	string? AzureBlobStorage { get; }
}
