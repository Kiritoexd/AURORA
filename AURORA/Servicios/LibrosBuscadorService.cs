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
            _http.Timeout = TimeSpan.FromSeconds(30);
            _http.DefaultRequestHeaders.Add("User-Agent", "AURORA-App/1.0 (contacto@aurora.app)");
            _logger = logger;
        }

        private static readonly Dictionary<string, string> _generoEs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fiction"] = "Ficción",
            ["Novel"] = "Novela",
            ["Short stories"] = "Cuentos",
            ["Science fiction"] = "Ciencia ficción",
            ["Fantasy fiction"] = "Fantasía",
            ["Fantasy"] = "Fantasía",
            ["Horror"] = "Terror",
            ["Mystery fiction"] = "Misterio",
            ["Mystery"] = "Misterio",
            ["Adventure stories"] = "Aventura",
            ["Adventure"] = "Aventura",
            ["Romance"] = "Romance",
            ["Historical fiction"] = "Ficción histórica",
            ["Fairy tales"] = "Cuentos de hadas",
            ["Mythology"] = "Mitología",
            ["Satire"] = "Sátira",
            ["Humor"] = "Humor",
            ["Drama"] = "Teatro",
            ["Poetry"] = "Poesía",
            ["History"] = "Historia",
            ["Biography"] = "Biografía",
            ["Autobiography"] = "Autobiografía",
            ["Essays"] = "Ensayo",
            ["Literature"] = "Literatura",
            ["Philosophy"] = "Filosofía",
            ["Science"] = "Ciencia",
            ["Geography"] = "Geografía",
            ["Travel"] = "Viajes",
            ["Religion"] = "Religión",
            ["Politics"] = "Política",
            ["Psychology"] = "Psicología",
            ["Education"] = "Educación",
            ["Mathematics"] = "Matemáticas",
            ["Medicine"] = "Medicina",
            ["Art"] = "Arte",
            ["Music"] = "Música",
            ["Technology"] = "Tecnología",
        };

        private static string TraducirGenero(string generoEn)
        {
            if (string.IsNullOrWhiteSpace(generoEn)) return "General";
            if (_generoEs.TryGetValue(generoEn.Trim(), out var traduccion))
                return traduccion;
            foreach (var kv in _generoEs)
                if (generoEn.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            return generoEn;
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

        public async Task<List<LibroExterno>> BuscarEnTodasAsync(string query)
        {
            var querySinTilde = QuitarTildes(query);

            var tareas = await Task.WhenAll(
                BuscarGutendex(query),
                BuscarGutendex(querySinTilde)
            );

            var resultados = tareas
                .SelectMany(x => x)
                .GroupBy(l => l.Titulo.ToLower().Trim())
                .Select(g => g.First())
                .Take(24)
                .ToList();

            _logger.LogInformation("Búsqueda '{Query}' → {Count} resultados", query, resultados.Count);
            return resultados;
        }

        private async Task<List<LibroExterno>> BuscarGutendex(string query)
        {
            try
            {
                // Gutendex es la API oficial de Gutenberg — no bloquea servidores cloud
                var url = $"https://gutendex.com/books?search={Uri.EscapeDataString(query)}&languages=en,es";

                _logger.LogInformation("Consultando Gutendex: {Url}", url);

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gutendex respondió {Status}", response.StatusCode);
                    return new();
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                if (!root.TryGetProperty("results", out var results))
                    return new();

                var libros = new List<LibroExterno>();

                foreach (var item in results.EnumerateArray())
                {
                    var titulo = item.TryGetProperty("title", out var t)
                        ? t.GetString() ?? "Sin título" : "Sin título";

                    var autor = "Desconocido";
                    if (item.TryGetProperty("authors", out var autores) && autores.GetArrayLength() > 0)
                    {
                        var primerAutor = autores[0];
                        if (primerAutor.TryGetProperty("name", out var nombre))
                            autor = nombre.GetString() ?? "Desconocido";
                    }

                    // Obtener URL de descarga TXT (siempre disponible en Gutenberg)
                    string? urlDescarga = null;
                    string formato = "txt";

                    if (item.TryGetProperty("formats", out var formats))
                    {
                        // Preferir TXT UTF-8 (más confiable desde cloud)
                        foreach (var f in formats.EnumerateObject())
                        {
                            if (f.Name.Contains("text/plain") && f.Value.GetString()?.EndsWith(".txt") == true)
                            {
                                urlDescarga = f.Value.GetString();
                                formato = "txt";
                                break;
                            }
                        }

                        // Fallback: cualquier TXT
                        if (urlDescarga == null)
                        {
                            foreach (var f in formats.EnumerateObject())
                            {
                                if (f.Name.Contains("text/plain"))
                                {
                                    urlDescarga = f.Value.GetString();
                                    formato = "txt";
                                    break;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(urlDescarga)) continue;

                    // Portada
                    string? portada = null;
                    if (item.TryGetProperty("formats", out var fmts2))
                    {
                        foreach (var f in fmts2.EnumerateObject())
                        {
                            if (f.Name.Contains("image/jpeg"))
                            {
                                portada = f.Value.GetString();
                                break;
                            }
                        }
                    }

                    // Género desde bookshelves
                    var genero = "General";
                    if (item.TryGetProperty("bookshelves", out var shelves) && shelves.GetArrayLength() > 0)
                    {
                        var shelf = shelves[0].GetString() ?? "General";
                        // Limpiar prefijos como "Browsing: " o "Movie Books"
                        shelf = shelf.Replace("Browsing: ", "").Replace("Movie Books", "General").Trim();
                        genero = TraducirGenero(shelf);
                    }
                    else if (item.TryGetProperty("subjects", out var subjects) && subjects.GetArrayLength() > 0)
                    {
                        var subj = subjects[0].GetString() ?? "General";
                        if (subj.Contains(" -- ")) subj = subj.Split(" -- ")[0].Trim();
                        genero = TraducirGenero(subj);
                    }

                    if (genero.Length > 50) genero = genero[..50];

                    // Idioma
                    var idioma = "en";
                    if (item.TryGetProperty("languages", out var langs) && langs.GetArrayLength() > 0)
                        idioma = langs[0].GetString() ?? "en";

                    libros.Add(new LibroExterno
                    {
                        Titulo = titulo,
                        Autor = autor,
                        UrlDescarga = urlDescarga,
                        Formato = formato,
                        Fuente = "Project Gutenberg",
                        Genero = genero,
                        Idioma = idioma,
                        Portada = portada
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
        public string? IaId { get; set; }
    }
}