namespace AURORA.Models
{
    public class Tb_LogroReclamado
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string LogroId { get; set; } = "";
        public DateTime FechaReclamo { get; set; } = DateTime.UtcNow;
        public Tb_Usuario? Usuario { get; set; }
    }

}