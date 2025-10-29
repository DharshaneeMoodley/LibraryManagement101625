using Microsoft.AspNetCore.Mvc;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnURL = null)
        {
            //REdirect to appropriate dashboard if already logged in
            if ( _authService.IsAuthenticated() )
            {
                return RedirectToRoleDashboard();
            }

            ViewData["ReturnUrl"] = returnURL;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string userName, string password, string? returnUrl = null)
        {
            try
            {
                if ( string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) )
                {
                    ViewBag.Error = "User and password are required";
                    return View();
                }

                var result = await _authService.LoginAsync(userName, password);

                if (result?.Success == true)
                {
                    _logger.LogInformation($"User {userName} logged in successfully");

                    if ( !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) )
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToRoleDashboard();
                }

                ViewBag.Error = result?.Message ?? "Invalid username or password";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ViewBag.Error = "An error occurred during lohin. Please try again.";
                return View();
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userName = _authService.GetCurrentUserName();
                await _authService.LogoutAsync();

                _logger.LogInformation($"User {userName} logged out");

                TempData["Success"] = "You have been logged out successfully";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return RedirectToAction("Login");
            }
        }

        public IActionResult AccessDenied ()
        {
            return View();
        }

        private IActionResult RedirectToRoleDashboard ()
        {
            var roles = _authService.GetCurrentUserRoles();

            //Superadmin and admin will be sent to admin dashboard
            if ( roles.Contains("SuperAdmin") || roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (roles.Contains("Librarian"))
            {
                return RedirectToAction("Index", "Books");
            }

            return RedirectToAction("Index", "Home");
        }




    }
}
