using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using MZikmund.LegacyMigration.Json;
using MZikmund.LegacyMigration.Processors;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MZikmund.LegacyMigration.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
	private const string PostMetaFileName = "wp_postmeta.json";
	private const string PostsFileName = "wp_posts.json";
	private const string EnrichedPostsFileName = "wp_posts_enriched.json";
	private const string TermRelationshipsFileName = "wp_term_relationships.json";
	private const string TermTaxonomyFileName = "wp_term_taxonomy.json";
	private const string TermsFileName = "wp_terms.json";

	private readonly Window _window;

	public MainViewModel(Window window)
	{
		_window = window ?? throw new ArgumentNullException(nameof(window));
	}

	public StorageFolder? SourceFolder { get; set; }

	public string OpenAiApiKey { get; set; } = string.Empty;

	public string ConnectionStringOverride { get; set; } = string.Empty;

	[RelayCommand]
	public async Task OpenSourceFolderAsync()
	{
		var picker = new FolderPicker()
		{
			ViewMode = PickerViewMode.Thumbnail,
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(_window));
		SourceFolder = await picker.PickSingleFolderAsync();
	}

	[RelayCommand]
	public async Task TryParseSourcesAsync()
	{
		if (SourceFolder is null)
		{
			return;
		}

		var files = await SourceFolder.GetFilesAsync();
		var posts = await Parse<Post>(files.FirstOrDefault(f => f.Name == EnrichedPostsFileName) ?? files.FirstOrDefault(f => f.Name == PostsFileName));
		var postMeta = await Parse<PostMeta>(files.FirstOrDefault(files => files.Name == PostMetaFileName));
		var terms = await Parse<Term>(files.FirstOrDefault(files => files.Name == TermsFileName));
		var termTaxonomies = await Parse<TermTaxonomy>(files.FirstOrDefault(files => files.Name == TermTaxonomyFileName));
		var termRelationships = await Parse<TermRelationship>(files.FirstOrDefault(files => files.Name == TermRelationshipsFileName));

		var uniqueTaxonomies = termTaxonomies.Select(t => t.Taxonomy).Distinct();

		var categoryProcessor = new CategoryProcessor(terms, termRelationships, termTaxonomies);
		var categories = categoryProcessor.Process();
		var tagProcessor = new TagProcessor(terms, termRelationships, termTaxonomies);
		var tags = tagProcessor.Process();

		var postTypes = posts.Select(p => p.PostType).Distinct();
		var postStatuses = posts.Select(p => p.PostStatus).Distinct();

		var postProcessor = new PostProcessor(posts, postMeta, terms, termRelationships, termTaxonomies, tags, categories);
		var processedPosts = postProcessor.Process();

		using var databaseContext = CreateDatabaseContext();
		//databaseContext.Database.Migrate();

		var existingCategories = new HashSet<string>(await databaseContext.Category.Select(c => c.RouteName).ToListAsync());
		var existingTags = new HashSet<string>(await databaseContext.Tag.Select(c => c.RouteName).ToListAsync());
		var allPosts = await databaseContext.Post.Select(p => new { Route = p.RouteName, Id = p.Id }).ToListAsync();
		var existingPosts = new HashSet<string>(allPosts.Select(c => c.Route));

		foreach (var category in categories.Values)
		{
			if (!existingCategories.Contains(category.RouteName))
			{
				databaseContext.Add(category);
			}
		}

		await databaseContext.SaveChangesAsync();

		foreach (var tag in tags.Values)
		{
			if (!existingTags.Contains(tag.RouteName))
			{
				databaseContext.Add(tag);
			}
		}

		await databaseContext.SaveChangesAsync();

		List<PostEntity> newPosts = new List<PostEntity>();

		foreach (var post in processedPosts.Posts.Values)
		{
			if (!existingPosts.Contains(post.RouteName))
			{
				databaseContext.Add(post);
				newPosts.Add(post);
			}
			else
			{
				var existingPost = allPosts.Single(p => p.Route == post.RouteName);
				var updatePost = new PostEntity() { Id = existingPost.Id };
				databaseContext.Attach(updatePost);
				updatePost.HeroImageUrl = post.HeroImageUrl;
				updatePost.HeroImageAlt = post.HeroImageAlt;
				updatePost.Abstract = post.Abstract;
			}
		}

		await databaseContext.SaveChangesAsync();

		foreach (var postTag in processedPosts.PostTags)
		{
			if (newPosts.Any(p => p.Id == postTag.PostId))
			{
				databaseContext.Add(postTag);
			}
		}

		await databaseContext.SaveChangesAsync();

		foreach (var postCategory in processedPosts.PostCategories)
		{
			if (newPosts.Any(p => p.Id == postCategory.PostId))
			{
				databaseContext.Add(postCategory);
			}
		}

		await databaseContext.SaveChangesAsync();
	}

	private DatabaseContext CreateDatabaseContext()
	{
		var contextOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
		contextOptionsBuilder.UseSqlServer(GetConnectionString());
		var databaseContext = new DatabaseContext(contextOptionsBuilder.Options);
		return databaseContext;
	}

	private string GetConnectionString()
	{
		if (string.IsNullOrEmpty(ConnectionStringOverride))
		{
			return "Server=(localdb)\\MSSQLLocalDB;Database=MZikmundDb;Trusted_Connection=True;";
		}

		return ConnectionStringOverride;
	}

	[RelayCommand]
	public async Task AddExcerptsAsync()
	{
		if (SourceFolder is null)
		{
			return;
		}

		var files = await SourceFolder.GetFilesAsync();
		var posts = await Parse<Post>(files.FirstOrDefault(f => f.Name == PostsFileName));
		var enricher = new ExcerptEnricher(OpenAiApiKey);
		var terms = await Parse<Term>(files.FirstOrDefault(files => files.Name == TermsFileName));
		var termTaxonomies = await Parse<TermTaxonomy>(files.FirstOrDefault(files => files.Name == TermTaxonomyFileName));
		var termRelationships = await Parse<TermRelationship>(files.FirstOrDefault(files => files.Name == TermRelationshipsFileName));

		var uniqueTaxonomies = termTaxonomies.Select(t => t.Taxonomy).Distinct();

		await enricher.EnrichAsync(posts, terms, termRelationships, termTaxonomies);

		var targetFile = await SourceFolder.CreateFileAsync(EnrichedPostsFileName, CreationCollisionOption.ReplaceExisting);
		var content = JsonSerializer.Serialize(posts);
		await FileIO.WriteTextAsync(targetFile, content);
	}

	[RelayCommand]
	public async Task DecodeCategoriesAsync()
	{
		using var databaseContext = CreateDatabaseContext();
		var allCategories = await databaseContext.Category.ToListAsync();
		foreach (var category in allCategories)
		{
			category.DisplayName = System.Net.WebUtility.HtmlDecode(category.DisplayName);
		}

		await databaseContext.SaveChangesAsync();
	}

	private async Task<IList<T>> Parse<T>(StorageFile? sourceFile)
	{
		if (sourceFile is null)
		{
			throw new ArgumentNullException(nameof(sourceFile));
		}

		using var stream = await sourceFile.OpenStreamForReadAsync();

		var json = await File.ReadAllTextAsync(sourceFile.Path);
		var result = JsonSerializer.Deserialize<IList<T>>(json);
		if (result is null)
		{
			throw new InvalidOperationException("Could not parse JSON file.");
		}

		return result;
	}
}
