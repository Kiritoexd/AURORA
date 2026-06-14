using System.ComponentModel.DataAnnotations.Schema;

namespace AURORA.Models
{
    public class Tb_LogroReclamado
    {
        [Column("id")]
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string LogroId { get; set; } = "";
        public DateTime FechaReclamo { get; set; } = DateTime.UtcNow;
        public Tb_Usuario? Usuario { get; set; }
    }
}