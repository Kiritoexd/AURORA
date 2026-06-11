using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AURORA.Models
{
    [Table("Tb_Racha")]
    public class Tb_Racha
    {
        [Key]
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public int DiasConsecutivos { get; set; }

        public DateTime? UltimaLectura { get; set; }

        public int MetaDias { get; set; } = 30;

        [ForeignKey("UsuarioId")]
        public Tb_Usuario Usuario { get; set; }
    }
}

