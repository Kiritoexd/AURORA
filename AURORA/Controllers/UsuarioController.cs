using AURORA.Data;
using AURORA.Models;
using AURORA.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AURORA.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public UsuarioController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Registro()
        {
            if (User.Identity.IsAuthenticated) return RedirectSegunRol();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registro(Tb_Usuario usuario)
        {
            usuario.Rol = "Lector";
            usuario.FechaRegistro = DateTime.UtcNow;
            if (!ModelState.IsValid) return View(usuario);
            var existe = _context.Usuarios.FirstOrDefault(u => u.Email == usuario.Email);
            if (existe != null)
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese correo.");
                return View(usuario);
            }
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password);
            usuario.ResetToken = null;
            usuario.ResetTokenExpiry = null;
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();
            TempData["Mensaje"] = "Registro exitoso, ahora inicia sesion.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated) return RedirectSegunRol();
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated) return RedirectSegunRol();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == model.Email);
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Password, usuario.Password))
            {
                ModelState.AddModelError("", "Correo o contrasena incorrectos");
                return View(model);
            }
            if (string.IsNullOrEmpty(usuario.Rol)) return RedirectToAction("CuentaBaja");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.ApellidoPaterno}"),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("Email", usuario.Email)
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidad);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            if (usuario.Rol == "Administrador") return RedirectToAction("Index", "Administrador");
            return RedirectToAction("Inicio", "Lector");
        }

        [HttpGet]
        public IActionResult CuentaBaja() => View();

        [HttpGet]
        public IActionResult LoginAdmin()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Administrador"))
                return RedirectToAction("Index", "Administrador");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAdmin(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == model.Email);
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Password, usuario.Password))
            {
                ModelState.AddModelError("", "Correo o contrasena incorrectos.");
                return View(model);
            }
            if (usuario.Rol != "Administrador")
            {
                ModelState.AddModelError("", "Acceso denegado.");
                return View(model);
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.ApellidoPaterno}"),
                new Claim(ClaimTypes.Role, "Administrador"),
                new Claim("Email", usuario.Email)
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidad);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Index", "Administrador");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult NoPermitido() => View();

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "No existe una cuenta con ese correo.");
                return View();
            }

            var rng = new Random();
            var codigo = rng.Next(100000, 999999).ToString();
            usuario.ResetToken = codigo;
            usuario.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            _context.SaveChanges();

            try
            {
                await _emailService.SendPasswordRecoveryCodeAsync(email, codigo);
                Console.WriteLine($"EMAIL OK enviado a {email} codigo={codigo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EMAIL FAIL: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }

            TempData["Mensaje"] = "Se ha enviado un codigo de recuperacion a tu correo.";
            return RedirectToAction("IngresarCodigo");
        }

        [HttpGet]
        public IActionResult IngresarCodigo() => View();

        [HttpPost]
        public IActionResult IngresarCodigo(string codigo)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.ResetToken == codigo && u.ResetTokenExpiry > DateTime.UtcNow);
            if (usuario == null)
            {
                TempData["ErrorCodigo"] = "El codigo ingresado es incorrecto o ha expirado.";
                return View();
            }
            TempData["CodigoValido"] = codigo;
            return RedirectToAction("CambiarContrasena");
        }

        [HttpGet]
        public IActionResult CambiarContrasena()
        {
            if (TempData["CodigoValido"] == null)
            {
                TempData["Error"] = "Primero ingresa un codigo valido.";
                return RedirectToAction("Login");
            }
            var model = new ResetPasswordViewModel { Token = TempData["CodigoValido"].ToString() };
            return View(model);
        }

        [HttpPost]
        public IActionResult CambiarContrasena(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.ResetToken == model.Token && u.ResetTokenExpiry > DateTime.UtcNow);
            if (usuario == null)
            {
                TempData["Error"] = "El codigo es invalido o ha expirado.";
                return RedirectToAction("Login");
            }
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            usuario.ResetToken = null;
            usuario.ResetTokenExpiry = null;
            _context.SaveChanges();
            TempData["Mensaje"] = "Tu contrasena ha sido restablecida correctamente.";
            return RedirectToAction("Login");
        }

        private IActionResult RedirectSegunRol()
        {
            if (User.IsInRole("Administrador")) return RedirectToAction("Index", "Administrador");
            return RedirectToAction("Inicio", "Lector");
        }
    }
}
