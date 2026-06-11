namespace AURORA.Models
{
    public class PerfilViewModel
    {
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string Email { get; set; }
        public string? FotoUrl { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Para mostrar en la vista
        public string NombreCompleto => $"{Nombres} {ApellidoPaterno} {ApellidoMaterno}";

        //Racha
        public int DiasSeguidos { get; set; }
        public int MetaDias { get; set; } = 30;

        public double RachaPct =>
            MetaDias > 0 ? Math.Min((double)DiasSeguidos / MetaDias * 100, 100) : 0;
    }
}