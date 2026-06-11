using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AURORA.Models
{
    [Table("Tb_UsuarioLibro")]
    public class Tb_UsuarioLibro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public int LibroId { get; set; }

        [ForeignKey("UsuarioId")]
        public Tb_Usuario Usuario { get; set; }

        [ForeignKey("LibroId")]
        public Tb_Libro Libro { get; set; }

        [Range(0, 100)]
        public int Progreso { get; set; } = 0;

        public int UltimaPagina { get; set; } = 1;

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public DateTime? UltimoAcceso { get; set; }
        public DateTime? UltimoInicioLectura { get; set; }
        public TimeSpan? TiempoLectura { get; set; }

        public int Posicion { get; set; } = 0;
    }
}
