using MZikmund.LegacyMigration.Json;
using MZikmund.Web.Data.Entities;
using System.Collections.Generic;
using System;
using System.Linq;

namespace MZikmund.LegacyMigration.Processors;

internal class TagProcessor
{
	private readonly IList<Term> _terms;
	private readonly IList<TermRelationship> _termRelationships;
	private readonly IList<TermTaxonomy> _termTaxonomies;

	public TagProcessor(
		IList<Term> terms,
		IList<TermRelationship> termRelationships,
		IList<TermTaxonomy> termTaxonomies)
	{
		_terms = terms;
		_termRelationships = termRelationships;
		_termTaxonomies = termTaxonomies;
	}

	public IDictionary<long, TagEntity> Process()
	{
		var tags = new Dictionary<long, TagEntity>();
		foreach (var termTaxonomy in _termTaxonomies)
		{
			if (termTaxonomy.Taxonomy != "post_tag")
			{
				continue;
			}

			var term = _terms.First(t => t.TermId == termTaxonomy.TermId);
			var tag = new TagEntity
			{
				Id = Guid.NewGuid(),
				DisplayName = term.Name,
				RouteName = term.Slug,
			};
			tags.Add(term.TermId, tag);
		}

		return tags;
	}
}
