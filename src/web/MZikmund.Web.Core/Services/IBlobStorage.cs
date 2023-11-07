namespace MZikmund.Web.Core.Services;

public interface IBlobStorage
{
	Task<string> AddAsync(string fileName, byte[] fileBytes);

	Task<BlobInfo?> GetAsync(string fileName);

	Task DeleteAsync(string fileName);
}
