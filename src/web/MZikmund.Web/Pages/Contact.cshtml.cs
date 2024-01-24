using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MZikmund.DataContracts.Email;

namespace MZikmund.Web.Pages;

public class ContactModel : PageModel
{
	private readonly IEmailService _emailService;
	private readonly IOptionsMonitor<EmailOptions> _emailOptions;
	private readonly ILogger<ContactModel> _logger;

	private const string EmailSentKey = "EmailSent";

	public ContactModel(
		IEmailService emailService,
		IOptionsMonitor<EmailOptions> emailOptions,
		ILogger<ContactModel> logger)
	{
		_emailService = emailService;
		_emailOptions = emailOptions;
		_logger = logger;
	}

	[BindProperty]
	public ContactFormViewModel Contact { get; set; }

	public bool ShowEmailSent { get; set; }

	public void OnGet()
	{
		if (TempData.ContainsKey(EmailSentKey) && TempData[EmailSentKey] is bool emailSent)
		{
			ShowEmailSent = emailSent;
		}
	}

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		await _emailService.SendPlainEmailAsync(
			new EmailRecipient(_emailOptions.CurrentValue.AdminEmail),
			new EmailRecipient(Contact.Name, Contact.Email),
			Contact.Subject,
			Contact.Message).ConfigureAwait(false);
		TempData[EmailSentKey] = true;
		return RedirectToPage();
	}
}
