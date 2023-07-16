using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using MZikmund.LegacyMigration.Json;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MZikmund.LegacyMigration.ViewModels;

internal partial class MainViewModel : ObservableObject
{
	private const string PostMetaFileName = "wp_postmeta.json";
	private const string PostsFileName = "wp_posts.json";
	private const string TermRelationshipsFileName = "wp_term_relationships.json";
	private const string TermTaxonomyFileName = "wp_term_taxonomy.json";
	private const string TermsFileName = "wp_terms.json";

	private readonly Window _window;

	public MainViewModel(Window window)
	{
		_window = window;
	}

	public StorageFolder SourceFolder { get; set; }

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
		var posts = await Parse<Post>(files.FirstOrDefault(f => f.Name == PostsFileName));
		var postMeta = await Parse<PostMeta>(files.FirstOrDefault(files => files.Name == PostMetaFileName));
		var terms = await Parse<Term>(files.FirstOrDefault(files => files.Name == TermsFileName));
		var termTaxonomy = await Parse<TermTaxonomy>(files.FirstOrDefault(files => files.Name == TermTaxonomyFileName));
		var termRelationships = await Parse<TermRelationship>(files.FirstOrDefault(files => files.Name == TermRelationshipsFileName));


		var uniqueTaxonomies = termTaxonomy.Select(t => t.Taxonomy).Distinct();

	}

	private async Task<IList<T>> Parse<T>(StorageFile sourceFile)
	{
		using var stream = await sourceFile.OpenStreamForReadAsync();

		var json = await File.ReadAllTextAsync(sourceFile.Path);
		return JsonConvert.DeserializeObject<IList<T>>(json);
	}
}
