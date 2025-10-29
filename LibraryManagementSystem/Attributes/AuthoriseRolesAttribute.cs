using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryManagementSystem.Attributes
{
    public class AuthoriseRolesAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public AuthoriseRolesAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var authService = context.HttpContext.RequestServices.GetService<AuthService>();

            if ( authService == null || !authService.IsAuthenticated())
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            var userRoles = authService.GetCurrentUserRoles();
            var hasRole = _roles.Any(role => userRoles.Contains(role));

            if (!hasRole)
            {
                context.Result = new ViewResult
                {
                    ViewName = "AccessDenied",
                    ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                        new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                        new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                    {
                        ["Message"] = $"You need one of these roles: {string.Join(",",_roles)}"
                    }
                };
                return;
            }

            base.OnActionExecuting(context);

        }

    }
}
