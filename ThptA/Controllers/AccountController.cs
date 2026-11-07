
using Microsoft.AspNetCore.Mvc;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVm());
        }
    }
}
