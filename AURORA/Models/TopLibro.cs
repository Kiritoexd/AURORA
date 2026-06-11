using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AURORA.Models
{
    [Table("TopLibro")]
    public class TopLibro
    {
        [Key]
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public int LibroId { get; set; }
        public int Posicion { get; set; }

        [ForeignKey("UsuarioId")]
        public Tb_Usuario Usuario { get; set; }

        [ForeignKey("LibroId")]
        public Tb_Libro Libro { get; set; }
    }
}
