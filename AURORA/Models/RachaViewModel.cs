namespace AURORA.Models
{
    public class RachaViewModel
    {
        public int DiasSeguidos { get; set; }
        public int MetaDias { get; set; }
        public List<LogroViewModel> Logros { get; set; } = new();
    }

    public class LogroViewModel
    {
        public string Nombre { get; set; }
        public int Dias { get; set; }
        public bool Desbloqueado { get; set; }
    }
}
