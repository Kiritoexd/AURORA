using Microsoft.EntityFrameworkCore;
using AURORA.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace AURORA.Data
{
    public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ✅ Tabla para guardar keys en PostgreSQL
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public DbSet<Tb_Usuario> Usuarios { get; set; }
        public DbSet<Tb_Libro> Libros { get; set; }
        public DbSet<Tb_UsuarioLibro> UsuarioLibros { get; set; }
        public DbSet<Tb_Racha> Rachas { get; set; }
        public DbSet<TopLibro> TopLibros { get; set; }
        public DbSet<Tb_LogroReclamado> LogrosReclamados { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Relaciones UsuarioLibro
            modelBuilder.Entity<Tb_UsuarioLibro>()
                .HasOne(ul => ul.Usuario)
                .WithMany()
                .HasForeignKey(ul => ul.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tb_UsuarioLibro>()
                .HasOne(ul => ul.Libro)
                .WithMany()
                .HasForeignKey(ul => ul.LibroId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Relaciones TopLibro
            modelBuilder.Entity<TopLibro>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TopLibro>()
                .HasOne(t => t.Libro)
                .WithMany()
                .HasForeignKey(t => t.LibroId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Configuración adicional para TimeSpan
            // Se guarda como ticks (long) para compatibilidad con SQLite/Postgres
            modelBuilder.Entity<Tb_UsuarioLibro>()
                .Property(ul => ul.TiempoLectura)
                .HasConversion<long>();
            modelBuilder.Entity<Tb_Usuario>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
