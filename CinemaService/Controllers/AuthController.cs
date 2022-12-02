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
    /// <summary>
    /// Controller class for Authorization/Authentication pages.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly CinemaContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(CinemaContext context, ILogger<AuthController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Login page GET-method. If user is logged in redirects to index page.
        /// </summary>
        /// <returns>Login page</returns>
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/Cinema/Index");
            }

            return View();
        }

        /// <summary>
        /// Login POST-method to authorize user.
        /// </summary>
        /// <param name="model">Login form model.</param>
        /// <returns>Redirect to index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));
            try
            {
                if (ModelState.IsValid)
                {
                    User? user = await _context.User.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == GenerateSHA256(model.Password));
                    if (user is not null)
                    {
                        await Authenticate(user);
                        return RedirectToAction("Index", "Cinema");
                    }
                    ModelState.AddModelError("", "Введены некорректные данные");
                }
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e.ToString());
                return Redirect("/Cinema/Error");
            }

            return View(model);
        }

        /// <summary>
        /// Register page GET-method. If user is logged in redirects to index page.
        /// </summary>
        /// <returns>Register page</returns>
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/Cinema/Index");
            }

            return View();
        }

        /// <summary>
        /// Register POST-method to register and authorize user.
        /// </summary>
        /// <param name="model">Register form model.</param>
        /// <returns>Redirect to index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterView model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            try
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
                        await Authenticate(newUser);

                        return RedirectToAction("Index", "Cinema");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Введены некорректные данные");
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e.ToString());
                return Redirect("/Cinema/Error");
            }

            return View(model);
        }

        /// <summary>
        /// Saves user authentication data in cookies.
        /// </summary>
        /// <param name="user">User to authenticate.</param>
        /// <returns>Task object.</returns>
        private async Task Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.FirstName),
                new Claim(ClaimsIdentity.DefaultIssuer, user.Email),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.ToString()),
            };
            ClaimsIdentity identity = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        /// <summary>
        /// Logs out currently authorized <see cref="User"/> and removes user data from cookies.
        /// </summary>
        /// <returns>Task object.</returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Cinema");
        }

        /// <summary>
        /// Generates SHA256 result <see cref="string"/> from input <see cref="string"/>.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>SHA256 <see cref="string"/>.</returns>
        private string GenerateSHA256(string input)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            string hash = BitConverter.ToString(hashedBytes).Replace("-", "");
            return hash;
        }

    }
}
