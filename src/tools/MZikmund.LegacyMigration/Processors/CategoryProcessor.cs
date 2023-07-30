using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MZikmund.LegacyMigration.Json;
using MZikmund.Web.Data.Entities;

namespace MZikmund.LegacyMigration.Processors;

internal sealed class CategoryProcessor
{
	private readonly IList<Term> _terms;
	private readonly IList<TermRelationship> _termRelationships;
	private readonly IList<TermTaxonomy> _termTaxonomies;

	public CategoryProcessor(
		IList<Term> terms,
		IList<TermRelationship> termRelationships,
		IList<TermTaxonomy> termTaxonomies)
	{
		_terms = terms;
		_termRelationships = termRelationships;
		_termTaxonomies = termTaxonomies;
	}

	public IDictionary<long, CategoryEntity> Process()
	{
		var categories = new Dictionary<long, CategoryEntity>();
		foreach (var termTaxonomy in _termTaxonomies)
		{
			if (termTaxonomy.Taxonomy != "category")
			{
				continue;
			}

			var term = _terms.First(t => t.TermId == termTaxonomy.TermId);
			var category = new CategoryEntity
			{
				Id = Guid.NewGuid(),
				DisplayName = term.Name,
				RouteName = term.Slug,
				//ParentId = termTaxonomy.Parent,
				Description = termTaxonomy.Description,
			};
			categories.Add(term.TermId, category);
		}

		return categories;
	}
}
