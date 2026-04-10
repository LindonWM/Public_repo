using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SafeVault.Pages;

[Authorize(Roles = "Admin")]
public class PrivacyModel : PageModel
{
    public void OnGet() { }
}

