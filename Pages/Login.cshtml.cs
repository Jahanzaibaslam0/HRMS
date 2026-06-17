using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS.Pages;

public class LoginModel : PageModel
{
    private readonly AuthService _auth;

    public LoginModel(AuthService auth) => _auth = auth;

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string ErrorMessage { get; set; } = "";
    public string ReturnUrl { get; set; } = "/EmployeeMaster";

    public IActionResult OnGet(string? returnUrl)
    {
        if (_auth.IsLoggedIn)
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/EmployeeMaster" : returnUrl);

        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/EmployeeMaster" : returnUrl;
        return Page();
    }

    public IActionResult OnPost()
    {
        ReturnUrl = string.IsNullOrWhiteSpace(ReturnUrl) ? "/EmployeeMaster" : ReturnUrl;

        var result = _auth.Login(Username, Password);
        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        return Redirect(ReturnUrl);
    }

    public IActionResult OnGetLogout()
    {
        _auth.Logout();
        return RedirectToPage("/Login");
    }
}
