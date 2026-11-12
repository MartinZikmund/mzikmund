using System.Net;
using Moq;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Account;
using MZikmund.ViewModels;
using Refit;

namespace MZikmund.App.Core.Tests.ViewModels;

public class SignInDialogViewModelTests
{
	private readonly Mock<IMZikmundApi> _api = new();
	private readonly Mock<IUserService> _userService = new();

	public SignInDialogViewModelTests()
	{
		TestLocalization.EnsureConfigured();
		_userService.SetupGet(u => u.IsLoggedIn).Returns(true);
	}

	private SignInDialogViewModel CreateViewModel() => new(_userService.Object, _api.Object, null);

	private static ApiResponse<T> Ok<T>(T content) => new(new HttpResponseMessage(HttpStatusCode.OK), content, new RefitSettings());

	private static async Task<ApiResponse<T>> Failure<T>(HttpStatusCode status, string? body = null)
	{
		var message = new HttpResponseMessage(status);
		if (body is not null)
		{
			message.Content = new StringContent(body);
		}

		var error = await ApiException.Create(new HttpRequestMessage(), HttpMethod.Put, message, new RefitSettings());
		return new(message, default, new RefitSettings(), error);
	}

	private void SetupProfile(bool chosen, string name = "Reader 4F2A") =>
		_api.Setup(a => a.GetMyCommentProfileAsync())
			.ReturnsAsync(Ok(new CommentProfile { DisplayName = name, HasChosenDisplayName = chosen, IsBanned = false }));

	private async Task<SignInDialogViewModel> SignedInAsync()
	{
		var viewModel = CreateViewModel();
		viewModel.Email = "a@b.cz";
		viewModel.Code = "123456";
		await viewModel.VerifyCodeCommand.ExecuteAsync(null);
		return viewModel;
	}

	[Fact]
	public async Task SignIn_ShowsTheNameStep_WhenNoNameWasChosen()
	{
		SetupProfile(chosen: false);

		var viewModel = await SignedInAsync();

		Assert.True(viewModel.IsNameStep);
		Assert.False(viewModel.IsCompleted);
		Assert.Equal("Reader 4F2A", viewModel.DisplayName);
	}

	[Fact]
	public async Task SignIn_SkipsTheNameStep_WhenANameWasChosen()
	{
		SetupProfile(chosen: true);

		var viewModel = await SignedInAsync();

		Assert.False(viewModel.IsNameStep);
		Assert.True(viewModel.IsCompleted);
	}

	[Fact]
	public async Task SignIn_Completes_WhenTheProfileFetchFails()
	{
		_api.Setup(a => a.GetMyCommentProfileAsync()).Returns(Failure<CommentProfile>(HttpStatusCode.InternalServerError));

		var viewModel = await SignedInAsync();

		Assert.False(viewModel.IsNameStep);
		Assert.True(viewModel.IsCompleted);
	}

	[Fact]
	public async Task Save_SendsTheTrimmedNameAndCompletes()
	{
		SetupProfile(chosen: false);
		_api.Setup(a => a.UpdateMyCommentProfileAsync(It.IsAny<UpdateCommentProfileRequest>()))
			.ReturnsAsync(Ok(new CommentProfile { DisplayName = "Martin", HasChosenDisplayName = true, IsBanned = false }));
		var viewModel = await SignedInAsync();
		viewModel.DisplayName = "  Martin ";

		await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

		_api.Verify(a => a.UpdateMyCommentProfileAsync(It.Is<UpdateCommentProfileRequest>(r => r.DisplayName == "Martin")));
		Assert.True(viewModel.IsCompleted);
		Assert.False(viewModel.HasError);
	}

	[Fact]
	public async Task Save_RejectsATooShortNameWithoutCallingTheApi()
	{
		SetupProfile(chosen: false);
		var viewModel = await SignedInAsync();
		viewModel.DisplayName = " x ";

		await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

		_api.Verify(a => a.UpdateMyCommentProfileAsync(It.IsAny<UpdateCommentProfileRequest>()), Times.Never);
		Assert.True(viewModel.HasError);
		Assert.False(viewModel.IsCompleted);
	}

	[Fact]
	public async Task Save_ShowsTheServerDetail_On400()
	{
		SetupProfile(chosen: false);
		_api.Setup(a => a.UpdateMyCommentProfileAsync(It.IsAny<UpdateCommentProfileRequest>()))
			.Returns(Failure<CommentProfile>(HttpStatusCode.BadRequest, """{"code":"display_name_invalid","detail":"No links in names."}"""));
		var viewModel = await SignedInAsync();

		await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

		Assert.Equal("No links in names.", viewModel.ErrorMessage);
		Assert.False(viewModel.IsCompleted);
		Assert.True(viewModel.IsNameStep);
	}

	[Fact]
	public async Task Save_FallsBackToAResource_On400WithoutDetail()
	{
		SetupProfile(chosen: false);
		_api.Setup(a => a.UpdateMyCommentProfileAsync(It.IsAny<UpdateCommentProfileRequest>()))
			.Returns(Failure<CommentProfile>(HttpStatusCode.BadRequest, """{"code":"display_name_reserved"}"""));
		var viewModel = await SignedInAsync();

		await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

		Assert.Equal("DisplayNameReserved", viewModel.ErrorMessage);
	}

	[Fact]
	public async Task Save_ShowsTheRateLimitMessage_On429()
	{
		SetupProfile(chosen: false);
		_api.Setup(a => a.UpdateMyCommentProfileAsync(It.IsAny<UpdateCommentProfileRequest>()))
			.Returns(Failure<CommentProfile>(HttpStatusCode.TooManyRequests));
		var viewModel = await SignedInAsync();

		await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

		Assert.Equal("DisplayNameRateLimited", viewModel.ErrorMessage);
		Assert.False(viewModel.IsCompleted);
	}

	[Fact]
	public async Task Skip_CompletesWithoutSaving()
	{
		SetupProfile(chosen: false);
		var viewModel = await SignedInAsync();

		viewModel.SkipDisplayNameCommand.Execute(null);

		_api.Verify(a => a.UpdateMyCommentProfileAsync(It.IsAny<UpdateCommentProfileRequest>()), Times.Never);
		Assert.True(viewModel.IsCompleted);
	}
}
