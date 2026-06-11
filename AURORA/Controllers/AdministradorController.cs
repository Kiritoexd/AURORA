using AURORA.Data;
using AURORA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AURORA.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministradorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdministradorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────
        //  INDEX — lista de todos los usuarios
        // ────────────────────────────────────────
        public async Task<IActionResult> Index(string? filtro)
        {
            var usuarios = await _context.Usuarios.ToListAsync();

            var resúmenes = new List<AdminUsuarioResumen>();

            foreach (var u in usuarios)
            {
                var libs = await _context.UsuarioLibros
                    .Include(ul => ul.Libro)
                    .Where(ul => ul.UsuarioId == u.Id)
                    .ToListAsync();

                var racha = await _context.Rachas
                    .FirstOrDefaultAsync(r => r.UsuarioId == u.Id);

                var tiempoTotal = libs
                    .Where(l => l.TiempoLectura.HasValue)
                    .Aggregate(TimeSpan.Zero, (acc, l) => acc + l.TiempoLectura!.Value);

                bool activo = !string.IsNullOrEmpty(u.Rol);

                resúmenes.Add(new AdminUsuarioResumen
                {
                    Id = u.Id,
                    NombreCompleto = $"{u.Nombres} {u.ApellidoPaterno} {u.ApellidoMaterno}".Trim(),
                    Email = u.Email,
                    Rol = u.Rol ?? "Sin rol",
                    FotoUrl = u.FotoUrl,
                    FechaRegistro = u.FechaRegistro,
                    EstaActivo = activo,
                    TotalLibros = libs.Count,
                    LibrosTerminados = libs.Count(l => l.Progreso == 100),
                    LibrosLeyendo = libs.Count(l => l.Progreso > 0 && l.Progreso < 100),
                    DiasRacha = racha?.DiasConsecutivos ?? 0,
                    TiempoTotal = tiempoTotal
                });
            }

            var vm = new AdminIndexViewModel
            {
                Usuarios = filtro switch
                {
                    "baja" => resúmenes.Where(u => !u.EstaActivo).ToList(),
                    "activo" => resúmenes.Where(u => u.EstaActivo).ToList(),
                    _ => resúmenes
                }
            };

            ViewBag.Filtro = filtro ?? "todos";
            return View(vm);
        }

        // ────────────────────────────────────────
        //  DETALLE — perfil completo de un usuario
        // ────────────────────────────────────────
        public async Task<IActionResult> Detalle(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var libs = await _context.UsuarioLibros
                .Include(ul => ul.Libro)
                .Where(ul => ul.UsuarioId == id)
                .ToListAsync();

            var racha = await _context.Rachas
                .FirstOrDefaultAsync(r => r.UsuarioId == id);

            var tiempoTotal = libs
                .Where(l => l.TiempoLectura.HasValue)
                .Aggregate(TimeSpan.Zero, (acc, l) => acc + l.TiempoLectura!.Value);

            var generoFav = libs
                .Where(l => l.Libro?.Genero != null)
                .GroupBy(l => l.Libro!.Genero)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            var vm = new AdminDetalleUsuarioViewModel
            {
                Usuario = usuario,
                DiasRacha = racha?.DiasConsecutivos ?? 0,
                MetaDias = racha?.MetaDias ?? 30,
                LibrosLeyendo = libs.Where(l => l.Progreso > 0 && l.Progreso < 100).ToList(),
                LibrosPendientes = libs.Where(l => l.Progreso == 0).ToList(),
                LibrosTerminados = libs.Where(l => l.Progreso == 100).ToList(),
                TiempoTotal = tiempoTotal,
                GeneroFavorito = generoFav
            };

            return View(vm);
        }

        // ────────────────────────────────────────
        //  DAR DE BAJA — quita el rol al usuario
        // ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Rol = null;
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"El usuario {usuario.Nombres} fue dado de baja.";
            TempData["Tipo"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        // ────────────────────────────────────────
        //  REACTIVAR — restaura el rol Lector
        // ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Rol = "Lector";
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"El usuario {usuario.Nombres} fue reactivado.";
            TempData["Tipo"] = "success";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLibro(int usuarioId, int libroId)
        {
            var ul = await _context.UsuarioLibros
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.LibroId == libroId);

            if (ul != null)
            {
                _context.UsuarioLibros.Remove(ul);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Detalle", new { id = usuarioId });
        }
    }
}