using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AURORA.Servicios;

namespace AURORA.Controllers
{
    public class RecomendacionesController : Controller
    {
        private readonly GroqService _groqService;

        public RecomendacionesController(GroqService groqService)
        {
            _groqService = groqService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Lector/Recomendaciones.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Recomendar(string genero)
        {
            var semilla = new Random().Next(1, 99999);

            string prompt = $@"Recomienda exactamente 5 libros del género {genero}.
Variación #{semilla} — los libros DEBEN ser diferentes a cualquier lista anterior.
Evita siempre los mismos títulos populares. Explora libros menos conocidos también.
Responde ÚNICAMENTE con un array JSON válido, sin texto antes ni después, sin bloques de código markdown:
[{{""titulo"":""..."",""autor"":""..."",""sinopsis"":""...""}}]";

            var respuesta = await _groqService.GetCompletionAsync(prompt);

            // Manejo de error de servicio
            if (respuesta.StartsWith("Por el momento") || respuesta.StartsWith("Servicio"))
            {
                ViewBag.Error = respuesta;
                ViewBag.GeneroSeleccionado = genero;
                return View("~/Views/Lector/Recomendaciones.cshtml", new List<LibroRecomendado>());
            }

            try
            {
                // Limpiar posibles bloques markdown que devuelva el modelo
                var json = respuesta
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var libros = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LibroRecomendado>>(json);

                ViewBag.GeneroSeleccionado = genero;
                return View("~/Views/Lector/Recomendaciones.cshtml", libros);
            }
            catch
            {
                ViewBag.Error = "No se pudo procesar la respuesta. Intenta de nuevo.";
                ViewBag.GeneroSeleccionado = genero;
                return View("~/Views/Lector/Recomendaciones.cshtml", new List<LibroRecomendado>());
            }
        }
    }

    public class LibroRecomendado
    {
        public string titulo { get; set; }
        public string autor { get; set; }
        public string sinopsis { get; set; }
    }
}