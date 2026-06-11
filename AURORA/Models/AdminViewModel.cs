using AURORA.Models;

namespace AURORA.Models
{
    // ── Vista principal: lista de usuarios ──
    public class AdminUsuarioResumen
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public string? FotoUrl { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool EstaActivo { get; set; }   // false = dado de baja

        // Estadísticas rápidas para la tabla
        public int TotalLibros { get; set; }
        public int LibrosTerminados { get; set; }
        public int LibrosLeyendo { get; set; }
        public int DiasRacha { get; set; }
        public TimeSpan TiempoTotal { get; set; }
    }

    public class AdminIndexViewModel
    {
        public List<AdminUsuarioResumen> Usuarios { get; set; } = new();
        public int TotalActivos => Usuarios.Count(u => u.EstaActivo);
        public int TotalBaja => Usuarios.Count(u => !u.EstaActivo);
        public int TotalLibros => Usuarios.Sum(u => u.TotalLibros);
    }

    // ── Detalle de un usuario ──
    public class AdminDetalleUsuarioViewModel
    {
        public Tb_Usuario Usuario { get; set; }
        public int DiasRacha { get; set; }
        public int MetaDias { get; set; }

        // Libros
        public List<Tb_UsuarioLibro> LibrosLeyendo { get; set; } = new();
        public List<Tb_UsuarioLibro> LibrosPendientes { get; set; } = new();
        public List<Tb_UsuarioLibro> LibrosTerminados { get; set; } = new();

        public int TotalLibros => LibrosLeyendo.Count + LibrosPendientes.Count + LibrosTerminados.Count;

        // Tiempo
        public TimeSpan TiempoTotal { get; set; }
        public string TiempoFormateado =>
            TiempoTotal.TotalHours >= 1
                ? $"{(int)TiempoTotal.TotalHours}h {TiempoTotal.Minutes}min"
                : $"{TiempoTotal.Minutes}min";

        // Género favorito
        public string? GeneroFavorito { get; set; }
    }
}
