using AURORA.Data;
using AURORA.Models;
using AURORA.ViewModels;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;
using ITextDocument = iText.Kernel.Pdf.PdfDocument;


namespace AURORA.Controllers
{
    public class LectorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LectorController(ApplicationDbContext context)
        {
            _context = context;
        }


        [Authorize(Roles = "Lector")]
        public IActionResult Inicio()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var libros = _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            var racha = _context.Rachas
                .FirstOrDefault(r => r.UsuarioId == usuarioId);

            var vm = new InicioViewModel
            {
                NombreUsuario = User.Identity?.Name ?? "Usuario",
                LibrosTerminados = libros.Count(l => l.Progreso == 100),
                LibrosLeyendo = libros.Count(l => l.Progreso > 0 && l.Progreso < 100),
                LibrosPendientes = libros.Count(l => l.Progreso == 0),
                DiasRacha = racha?.DiasConsecutivos ?? 0
            };

            return View(vm);
        }

        [Authorize(Roles = "Lector")]
        public IActionResult Biblioteca()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var libros = _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            var vm = new LibrosViewModel
            {
                Leyendo = libros.Where(l => l.Progreso > 0 && l.Progreso < 100).ToList(),
                Pendientes = libros.Where(l => l.Progreso == 0).ToList(),
                Completados = libros.Where(l => l.Progreso == 100).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Lector")]
        public async Task<IActionResult> Perfil()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null) return NotFound();

            var racha = await _context.Rachas
                .FirstOrDefaultAsync(r => r.UsuarioId == usuarioId);

            var vm = new PerfilViewModel
            {
                Id = usuario.Id,
                Nombres = usuario.Nombres,
                ApellidoPaterno = usuario.ApellidoPaterno,
                ApellidoMaterno = usuario.ApellidoMaterno,
                Email = usuario.Email,
                FotoUrl = usuario.FotoUrl ?? "",
                FechaRegistro = usuario.FechaRegistro,
                DiasSeguidos = racha?.DiasConsecutivos ?? 0,
                MetaDias = racha?.MetaDias ?? 30,
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        public IActionResult EditarPerfil(PerfilViewModel model)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
            if (usuario != null)
            {
                usuario.Nombres = model.Nombres;
                usuario.ApellidoPaterno = model.ApellidoPaterno;
                usuario.ApellidoMaterno = model.ApellidoMaterno;
                _context.SaveChanges();
                TempData["Success"] = "Perfil actualizado correctamente.";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el perfil.";
            }
            return RedirectToAction("Perfil");
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        public async Task<IActionResult> SubirFoto(IFormFile foto)
        {
            int usuarioId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);

            if (foto == null || foto.Length == 0)
            {
                TempData["Error"] = "Selecciona una imagen válida.";
                return RedirectToAction("Perfil");
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(foto.ContentType.ToLower()))
            {
                TempData["Error"] = "Solo se permiten imágenes (JPG, PNG, GIF, WEBP).";
                return RedirectToAction("Perfil");
            }

            if (foto.Length > 2 * 1024 * 1024)
            {
                TempData["Error"] = "La imagen no debe superar 2MB.";
                return RedirectToAction("Perfil");
            }

            if (usuario != null)
            {
                using var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                usuario.FotoUrl = $"data:{foto.ContentType};base64,{base64}";
                _context.Update(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Foto de perfil actualizada.";
            }

            return RedirectToAction("Perfil");
        }

        [Authorize(Roles = "Lector")]
        public IActionResult AbrirLibro(int id)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var usuarioLibro = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .FirstOrDefault(ul => ul.UsuarioId == usuarioId && ul.LibroId == id);

            if (usuarioLibro == null)
                return RedirectToAction("Biblioteca");

            int? libroId = usuarioLibro?.LibroId;
            int ultimaPagina = usuarioLibro.UltimaPagina;
            int totalPaginas = usuarioLibro.Libro?.Paginas ?? 0;

            usuarioLibro.Progreso = CalcularProgreso(totalPaginas, ultimaPagina);

            return View(usuarioLibro);
        }


        [Authorize(Roles = "Lector")]
        public IActionResult Estadisticas()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibros = _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            var model = new EstadisticasViewModel
            {
                LibrosPendientes = usuarioLibros.Count(u => u.Progreso == 0),
                LibrosLeyendo = usuarioLibros.Count(u => u.Progreso > 0 && u.Progreso < 100),
                LibrosTerminados = usuarioLibros.Count(u => u.Progreso == 100),

                Generos = usuarioLibros
                    .Where(u => u.Libro?.Genero != null)
                    .Select(u => u.Libro.Genero)
                    .Distinct()
                    .ToList(),

                CantidadPorGenero = usuarioLibros
                    .Where(u => u.Libro?.Genero != null)
                    .GroupBy(u => u.Libro.Genero)
                    .Select(g => g.Count())
                    .ToList(),

                Anios = usuarioLibros
                    .Where(u => u.UltimoAcceso.HasValue)
                    .GroupBy(u => u.UltimoAcceso.Value.Year)
                    .Select(g => g.Key.ToString())
                    .ToList(),

                LibrosPorAnio = usuarioLibros
                    .Where(u => u.UltimoAcceso.HasValue)
                    .GroupBy(u => u.UltimoAcceso.Value.Year)
                    .Select(g => g.Count())
                    .ToList(),
            };

            return View(model);
        }


        public async Task<IActionResult> Top()
        {
            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var top = await _context.UsuarioLibros
                .Include(u => u.Libro)
                .Where(u => u.UsuarioId == usuarioId)
                .OrderBy(u => u.Posicion)
                .Take(6)
                .Select(u => new TopLibroViewModel
                {
                    LibroId = u.Libro.Id,
                    Titulo = u.Libro.Titulo ?? "Sin título",
                    Autor = u.Libro.Autor ?? "Desconocido",
                    Genero = u.Libro.Genero ?? "N/A",
                    Progreso = u.Progreso,
                    Posicion = u.Posicion,
                    PortadaUrl = u.Libro.PortadaUrl
                })
                .ToListAsync();

            return View(top);
        }


        [HttpPost]
        [Authorize(Roles = "Lector")]
        public async Task<IActionResult> EliminarPdf(int id)
        {
            await _context.UsuarioLibros
                .Where(ul => ul.LibroId == id)
                .ExecuteDeleteAsync();

            await _context.Libros
                .Where(l => l.Id == id)
                .ExecuteDeleteAsync();

            return RedirectToAction("Biblioteca");
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        public IActionResult GuardarProgreso(int id, int pagina, string? redirectTo = null)
        {
            var usuarioLibro = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .FirstOrDefault(ul => ul.Id == id);

            if (usuarioLibro != null && usuarioLibro.Libro != null)
            {
                usuarioLibro.UltimaPagina = pagina;

                var paginas = usuarioLibro.Libro.Paginas;
                usuarioLibro.Progreso = paginas > 0
                    ? (int)Math.Round((decimal)pagina * 100m / paginas)
                    : 0;

                usuarioLibro.UltimoAcceso = DateTime.UtcNow;
                _context.SaveChanges();
            }

            if (redirectTo == "Biblioteca")
                return RedirectToAction("Biblioteca");

            return RedirectToAction("AbrirLibro", new { id = id });
        }


        private int CalcularProgreso(int? totalPaginas, int paginaActual)
        {
            if (!totalPaginas.HasValue || totalPaginas.Value == 0) return 0;
            if (paginaActual > totalPaginas.Value) paginaActual = totalPaginas.Value;
            return (int)((paginaActual * 100.0) / totalPaginas.Value);
        }

        [Authorize(Roles = "Lector")]
        public IActionResult LeerLibro(int id)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .FirstOrDefault(ul => ul.UsuarioId == usuarioId && ul.LibroId == id);

            if (usuarioLibro == null)
                return RedirectToAction("Biblioteca");

            usuarioLibro.UltimoAcceso = DateTime.UtcNow;
            _context.SaveChanges();

            return View(usuarioLibro);
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        public IActionResult CerrarLectura(int idLibro)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .FirstOrDefault(x => x.LibroId == idLibro && x.UsuarioId == usuarioId);

            if (usuarioLibro != null && usuarioLibro.UltimoInicioLectura.HasValue)
            {
                var fin = DateTime.UtcNow;
                var tiempo = fin - usuarioLibro.UltimoInicioLectura.Value;
                usuarioLibro.TiempoLectura += tiempo;
                usuarioLibro.UltimoInicioLectura = null;
                _context.SaveChanges();
            }

            return RedirectToAction("Estadisticas", "Lector");
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        public async Task<IActionResult> ImportarPdf(IFormFile pdfLibro, string genero)
        {
            if (pdfLibro == null || pdfLibro.Length == 0 || string.IsNullOrEmpty(genero))
                return RedirectToAction("Biblioteca");

            string texto = "";
            int totalPaginas = 0;
            string titulo = Path.GetFileNameWithoutExtension(pdfLibro.FileName);
            string autor = "Autor desconocido";

            using var ms = new MemoryStream();
            await pdfLibro.CopyToAsync(ms);
            var pdfBytes = ms.ToArray();
            ms.Position = 0;

            using (var pdf = PdfPigDocument.Open(ms))
            {
                totalPaginas = pdf.NumberOfPages;

                if (!string.IsNullOrWhiteSpace(pdf.Information.Title))
                    titulo = pdf.Information.Title;
                if (!string.IsNullOrWhiteSpace(pdf.Information.Author))
                    autor = pdf.Information.Author;

                var sb = new System.Text.StringBuilder();
                foreach (var pagina in pdf.GetPages())
                    sb.AppendLine(pagina.Text);
                texto = sb.ToString();
            }

            var libro = new Tb_Libro
            {
                Titulo = titulo,
                Autor = autor,
                Genero = genero,
                Editorial = "Usuario",
                Año = DateTime.UtcNow.Year,
                Paginas = totalPaginas,
                RutaPdf = null,
                ContenidoTexto = texto,
                PdfBytes = pdfBytes
            };

            _context.Libros.Add(libro);
            await _context.SaveChangesAsync();

            int usuarioId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            var usuarioLibro = new Tb_UsuarioLibro
            {
                UsuarioId = usuarioId,
                LibroId = libro.Id,
                Progreso = 0,
                UltimaPagina = 1,
                UltimoAcceso = DateTime.UtcNow
            };

            _context.UsuarioLibros.Add(usuarioLibro);
            await _context.SaveChangesAsync();

            return RedirectToAction("Biblioteca");
        }

        public IActionResult ProxyPdf(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            var claimId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claimId, out int usuarioId))
                return Unauthorized();

            var tieneAcceso = _context.UsuarioLibros
                .Any(ul => ul.UsuarioId == usuarioId && ul.LibroId == id);

            if (!tieneAcceso)
                return Unauthorized();

            var pdfBytes = _context.Libros
                .Where(l => l.Id == id)
                .Select(l => l.PdfBytes)
                .FirstOrDefault();

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound();

            Response.Headers["Cache-Control"] = "private, max-age=3600";
            return File(pdfBytes, "application/pdf");
        }

        public IActionResult Leer(int idLibro)
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var usuarioLibro = _context.UsuarioLibros
                .FirstOrDefault(x => x.LibroId == idLibro && x.UsuarioId == usuarioId);

            if (usuarioLibro != null)
            {
                usuarioLibro.UltimoInicioLectura = DateTime.UtcNow;
                _context.SaveChanges();
            }

            return View(usuarioLibro);
        }

        public IActionResult Recomendaciones()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Lector")]
        public IActionResult Racha()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var racha = _context.Rachas.FirstOrDefault(r => r.UsuarioId == usuarioId);

            if (racha == null)
            {
                racha = new Tb_Racha
                {
                    UsuarioId = usuarioId,
                    DiasConsecutivos = 0,
                    UltimaLectura = null,
                    MetaDias = 30
                };
                _context.Rachas.Add(racha);
                _context.SaveChanges();
            }

            bool yaRegistroHoy = racha.UltimaLectura?.Date == DateTime.UtcNow.Date;
            bool esNuevoRegistro = false;

            if (!yaRegistroHoy)
            {
                racha.DiasConsecutivos = racha.UltimaLectura?.Date == DateTime.UtcNow.Date.AddDays(-1)
                    ? racha.DiasConsecutivos + 1
                    : 1;

                racha.UltimaLectura = DateTime.UtcNow.Date;
                _context.SaveChanges();
                esNuevoRegistro = true;
            }

            ViewBag.EsNuevoRegistro = esNuevoRegistro;

            var logros = new List<LogroViewModel>
            {
                new() { Nombre = "Primera semana",    Dias = 7,   Desbloqueado = racha.DiasConsecutivos >= 7   },
                new() { Nombre = "Lectura constante", Dias = 15,  Desbloqueado = racha.DiasConsecutivos >= 15  },
                new() { Nombre = "Meta mensual",      Dias = 30,  Desbloqueado = racha.DiasConsecutivos >= 30  },
                new() { Nombre = "Super lector",      Dias = 60,  Desbloqueado = racha.DiasConsecutivos >= 60  },
                new() { Nombre = "Leyenda",           Dias = 100, Desbloqueado = racha.DiasConsecutivos >= 100 },
            };

            var model = new RachaViewModel
            {
                DiasSeguidos = racha.DiasConsecutivos,
                MetaDias = racha.MetaDias,
                Logros = logros
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarLectura()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var racha = _context.Rachas.FirstOrDefault(r => r.UsuarioId == usuarioId);

            if (racha == null)
            {
                racha = new Tb_Racha
                {
                    UsuarioId = usuarioId,
                    DiasConsecutivos = 0,
                    UltimaLectura = null,
                    MetaDias = 30
                };
                _context.Rachas.Add(racha);
            }

            if (racha.UltimaLectura?.Date == DateTime.UtcNow.Date)
            {
                TempData["Info"] = "Ya registraste tu lectura hoy.";
                return RedirectToAction("Racha");
            }

            racha.DiasConsecutivos = racha.UltimaLectura?.Date == DateTime.UtcNow.Date.AddDays(-1)
                ? racha.DiasConsecutivos + 1
                : 1;

            racha.UltimaLectura = DateTime.UtcNow.Date;
            _context.SaveChanges();

            return RedirectToAction("Racha");
        }

        // ═══════════════════════════════════════════════════════
        //  LOGROS
        // ═══════════════════════════════════════════════════════

        // SVG icons reutilizables
        private const string IcoLibro = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19V5a2 2 0 0 1 2-2h13a1 1 0 0 1 1 1v13"/><path d="M4 19a2 2 0 0 0 2 2h13a1 1 0 0 0 1-1"/></svg>""";
        private const string IcoPag = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>""";
        private const string IcoReloj = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>""";
        private const string IcoRacha = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/></svg>""";
        private const string IcoBrujula = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polygon points="16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76"/></svg>""";
        private const string IcoGenero = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>""";
        private const string IcoPdf = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>""";
        private const string IcoCheck = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>""";
        private const string IcoNota = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>""";
        private const string IcoIA = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M4.22 4.22l2.12 2.12M17.66 17.66l2.12 2.12M2 12h3M19 12h3M4.22 19.78l2.12-2.12M17.66 6.34l2.12-2.12"/></svg>""";
        private const string IcoStats = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>""";
        private const string IcoRayo = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/></svg>""";
        private const string IcoLuna = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>""";
        private const string IcoSol = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>""";
        private const string IcoUser = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>""";
        private const string IcoMeta = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="2"/></svg>""";
        private const string IcoFuego = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/></svg>""";
        private const string IcoCopa = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"/><path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"/><path d="M4 22h16"/><path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"/></svg>""";
        private const string IcoLibrote = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>""";
        private const string IcoCalend = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>""";

        private static readonly List<LogroDef> LOGROS_DEF = new()
        {
            // ── DIARIOS ───────────────────────────────────────────
            new("diario_abrir_libro", "diario",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>""",
                "Abre un libro", "Abre cualquier libro hoy.", 1),

            new("diario_10_paginas", "diario",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>""",
                "10 páginas", "Lee al menos 10 páginas en el día.", 10),

            new("diario_25_paginas", "diario",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2z"/><polyline points="9 11 12 14 22 4"/></svg>""",
                "Lector activo", "Lee al menos 25 páginas en el día.", 25),

            new("diario_guardar_progreso", "diario",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>""",
                "Guarda tu avance", "Guarda el progreso de lectura al menos una vez.", 1),

            // ── PERMANENTES — LIBROS COMPLETADOS ─────────────────
            new("perm_primer_libro", "permanente", IcoCopa,
                "Primer libro", "Completa tu primer libro al 100%.", 1),

            new("perm_5_libros", "permanente", IcoLibro,
                "Bibliófilo", "Completa 5 libros al 100%.", 5),

            new("perm_10_libros", "permanente", IcoLibro,
                "Decena de lecturas", "Completa 10 libros.", 10),

            new("perm_20_libros", "permanente", IcoLibro,
                "Veinte mundos", "Completa 20 libros.", 20),

            new("perm_50_libros", "permanente", IcoLibro,
                "Cincuenta historias", "Completa 50 libros.", 50),

            new("perm_100_libros", "permanente", IcoLibrote,
                "Centenario", "Completa 100 libros. Leyenda.", 100),

            // ── PERMANENTES — PÁGINAS ─────────────────────────────
            new("perm_100_paginas", "permanente", IcoPag,
                "100 páginas", "Acumula 100 páginas leídas en total.", 100),

            new("perm_500_paginas", "permanente", IcoPag,
                "500 páginas", "Acumula 500 páginas leídas en total.", 500),

            new("perm_1000_paginas", "permanente", IcoPag,
                "Mil páginas", "Acumula 1 000 páginas en total.", 1000),

            new("perm_5000_paginas", "permanente", IcoPag,
                "Cinco mil páginas", "Acumula 5 000 páginas en total.", 5000),

            new("perm_10000_paginas", "permanente", IcoPag,
                "Diez mil páginas", "Acumula 10 000 páginas. Tu mente es un océano.", 10000),

            // ── PERMANENTES — RACHAS ──────────────────────────────
            new("perm_racha_3", "permanente", IcoRacha,
                "Tres días seguidos", "Mantén una racha de 3 días.", 3),

            new("perm_racha_7", "permanente", IcoRacha,
                "Racha 7 días", "Mantén una racha de lectura de 7 días.", 7),

            new("perm_racha_14", "permanente", IcoRacha,
                "Dos semanas", "Racha de 14 días.", 14),

            new("perm_racha_30", "permanente", IcoRacha,
                "Racha 30 días", "Mantén una racha de lectura de 30 días.", 30),

            new("perm_racha_60", "permanente", IcoRacha,
                "Dos meses", "Racha de 60 días.", 60),

            new("perm_racha_100", "permanente", IcoFuego,
                "Cien días", "Racha de 100 días consecutivos.", 100),

            new("perm_racha_180", "permanente", IcoFuego,
                "Medio año", "Racha de 180 días. Imparable.", 180),

            new("perm_racha_365", "permanente", IcoFuego,
                "Un año entero", "Racha de 365 días. Eres extraordinario.", 365),

            // ── PERMANENTES — PDFs SUBIDOS ────────────────────────
            new("perm_pdf_1", "permanente", IcoPdf,
                "Primer PDF", "Sube tu primer PDF a Aurora.", 1),

            new("perm_pdf_5", "permanente", IcoPdf,
                "Cinco subidos", "Sube 5 PDFs.", 5),

            new("perm_pdf_25", "permanente", IcoPdf,
                "Coleccionista", "Sube 25 PDFs.", 25),

            new("perm_pdf_50", "permanente", IcoPdf,
                "Archivo personal", "Sube 50 PDFs.", 50),

            new("perm_pdf_100", "permanente", IcoPdf,
                "Gran archivo", "Sube 100 PDFs.", 100),

            // ── PERMANENTES — GÉNEROS ─────────────────────────────
            new("perm_generos_3", "permanente", IcoBrujula,
                "Explorador", "Lee libros de al menos 3 géneros distintos.", 3),

            new("perm_generos_6", "permanente", IcoBrujula,
                "Omnívoro literario", "Lee libros de 6 géneros distintos.", 6),

            new("perm_generos_10", "permanente", IcoBrujula,
                "Sin fronteras", "Lee libros de 10 géneros distintos.", 10),

            new("perm_misterio_5", "permanente", IcoGenero,
                "Amante del misterio", "Lee 5 libros del género Misterio.", 5),

            new("perm_scifi_5", "permanente", IcoGenero,
                "Viajero espacial", "Lee 5 libros de Ciencia Ficción.", 5),

            new("perm_historia_5", "permanente", IcoGenero,
                "Corazón histórico", "Lee 5 libros de Historia.", 5),

            new("perm_romance_5", "permanente", IcoGenero,
                "Alma romántica", "Lee 5 libros de Romance.", 5),

            new("perm_ciencia_5", "permanente", IcoGenero,
                "Mente científica", "Lee 5 libros de Ciencia.", 5),

            new("perm_filosofia_5", "permanente", IcoGenero,
                "Filósofo en ciernes", "Lee 5 libros de Filosofía.", 5),

            new("perm_fantasia_5", "permanente", IcoGenero,
                "Mundo de fantasía", "Lee 5 libros de Fantasía.", 5),

            new("perm_biografia_5", "permanente", IcoGenero,
                "Biógrafo aficionado", "Lee 5 biografías.", 5),

            // ── PERMANENTES — IA ──────────────────────────────────
            new("perm_ia_rec_1", "permanente", IcoIA,
                "Primera recomendación", "Recibe tu primera recomendación de la IA.", 1),

            new("perm_ia_rec_5", "permanente", IcoIA,
                "Cinco recomendaciones", "Recibe 5 recomendaciones de la IA.", 5),

            new("perm_ia_rec_10", "permanente", IcoIA,
                "Confío en la IA", "Recibe 10 recomendaciones.", 10),

            new("perm_ia_resumen_1", "permanente", IcoIA,
                "Primer resumen", "Genera tu primer resumen con IA.", 1),

            new("perm_ia_resumen_10", "permanente", IcoIA,
                "Diez resúmenes", "Genera 10 resúmenes con IA.", 10),

            new("perm_ia_resumen_50", "permanente", IcoIA,
                "Cincuenta resúmenes", "Genera 50 resúmenes con IA.", 50),

            new("perm_ia_chat_10", "permanente", IcoIA,
                "Chat lector", "Chatea 10 veces con el asistente de lectura.", 10),

            new("perm_ia_chat_50", "permanente", IcoIA,
                "Asistente frecuente", "Chatea 50 veces con el asistente.", 50),

            // ── PERMANENTES — ESTADÍSTICAS ────────────────────────
            new("perm_stats_1", "permanente", IcoStats,
                "Mi primer reporte", "Consulta tus estadísticas por primera vez.", 1),

            new("perm_stats_10", "permanente", IcoStats,
                "Autoconocimiento", "Consulta tus estadísticas 10 veces.", 10),

            new("perm_mes_activo_1", "permanente", IcoCalend,
                "Mes completo", "Mantén actividad de lectura durante un mes.", 1),

            new("perm_mes_activo_3", "permanente", IcoCalend,
                "Tres meses activo", "Mantén actividad durante 3 meses.", 3),

            new("perm_mes_activo_6", "permanente", IcoCalend,
                "Medio año activo", "Mantén actividad durante 6 meses.", 6),

            new("perm_mes_activo_12", "permanente", IcoCalend,
                "Un año en Aurora", "Mantén actividad durante 12 meses.", 12),

            // ── PERMANENTES — VELOCIDAD ───────────────────────────
            new("perm_express_1", "permanente", IcoRayo,
                "Lectura express", "Termina un libro en menos de 24 horas.", 1),

            new("perm_express_5", "permanente", IcoRayo,
                "Velocista", "Termina 5 libros en tiempo récord.", 5),

            new("perm_nocturno_10", "permanente", IcoLuna,
                "Lector nocturno", "Lee después de las 11 PM en 10 ocasiones.", 10),

            new("perm_madrugador_10", "permanente", IcoSol,
                "Madrugador literario", "Lee antes de las 7 AM en 10 ocasiones.", 10),

            // ── PERMANENTES — PERFIL ──────────────────────────────
            new("perm_perfil_completo", "permanente", IcoUser,
                "Perfil completo", "Completa todos los campos de tu perfil.", 1),

            new("perm_foto_perfil", "permanente", IcoUser,
                "Avatar puesto", "Sube una foto de perfil.", 1),

            // ── PERMANENTES — LOGROS DIARIOS ACUMULADOS ──────────
            new("perm_diarios_x7", "permanente", IcoFuego,
                "Racha de logros x7", "Completa todos los logros diarios 7 días seguidos.", 7),

            new("perm_diarios_x30", "permanente", IcoFuego,
                "Racha de logros x30", "Completa todos los logros diarios 30 días seguidos.", 30),

            new("perm_diarios_x100", "permanente", IcoFuego,
                "Perfeccionista", "Completa todos los logros diarios 100 días en total.", 100),

            // ── PERMANENTES — HITOS ───────────────────────────────
            new("perm_logro_1", "permanente", IcoCopa,
                "Primer logro", "Reclama tu primer logro de cualquier tipo.", 1),

            new("perm_logro_10", "permanente", IcoCopa,
                "Diez logros", "Reclama 10 logros en total.", 10),

            new("perm_logro_50", "permanente", IcoCopa,
                "Cincuenta logros", "Reclama 50 logros en total.", 50),

            new("perm_logro_100", "permanente", IcoCopa,
                "Cien logros", "Reclama 100 logros. Eres el jugador definitivo.", 100),

            // ── PERMANENTES — LIBRO MÁS LARGO / CORTO ────────────
            new("perm_libro_largo", "permanente", IcoLibrote,
                "El libro más largo", "Termina un libro de más de 1 000 páginas.", 1),

            new("perm_libro_corto", "permanente", IcoLibrote,
                "El libro más corto", "Termina un libro de menos de 50 páginas.", 1),

            new("perm_sesion_3h", "permanente", IcoReloj,
                "Sin pausas", "Lee más de 3 horas en una sola sesión.", 1),

            new("perm_finde_200pag", "permanente", IcoPag,
                "Fin de semana literario", "Lee más de 200 páginas en un fin de semana.", 200),

            new("perm_multitarea_3", "permanente", IcoLibro,
                "Multitarea literaria", "Ten 3 libros en estado 'Leyendo' al mismo tiempo.", 3),

            new("perm_domingo_10", "permanente", IcoCalend,
                "Domingo de lectura", "Lee todos los domingos durante 10 semanas seguidas.", 10),

            new("perm_viernes_5", "permanente", IcoCalend,
                "Noche de viernes", "Lee un viernes por la noche en 5 ocasiones.", 5),

            // ── ESPECIALES ────────────────────────────────────────
            new("esp_madrugador", "especial",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>""",
                "Madrugador", "Lee antes de las 8 AM.", 1),

            new("esp_nocturno", "especial",
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>""",
                "Lectura nocturna", "Lee después de las 10 PM.", 1),
        };

        [Authorize(Roles = "Lector")]
        public IActionResult Logros()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var reclamadosDB = _context.LogrosReclamados
                .Where(l => l.UsuarioId == usuarioId)
                .Select(l => l.LogroId)
                .ToHashSet();

            var estado = CargarEstado();
            SincronizarLogrosDesdeDB(ref estado, usuarioId, reclamadosDB);
            GuardarEstado(estado);

            return View(BuildViewModel(estado, reclamadosDB));
        }

        [HttpPost]
        [Authorize(Roles = "Lector")]
        [ValidateAntiForgeryToken]
        public IActionResult Reclamar(string id)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var def = LOGROS_DEF.FirstOrDefault(d => d.Id == id);
                if (def is null)
                    return Json(new { ok = false, msg = "Logro no encontrado." });

                var yaEnBD = _context.LogrosReclamados
                    .Any(l => l.UsuarioId == usuarioId && l.LogroId == id);
                if (yaEnBD)
                    return Json(new { ok = false, msg = "Ya reclamado." });

                var reclamadosDB = _context.LogrosReclamados
                    .Where(l => l.UsuarioId == usuarioId)
                    .Select(l => l.LogroId)
                    .ToHashSet();

                var estado = CargarEstado();
                SincronizarLogrosDesdeDB(ref estado, usuarioId, reclamadosDB);

                var entrada = estado.ContainsKey(id) ? estado[id] : new LogroEntrada();

                if (!entrada.Completado)
                    return Json(new { ok = false, msg = $"No completado. Progreso: {entrada.Progreso}/{def.Meta}" });

                _context.LogrosReclamados.Add(new Tb_LogroReclamado
                {
                    UsuarioId = usuarioId,
                    LogroId = id,
                    FechaReclamo = DateTime.UtcNow
                });
                _context.SaveChanges();

                return Json(new { ok = true, nombre = def.Nombre });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "sin inner exception";
                return Json(new { ok = false, msg = $"{ex.Message} | Inner: {inner}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetearDiarios()
        {
            var estado = CargarEstado();
            foreach (var def in LOGROS_DEF.Where(d => d.Tipo == "diario"))
            {
                if (estado.ContainsKey(def.Id))
                {
                    estado[def.Id].Progreso = 0;
                    estado[def.Id].Completado = false;
                }
            }
            HttpContext.Session.SetString(SESSION_FECHA, "");
            GuardarEstado(estado);
            return Json(new { ok = true });
        }

        private const string SESSION_ESTADO = "logros_estado_v1";
        private const string SESSION_FECHA = "logros_ultima_fecha";

        private void SincronizarLogrosDesdeDB(
            ref Dictionary<string, LogroEntrada> estado,
            int usuarioId,
            HashSet<string> reclamadosDB)
        {
            var hoy = DateTime.UtcNow.Date;

            var librosHoy = _context.UsuarioLibros
                .Where(ul => ul.UsuarioId == usuarioId
                          && ul.UltimoAcceso.HasValue
                          && ul.UltimoAcceso.Value.Date == hoy)
                .ToList();

            int paginasHoy = librosHoy.Sum(ul => ul.UltimaPagina);
            bool abrioLibro = librosHoy.Any();
            bool guardo = librosHoy.Any(ul => ul.UltimoAcceso.HasValue
                                           && ul.UltimoAcceso.Value.Date == hoy);

            // Diarios
            SetProgreso(ref estado, "diario_abrir_libro", abrioLibro ? 1 : 0, reclamadosDB);
            SetProgreso(ref estado, "diario_10_paginas", Math.Min(paginasHoy, 10), reclamadosDB);
            SetProgreso(ref estado, "diario_25_paginas", Math.Min(paginasHoy, 25), reclamadosDB);
            SetProgreso(ref estado, "diario_guardar_progreso", guardo ? 1 : 0, reclamadosDB);

            // Especiales por hora
            var hora = DateTime.UtcNow.Hour;
            if (abrioLibro && hora < 8) SetProgreso(ref estado, "esp_madrugador", 1, reclamadosDB);
            if (abrioLibro && hora >= 22) SetProgreso(ref estado, "esp_nocturno", 1, reclamadosDB);

            // Totales de páginas
            var totalPaginas = _context.UsuarioLibros
                .Where(ul => ul.UsuarioId == usuarioId)
                .Sum(ul => (int?)ul.UltimaPagina) ?? 0;

            SetProgreso(ref estado, "perm_100_paginas", Math.Min(totalPaginas, 100), reclamadosDB);
            SetProgreso(ref estado, "perm_500_paginas", Math.Min(totalPaginas, 500), reclamadosDB);
            SetProgreso(ref estado, "perm_1000_paginas", Math.Min(totalPaginas, 1000), reclamadosDB);
            SetProgreso(ref estado, "perm_5000_paginas", Math.Min(totalPaginas, 5000), reclamadosDB);
            SetProgreso(ref estado, "perm_10000_paginas", Math.Min(totalPaginas, 10000), reclamadosDB);
            SetProgreso(ref estado, "perm_finde_200pag", Math.Min(paginasHoy, 200), reclamadosDB);

            // Libros completados
            var librosCompletos = _context.UsuarioLibros
                .Count(ul => ul.UsuarioId == usuarioId && ul.Progreso >= 100);

            SetProgreso(ref estado, "perm_primer_libro", Math.Min(librosCompletos, 1), reclamadosDB);
            SetProgreso(ref estado, "perm_5_libros", Math.Min(librosCompletos, 5), reclamadosDB);
            SetProgreso(ref estado, "perm_10_libros", Math.Min(librosCompletos, 10), reclamadosDB);
            SetProgreso(ref estado, "perm_20_libros", Math.Min(librosCompletos, 20), reclamadosDB);
            SetProgreso(ref estado, "perm_50_libros", Math.Min(librosCompletos, 50), reclamadosDB);
            SetProgreso(ref estado, "perm_100_libros", Math.Min(librosCompletos, 100), reclamadosDB);

            // Rachas
            var racha = _context.Rachas.FirstOrDefault(r => r.UsuarioId == usuarioId);
            int diasRacha = racha?.DiasConsecutivos ?? 0;

            SetProgreso(ref estado, "perm_racha_3", Math.Min(diasRacha, 3), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_7", Math.Min(diasRacha, 7), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_14", Math.Min(diasRacha, 14), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_30", Math.Min(diasRacha, 30), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_60", Math.Min(diasRacha, 60), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_100", Math.Min(diasRacha, 100), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_180", Math.Min(diasRacha, 180), reclamadosDB);
            SetProgreso(ref estado, "perm_racha_365", Math.Min(diasRacha, 365), reclamadosDB);

            // PDFs subidos
            var totalPdfs = _context.UsuarioLibros
                .Count(ul => ul.UsuarioId == usuarioId);

            SetProgreso(ref estado, "perm_pdf_1", Math.Min(totalPdfs, 1), reclamadosDB);
            SetProgreso(ref estado, "perm_pdf_5", Math.Min(totalPdfs, 5), reclamadosDB);
            SetProgreso(ref estado, "perm_pdf_25", Math.Min(totalPdfs, 25), reclamadosDB);
            SetProgreso(ref estado, "perm_pdf_50", Math.Min(totalPdfs, 50), reclamadosDB);
            SetProgreso(ref estado, "perm_pdf_100", Math.Min(totalPdfs, 100), reclamadosDB);

            // Géneros distintos
            var generosDistintos = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .Where(ul => ul.UsuarioId == usuarioId && ul.Libro.Genero != null)
                .Select(ul => ul.Libro.Genero)
                .Distinct()
                .Count();

            SetProgreso(ref estado, "perm_generos_3", Math.Min(generosDistintos, 3), reclamadosDB);
            SetProgreso(ref estado, "perm_generos_6", Math.Min(generosDistintos, 6), reclamadosDB);
            SetProgreso(ref estado, "perm_generos_10", Math.Min(generosDistintos, 10), reclamadosDB);

            void SetGenero(ref Dictionary<string, LogroEntrada> estado, string id, string genero, int meta)
            {
                var cnt = _context.UsuarioLibros
                    .Include(ul => ul.Libro)
                    .Count(ul => ul.UsuarioId == usuarioId
                              && ul.Progreso >= 100
                              && ul.Libro.Genero == genero);
                SetProgreso(ref estado, id, Math.Min(cnt, meta), reclamadosDB);
            }

            SetGenero(ref estado, "perm_misterio_5", "Misterio", 5);
            SetGenero(ref estado, "perm_scifi_5", "Ciencia Ficción", 5);
            SetGenero(ref estado, "perm_historia_5", "Historia", 5);
            SetGenero(ref estado, "perm_romance_5", "Romance", 5);
            SetGenero(ref estado, "perm_ciencia_5", "Ciencia", 5);
            SetGenero(ref estado, "perm_filosofia_5", "Filosofía", 5);
            SetGenero(ref estado, "perm_fantasia_5", "Fantasía", 5);
            SetGenero(ref estado, "perm_biografia_5", "Biografía", 5);

            // Libros leyendo simultáneamente
            var leyendoAhora = _context.UsuarioLibros
                .Count(ul => ul.UsuarioId == usuarioId && ul.Progreso > 0 && ul.Progreso < 100);
            SetProgreso(ref estado, "perm_multitarea_3", Math.Min(leyendoAhora, 3), reclamadosDB);

            // Foto de perfil
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
            bool tieneFoto = !string.IsNullOrEmpty(usuario?.FotoUrl);
            SetProgreso(ref estado, "perm_foto_perfil", tieneFoto ? 1 : 0, reclamadosDB);
            SetProgreso(ref estado, "perm_perfil_completo", tieneFoto ? 1 : 0, reclamadosDB);

            // Logros reclamados totales
            var totalReclamados = reclamadosDB.Count;
            SetProgreso(ref estado, "perm_logro_1", Math.Min(totalReclamados, 1), reclamadosDB);
            SetProgreso(ref estado, "perm_logro_10", Math.Min(totalReclamados, 10), reclamadosDB);
            SetProgreso(ref estado, "perm_logro_50", Math.Min(totalReclamados, 50), reclamadosDB);
            SetProgreso(ref estado, "perm_logro_100", Math.Min(totalReclamados, 100), reclamadosDB);

            // Libro largo / corto (requiere que exista campo Paginas en el libro)
            bool tieneLibroLargo = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .Any(ul => ul.UsuarioId == usuarioId && ul.Progreso >= 100 && ul.Libro.Paginas > 1000);
            bool tieneLibroCorto = _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .Any(ul => ul.UsuarioId == usuarioId && ul.Progreso >= 100 && ul.Libro.Paginas < 50);
            SetProgreso(ref estado, "perm_libro_largo", tieneLibroLargo ? 1 : 0, reclamadosDB);
            SetProgreso(ref estado, "perm_libro_corto", tieneLibroCorto ? 1 : 0, reclamadosDB);

            // Nocturno / madrugador acumulado (simplificado: si aplica hoy suma 1)
            // Para un conteo real necesitarías una tabla de sesiones; por ahora se activa con la hora
            if (abrioLibro && hora >= 23)
                SetProgreso(ref estado, "perm_nocturno_10", 1, reclamadosDB);
            if (abrioLibro && hora < 7)
                SetProgreso(ref estado, "perm_madrugador_10", 1, reclamadosDB);
        }

        private void SetProgreso(
            ref Dictionary<string, LogroEntrada> estado,
            string id, int valor,
            HashSet<string> reclamadosDB)
        {
            var def = LOGROS_DEF.FirstOrDefault(d => d.Id == id);
            if (def is null) return;

            if (!estado.ContainsKey(id)) estado[id] = new LogroEntrada();
            var entrada = estado[id];

            if (reclamadosDB.Contains(id))
            {
                entrada.Reclamado = true;
                entrada.Progreso = def.Meta;
                entrada.Completado = true;
                return;
            }

            entrada.Progreso = valor;
            if (entrada.Progreso >= def.Meta && !entrada.Completado)
                entrada.Completado = true;
        }

        private LogroEntrada InitEntrada(
            Dictionary<string, LogroEntrada> estado, LogroDef def)
        {
            if (!estado.ContainsKey(def.Id))
                estado[def.Id] = new LogroEntrada();
            return estado[def.Id];
        }

        private Dictionary<string, LogroEntrada> CargarEstado()
        {
            return new();
        }

        private void GuardarEstado(Dictionary<string, LogroEntrada> estado)
        {
            // Estado se recalcula desde BD en cada visita, no se persiste en sesión.
        }

        private LogrosViewModel BuildViewModel(
            Dictionary<string, LogroEntrada> estado,
            HashSet<string> reclamadosDB)
        {
            var vm = new LogrosViewModel
            {
                FechaHoy = DateTime.UtcNow.ToString("dddd, dd 'de' MMMM 'de' yyyy",
                                  new System.Globalization.CultureInfo("es-MX")),
                Diarios = new(),
                Permanentes = new(),
                Completados = new(),
            };

            foreach (var def in LOGROS_DEF)
            {
                if (!estado.ContainsKey(def.Id)) estado[def.Id] = new LogroEntrada();
                var entrada = estado[def.Id];

                if (reclamadosDB.Contains(def.Id))
                {
                    entrada.Reclamado = true;
                    entrada.Completado = true;
                    entrada.Progreso = def.Meta;
                }

                var card = new LogroCard(def, entrada);

                if (def.Tipo == "diario") vm.Diarios.Add(card);
                else vm.Permanentes.Add(card);

                if (entrada.Reclamado) vm.Completados.Add(card);
            }

            vm.TotalDiarios = vm.Diarios.Count;
            vm.CompletadosHoy = vm.Diarios.Count(c => c.Entrada.Completado);
            return vm;
        }

        [HttpPost]
        public IActionResult GuardarOrden([FromBody] List<OrdenTopDto> orden)
        {
            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var librosUsuario = _context.UsuarioLibros
                .Where(u => u.UsuarioId == usuarioId)
                .ToList();

            foreach (var item in orden)
            {
                var libro = librosUsuario.FirstOrDefault(l => l.LibroId == item.LibroId);
                if (libro != null)
                    libro.Posicion = item.Posicion;
            }

            _context.SaveChanges();
            return Ok(new { mensaje = "TOP guardado correctamente" });
        }
    }
}