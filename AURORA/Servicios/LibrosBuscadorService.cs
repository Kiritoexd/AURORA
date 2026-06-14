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

        // Diccionario inglés → español para géneros de Open Library
        private static readonly Dictionary<string, string> _generoEs = new(StringComparer.OrdinalIgnoreCase)
        {
            // Ficción y narrativa
            ["Fiction"] = "Ficción",
            ["Novel"] = "Novela",
            ["Novels"] = "Novela",
            ["Short stories"] = "Cuentos",
            ["Short story"] = "Cuento",
            ["Science fiction"] = "Ciencia ficción",
            ["Fantasy fiction"] = "Fantasía",
            ["Fantasy"] = "Fantasía",
            ["Horror"] = "Terror",
            ["Horror tales"] = "Terror",
            ["Mystery fiction"] = "Misterio",
            ["Mystery"] = "Misterio",
            ["Detective and mystery stories"] = "Misterio",
            ["Adventure stories"] = "Aventura",
            ["Adventure"] = "Aventura",
            ["Romance"] = "Romance",
            ["Love stories"] = "Romance",
            ["Historical fiction"] = "Ficción histórica",
            ["Fairy tales"] = "Cuentos de hadas",
            ["Folklore"] = "Folclore",
            ["Legends"] = "Leyendas",
            ["Mythology"] = "Mitología",
            ["Satire"] = "Sátira",
            ["Humorous stories"] = "Humor",
            ["Humor"] = "Humor",
            ["Comedy"] = "Comedia",
            ["Tragedy"] = "Tragedia",
            // Teatro y poesía
            ["Drama"] = "Teatro",
            ["Plays"] = "Teatro",
            ["Poetry"] = "Poesía",
            ["Poems"] = "Poesía",
            ["Epic poetry"] = "Poesía épica",
            // No ficción
            ["History"] = "Historia",
            ["Biography"] = "Biografía",
            ["Autobiography"] = "Autobiografía",
            ["Autobiography. lcgft"] = "Autobiografía",
            ["Memoirs"] = "Memorias",
            ["Essays"] = "Ensayo",
            ["Literature"] = "Literatura",
            ["Philosophy"] = "Filosofía",
            ["Science"] = "Ciencia",
            ["Natural history"] = "Historia natural",
            ["Geography"] = "Geografía",
            ["Travel"] = "Viajes",
            ["Voyages and travels"] = "Viajes",
            ["Religion"] = "Religión",
            ["Theology"] = "Teología",
            ["Politics"] = "Política",
            ["Politics and government"] = "Política",
            ["Political science"] = "Ciencias políticas",
            ["Economics"] = "Economía",
            ["Sociology"] = "Sociología",
            ["Psychology"] = "Psicología",
            ["Education"] = "Educación",
            ["Mathematics"] = "Matemáticas",
            ["Physics"] = "Física",
            ["Chemistry"] = "Química",
            ["Medicine"] = "Medicina",
            ["Law"] = "Derecho",
            ["Art"] = "Arte",
            ["Music"] = "Música",
            ["Architecture"] = "Arquitectura",
            ["Cooking"] = "Cocina",
            ["Sports"] = "Deportes",
            ["Technology"] = "Tecnología",
            ["Accessible book"] = "General",
            ["Protected DAISY"] = "General",
        };

        private static string TraducirGenero(string generoEn)
        {
            if (string.IsNullOrWhiteSpace(generoEn)) return "General";
            // Buscar coincidencia exacta primero
            if (_generoEs.TryGetValue(generoEn.Trim(), out var traduccion))
                return traduccion;
            // Buscar si el género empieza con alguna clave conocida
            foreach (var kv in _generoEs)
                if (generoEn.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            // Si no se encontró traducción, devolver el original
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
                BuscarOpenLibrary(query),
                BuscarOpenLibrary(querySinTilde)
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

        private async Task<List<LibroExterno>> BuscarOpenLibrary(string query)
        {
            try
            {
                var url = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}&has_fulltext=true&limit=20&fields=key,title,author_name,subject,language,ia,cover_i";

                _logger.LogInformation("Consultando Open Library: {Url}", url);

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Open Library respondió {Status} para '{Query}'", response.StatusCode, query);
                    return new();
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                if (!root.TryGetProperty("docs", out var docs))
                    return new();

                var libros = new List<LibroExterno>();

                foreach (var item in docs.EnumerateArray())
                {
                    // Necesitamos el ID de Internet Archive para poder descargar
                    if (!item.TryGetProperty("ia", out var iaArray) || iaArray.GetArrayLength() == 0)
                        continue;

                    var iaId = iaArray[0].GetString();
                    if (string.IsNullOrEmpty(iaId)) continue;

                    var titulo = item.TryGetProperty("title", out var t) ? t.GetString() ?? "Sin título" : "Sin título";

                    var autor = "Desconocido";
                    if (item.TryGetProperty("author_name", out var autores) && autores.GetArrayLength() > 0)
                        autor = autores[0].GetString() ?? "Desconocido";

                    var genero = "General";
                    if (item.TryGetProperty("subject", out var subjects) && subjects.GetArrayLength() > 0)
                    {
                        // Open Library devuelve subjects como "Don Quixote (Cervantes...)" o "Fiction -- Spain"
                        // Buscamos el primer subject que parezca un género real, no un título/autor
                        var generosLimpios = new[] { "Fiction", "Novel", "Drama", "Poetry", "History",
                            "Science", "Philosophy", "Romance", "Adventure", "Fantasy", "Mystery",
                            "Biography", "Essays", "Literature", "Horror", "Novela", "Cuento",
                            "Poesía", "Teatro", "Historia", "Ciencia", "Filosofía" };

                        string? encontrado = null;
                        foreach (var subj in subjects.EnumerateArray())
                        {
                            var s = subj.GetString() ?? "";
                            // Ignorar si contiene paréntesis (suele ser "Título (Autor)")
                            if (s.Contains('(')) continue;
                            // Ignorar si es muy largo o tiene --
                            if (s.Length > 40 || s.Contains(" -- ")) continue;
                            encontrado = s;
                            break;
                        }

                        genero = TraducirGenero(encontrado ?? "General");
                        if (genero.Length > 50) genero = genero[..50];
                    }

                    var idioma = "en";
                    if (item.TryGetProperty("language", out var langs) && langs.GetArrayLength() > 0)
                        idioma = langs[0].GetString() ?? "en";

                    // Descarga directa desde Internet Archive
                    var urlDescarga = $"https://archive.org/download/{iaId}/{iaId}.pdf";

                    string? portada = null;
                    if (item.TryGetProperty("cover_i", out var coverId))
                        portada = $"https://covers.openlibrary.org/b/id/{coverId.GetInt32()}-M.jpg";

                    libros.Add(new LibroExterno
                    {
                        Titulo = titulo,
                        Autor = autor,
                        UrlDescarga = urlDescarga,
                        Formato = "pdf",
                        Fuente = "Open Library",
                        Genero = genero,
                        Idioma = idioma,
                        Portada = portada,
                        IaId = iaId
                    });
                }

                _logger.LogInformation("Open Library devolvió {Count} libros para '{Query}'", libros.Count, query);
                return libros;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Timeout al consultar Open Library para '{Query}'", query);
                return new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar Open Library para '{Query}'", query);
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