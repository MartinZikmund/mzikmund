using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZikmund.DataContracts;

public class PagedResponse<T>
{
	public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalCount)
	{
		Data = data;
		PageNumber = pageNumber;
		PageSize = pageSize;
		TotalCount = totalCount;
	}

	public IEnumerable<T> Data { get; }

	public int PageNumber { get; }

	public int PageSize { get; }

	public int TotalCount { get; }
}
