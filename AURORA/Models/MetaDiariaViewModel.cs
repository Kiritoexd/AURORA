using Microsoft.AspNetCore.Mvc;

namespace AURORA.Models
{
    public class MetaDiariaViewModel
    {
        public string Nombre { get; set; }
        public int Progreso { get; set; } // porcentaje 0-100
        public bool Completada => Progreso >= 100;
    }
}


