using AutoMapper;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings;

public class CommentMap : Profile
{
	public CommentMap()
	{
		CreateMap<CommentEntity, CommentDto>()
			.ForMember(dto => dto.AuthorDisplayName, opt => opt.MapFrom(entity => entity.Author.DisplayName))
			.ForMember(dto => dto.AuthorAvatarUrl, opt => opt.MapFrom(entity => entity.Author.AvatarUrl))
			.ForMember(dto => dto.AuthorIsAdmin, opt => opt.MapFrom(entity => entity.Author.IsAdmin))
			// Per-caller and bounded members are hand-projected by the query handlers.
			.ForMember(dto => dto.HasUpvoted, opt => opt.Ignore())
			.ForMember(dto => dto.IsOwn, opt => opt.Ignore())
			.ForMember(dto => dto.Replies, opt => opt.Ignore());
	}
}
