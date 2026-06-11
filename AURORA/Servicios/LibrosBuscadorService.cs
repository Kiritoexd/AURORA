using System.Text.Json;
using System.Text;

namespace AURORA.Servicios
{
    public class LibrosBuscadorService
    {
        private readonly HttpClient _http;

        public LibrosBuscadorService(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(10); // Railway puede tener latencia alta
        }

        private static string QuitarTildes(string texto)
        {
            var normalized = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Gutendex a veces devuelve URLs del tipo:
        ///   https://www.gutenberg.org/ebooks/1342.epub3.images   ← redirige a HTML, da 404 al descargar
        ///   https://www.gutenberg.org/ebooks/1342.epub.images    ← igual
        /// Las reemplazamos por la URL directa del cache:
        ///   https://www.gutenberg.org/cache/epub/1342/pg1342.epub
        /// Para PDF, Gutendex ya suele dar la URL del cache directamente, pero también lo normalizamos.
        /// </summary>
        private static string NormalizarUrlGutenberg(string url, string formato, out string formatoFinal)
        {
            formatoFinal = formato;

            // Detectar patrón /ebooks/{id}.epub* o /ebooks/{id}.pdf
            var match = System.Text.RegularExpressions.Regex.Match(
                url, @"gutenberg\.org/ebooks/(\d+)\.(epub|pdf)");

            if (match.Success)
            {
                var libroId = match.Groups[1].Value;
                var ext = match.Groups[2].Value;

                if (ext == "epub")
                {
                    formatoFinal = "epub";
                    return $"https://www.gutenberg.org/cache/epub/{libroId}/pg{libroId}.epub";
                }
                if (ext == "pdf")
                {
                    formatoFinal = "pdf";
                    return $"https://www.gutenberg.org/cache/epub/{libroId}/pg{libroId}-pdf.pdf";
                }
            }

            // Si ya es una URL del cache, dejarla como está
            return url;
        }

        public async Task<List<LibroExterno>> BuscarEnTodasAsync(string query)
        {
            var querySinTilde = QuitarTildes(query);

            var tareas = await Task.WhenAll(
                BuscarGutendex(query, soloEspanol: true),
                BuscarGutendex(querySinTilde, soloEspanol: true),
                BuscarGutendex(query, soloEspanol: false),
                BuscarGutendex(querySinTilde, soloEspanol: false)
            );

            var resultados = tareas
                .SelectMany(x => x)
                .GroupBy(l => l.Titulo.ToLower().Trim())
                .Select(g => g.First())
                .OrderByDescending(l => l.Idioma == "es")
                .ThenByDescending(l => l.Formato == "pdf")
                .Take(24)
                .ToList();

            return resultados;
        }

        private async Task<List<LibroExterno>> BuscarGutendex(string query, bool soloEspanol)
        {
            try
            {
                var lang = soloEspanol ? "&languages=es" : "";
                var url = $"https://gutendex.com/books/?search={Uri.EscapeDataString(query)}{lang}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                var libros = new List<LibroExterno>();

                foreach (var item in root.GetProperty("results").EnumerateArray())
                {
                    var libroId = item.GetProperty("id").GetInt32();
                    string? urlDescarga = null;
                    string formato = "pdf";

                    if (item.TryGetProperty("formats", out var formats))
                    {
                        foreach (var fmt in formats.EnumerateObject())
                        {
                            var val = fmt.Value.GetString() ?? "";
                            if (val.EndsWith(".png") || val.EndsWith(".jpg") ||
                                val.EndsWith(".jpeg") || val.Contains(".htm")) continue;

                            if (fmt.Name == "application/pdf" && urlDescarga == null)
                            { urlDescarga = val; formato = "pdf"; }
                            else if (fmt.Name == "application/epub+zip" && urlDescarga == null)
                            { urlDescarga = val; formato = "epub"; }
                        }
                    }

                    if (string.IsNullOrEmpty(urlDescarga)) continue;

                    // 🔑 FIX: Normalizar la URL antes de guardarla
                    urlDescarga = NormalizarUrlGutenberg(urlDescarga, formato, out formato);

                    var titulo = item.GetProperty("title").GetString() ?? "Sin título";
                    var autores = item.GetProperty("authors").EnumerateArray()
                        .Select(a => a.GetProperty("name").GetString() ?? "")
                        .Where(a => !string.IsNullOrEmpty(a)).Take(2).ToList();

                    var genero = "General";
                    if (item.TryGetProperty("subjects", out var subjects) && subjects.GetArrayLength() > 0)
                        genero = (subjects[0].GetString() ?? "General").Split(" -- ")[0].Split(",")[0].Trim();
                    if (genero.Length > 50) genero = genero[..50];

                    var idioma = "en";
                    if (item.TryGetProperty("languages", out var langs) && langs.GetArrayLength() > 0)
                        idioma = langs[0].GetString() ?? "en";

                    libros.Add(new LibroExterno
                    {
                        Titulo = titulo,
                        Autor = autores.Any() ? string.Join(", ", autores) : "Desconocido",
                        UrlDescarga = urlDescarga,
                        Formato = formato,
                        Fuente = "Gutenberg",
                        Genero = genero,
                        Idioma = idioma,
                        Portada = $"https://www.gutenberg.org/cache/epub/{libroId}/pg{libroId}.cover.medium.jpg"
                    });
                }

                return libros;
            }
            catch { return new(); }
        }
    }

    public class LibroExterno
    {
        public string Titulo { get; set; } = "";
        public string Autor { get; set; } = "";
        public string UrlDescarga { get; set; } = "";
        public string Formato { get; set; } = "";
        public string Fuente { get; set; } = "";
        public string Genero { get; set; } = "";
        public string? Portada { get; set; }
        public string Idioma { get; set; } = "en";
    }
}