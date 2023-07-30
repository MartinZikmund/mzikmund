﻿// Based on https://github.com/EdiWang/Moonglade

namespace MZikmund.Web.Data.Infrastructure;

public class SpecificationEvaluator<T> where T : class
{
	public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
	{
		var query = inputQuery;

		// modify the IQueryable using the specification's criteria expression
		if (specification.Criteria is not null)
		{
			query = query.Where(specification.Criteria);
		}

		//// Includes all expression-based includes
		//query = specification.Includes.Aggregate(query,
		//    (current, include) => current.Include(include));

		//// Include any string-based include statements
		//query = specification.IncludeStrings.Aggregate(query,
		//    (current, include) => current.Include(include));

		if (specification.Include is not null)
		{
			query = specification.Include(query);
		}

		// Apply ordering if expressions are set
		if (specification.OrderBy is not null)
		{
			query = query.OrderBy(specification.OrderBy);
		}
		else if (specification.OrderByDescending is not null)
		{
			query = query.OrderByDescending(specification.OrderByDescending);
		}

		// Apply paging if enabled
		if (specification.IsPagingEnabled)
		{
			query = query.Skip(specification.Skip)
				.Take(specification.Take);
		}
		return query;
	}
}
