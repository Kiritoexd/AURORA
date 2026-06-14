using System.Text.Json;
using System.Text;

namespace AURORA.Servicios
{
    public class LibrosBuscadorService
    {
        private readonly HttpClient _http;
        private readonly ILogger<LibrosBuscadorService> _logger;

        public LibrosBuscadorService(HttpClient http, ILogger<LibrosBuscadorService> logger)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(30); // Railway necesita más margen
            _logger = logger;
        }

        // Quita tildes para búsquedas más amplias
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

        // Normaliza URLs raras de Gutenberg a la URL directa del cache
        private static string NormalizarUrlGutenberg(string url, string formato, out string formatoFinal)
        {
            formatoFinal = formato;

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

            return url;
        }

        public async Task<List<LibroExterno>> BuscarEnTodasAsync(string query)
        {
            var querySinTilde = QuitarTildes(query);

            // CORRECCIÓN: ya no filtramos solo español porque Gutendex tiene muy
            // pocos libros en español y el filtro ?languages=es bota casi todo.
            // Hacemos 2 búsquedas: con y sin tildes, sin restricción de idioma.
            var tareas = await Task.WhenAll(
                BuscarGutendex(query),
                BuscarGutendex(querySinTilde)
            );

            var resultados = tareas
                .SelectMany(x => x)
                .GroupBy(l => l.Titulo.ToLower().Trim())
                .Select(g => g.First())
                // Priorizar español primero, luego PDF
                .OrderByDescending(l => l.Idioma == "es")
                .ThenByDescending(l => l.Formato == "pdf")
                .Take(24)
                .ToList();

            _logger.LogInformation("Búsqueda '{Query}' → {Count} resultados", query, resultados.Count);

            return resultados;
        }

        private async Task<List<LibroExterno>> BuscarGutendex(string query)
        {
            try
            {
                // Sin filtro de idioma para obtener más resultados
                var url = $"https://gutendex.com/books/?search={Uri.EscapeDataString(query)}";

                _logger.LogInformation("Consultando Gutendex: {Url}", url);

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gutendex respondió {Status} para query '{Query}'",
                        response.StatusCode, query);
                    return new();
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                var libros = new List<LibroExterno>();

                foreach (var item in root.GetProperty("results").EnumerateArray())
                {
                    var libroId = item.GetProperty("id").GetInt32();
                    string? urlDescarga = null;
                    string formato = "epub";

                    if (item.TryGetProperty("formats", out var formats))
                    {
                        // Prioridad: epub primero (más compatible), luego pdf
                        string? urlEpub = null;
                        string? urlPdf = null;

                        foreach (var fmt in formats.EnumerateObject())
                        {
                            var val = fmt.Value.GetString() ?? "";

                            // Saltar imágenes y HTML
                            if (val.EndsWith(".png") || val.EndsWith(".jpg") ||
                                val.EndsWith(".jpeg") || val.Contains(".htm") ||
                                val.Contains("cover")) continue;

                            if (fmt.Name == "application/epub+zip" && urlEpub == null)
                                urlEpub = val;
                            else if (fmt.Name == "application/pdf" && urlPdf == null)
                                urlPdf = val;
                        }

                        // Preferimos epub pero aceptamos pdf si no hay epub
                        if (urlEpub != null)
                        { urlDescarga = urlEpub; formato = "epub"; }
                        else if (urlPdf != null)
                        { urlDescarga = urlPdf; formato = "pdf"; }
                    }

                    if (string.IsNullOrEmpty(urlDescarga)) continue;

                    // Normalizar URL de Gutenberg
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

                _logger.LogInformation("Gutendex devolvió {Count} libros para '{Query}'", libros.Count, query);
                return libros;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Timeout al consultar Gutendex para '{Query}'", query);
                return new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar Gutendex para '{Query}'", query);
                return new();
            }
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