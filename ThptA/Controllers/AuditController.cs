
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly AppDbContext _db;

        public AuditController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var logs = await _db.AuditLogs
                .AsNoTracking()
                .Include(x => x.Actor)
                .OrderByDescending(x => x.AtUtc)
                .ThenByDescending(x => x.Id)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}
