using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ThptA.Models;

namespace ThptA.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }


    public IActionResult Index()
    {
        // Học sinh được chuyển đến cổng thông tin riêng
        if (User.Identity?.IsAuthenticated == true &&
            User.IsInRole("Student"))
        {
            return RedirectToAction(
                "Index", "StudentPortal");
        }

        // Admin, giáo viên và khách giữ nguyên trang chủ
        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
