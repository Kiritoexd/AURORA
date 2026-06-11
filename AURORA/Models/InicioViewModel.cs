using AURORA.ViewModels;
using System.Collections.Generic;


namespace AURORA.ViewModels
{
    public class MetaDiariaViewModel
    {

        public string Nombre { get; set; }
        public int Progreso { get; set; } // porcentaje 0-100
        public bool Completada => Progreso >= 100;
    }

    public class InicioViewModel
    {
        public string NombreUsuario { get; set; } = "";
        public int LibrosPendientes { get; set; }
        public int LibrosLeyendo { get; set; }
        public int LibrosTerminados { get; set; }
        public int DiasRacha { get; set; }

        public List<MetaDiariaViewModel> MetasDiarias { get; set; } = new List<MetaDiariaViewModel>();
        public List<string> Habitos { get; set; } = new List<string>();
        public Dictionary<string, int> Tareas { get; set; } = new Dictionary<string, int>();
    }
}




