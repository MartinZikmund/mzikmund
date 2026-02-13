using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MZikmund.Web.Pages;

public class ContactModel : PageModel
{
    private readonly ILogger<ContactModel> _logger;

    public ContactModel(ILogger<ContactModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public ContactForm Form { get; set; } = new();

    public bool ShowSuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // In a real implementation, you would send email or save to database
        // For now, we'll just log and show success message
        _logger.LogInformation("Contact form submitted by {Name} ({Email}): {Subject}", 
            Form.Name, Form.Email, Form.Subject);

        ShowSuccessMessage = true;
        Form = new(); // Clear the form

        return Page();
    }
}

public class ContactForm
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject is required")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [Display(Name = "Message")]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;
}