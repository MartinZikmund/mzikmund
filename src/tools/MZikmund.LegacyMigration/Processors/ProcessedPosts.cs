using System.Collections.Generic;
using MZikmund.Web.Data.Entities;

namespace MZikmund.LegacyMigration.Processors;

internal record struct ProcessedPosts(
	IDictionary<long, PostEntity> Posts,
	IList<PostCategoryEntity> PostCategories,
	IList<PostTagEntity> PostTags,
	IList<PostRevisionEntity> PostRevisions)
{
}
