using System.Collections.Generic;

namespace AURORA.ViewModels
{
    public class EstadisticasViewModel
    {
        // Ya existentes
        public int LibrosPendientes { get; set; }
        public int LibrosLeyendo { get; set; }
        public int LibrosTerminados { get; set; }

        public List<string> Generos { get; set; }
        public List<int> CantidadPorGenero { get; set; }

        public List<string> Meses { get; set; }
        public List<int> MinutosPorMes { get; set; }

        // 🔹 Nuevas propiedades para las gráficas adicionales
        public List<string> Anios { get; set; } = new List<string>();
        public List<int> LibrosPorAnio { get; set; } = new List<int>();

        public List<string> Sesiones { get; set; } = new List<string>();
        public List<int> PromedioMinutos { get; set; } = new List<int>();
       

    }
}
