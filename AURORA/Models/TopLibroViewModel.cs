using Microsoft.AspNetCore.Mvc;

namespace AURORA.Models
{
    public class TopLibroViewModel
    {
        public int LibroId { get; set; }
        public string Titulo { get; set; } = "";
        public string Autor { get; set; } = "";
        public string Genero { get; set; } = "";
        public int Progreso { get; set; }
        public int Posicion { get; set; }
    }
}