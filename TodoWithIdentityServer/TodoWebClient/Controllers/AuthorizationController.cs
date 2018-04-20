using Microsoft.AspNetCore.Mvc;

namespace TodoWebClient.Controllers
{
    public class AuthorizationController : Controller
    {
        public IActionResult AccessDenied()
        {
            // the AccessDeniedPath ("/Authorization/AccessDenied") is set in '.AddCookie' of 'Startup' of this web client.
            return View(null);
        }
    }
}