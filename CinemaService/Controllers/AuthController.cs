using CinemaService.Models;
using CinemaService.Models.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CinemaService.Controllers
{
    public class AuthController : Controller
    {
        private CinemaContext _context;
        private ILogger<AuthController> _logger;

        public AuthController(CinemaContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView model)
        {
            if (ModelState.IsValid)
            {
                User? user = await _context.User.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == GenerateSHA256(model.Password));
                if (user is not null)
                {
                    await Authenticate(user);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Введены некорректные данные");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterView model)
        {
            if (ModelState.IsValid)
            {
                User? user = await _context.User.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user is null)
                {
                    User newUser = new User()
                    {
                        Email = model.Email,
                        PasswordHash = GenerateSHA256(model.Password),
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Birthdate = model.Birthdate,
                        RegisterDate = DateTime.UtcNow,
                        Role = UserRole.Customer
                    };
                    _context.User.Add(newUser);
                    await _context.SaveChangesAsync();
                    await Authenticate(newUser); // аутентификация

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Введены некорректные данные");
                }
            }
            return View(model);
        }

        private async Task Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.FirstName),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.ToString()),
            };
            ClaimsIdentity identity = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string GenerateSHA256(string input)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            string hash = BitConverter.ToString(hashedBytes).Replace("-", "");
            return hash;
        }

    }
}
