using Microsoft.AspNetCore.Mvc;
using BloodDonation.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Controllers
{
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;

        public NewsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var newsList = await _context.News
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(newsList);
        }
    }
}
