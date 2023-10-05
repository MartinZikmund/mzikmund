using System.ComponentModel.DataAnnotations;

namespace MZikmund.Web.Configuration.ConfigSections;

public class AuthorOptions
{
	public string Username { get; set; } = "";

	public string FirstName { get; set; } = "";

	public string LastName { get; set; } = "";

	public string Email { get; set; } = "";

	public string PasswordHash { get; set; } = "";
}
