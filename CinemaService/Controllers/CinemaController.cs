using CinemaService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CinemaService.Controllers
{
    public class CinemaController : Controller
    {
        private readonly ILogger<CinemaController> _logger;
        private readonly CinemaContext _context;

        public CinemaController(ILogger<CinemaController> logger, CinemaContext context)
        {
            _logger = logger;
            _context = context;
        }

        //[Authorize(Roles = "Cashier")]
        public IActionResult Index()
        {
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
}