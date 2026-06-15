using AURORA.Data;
using AURORA.Models;
using AURORA.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AURORA.Controllers
{
    [Authorize(Roles = "Lector")]
    public class BuscadorController : Controller
    {
        private readonly LibrosBuscadorService _buscador;
        private readonly IFileRepository _fileRepo;
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _http;
        private readonly EpubConverterService _epubConverter;

        public BuscadorController(
            LibrosBuscadorService buscador,
            IFileRepository fileRepo,
            ApplicationDbContext context,
            IHttpClientFactory httpFactory,
            EpubConverterService epubConverter)
        {
            _buscador = buscador;
            _fileRepo = fileRepo;
            _context = context;
            _http = httpFactory.CreateClient();
            _epubConverter = epubConverter;
        }

        public IActionResult Index() => View(new List<LibroExterno>());

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Buscar(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View("Index", new List<LibroExterno>());

            try
            {
                var resultados = await _buscador.BuscarEnTodasAsync(query);
                ViewBag.Query = query;
                if (!resultados.Any())
                    TempData["Error"] = "No se encontraron resultados. Intenta con otro término.";
                return View("Index", resultados);
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "La búsqueda tardó demasiado. Verifica tu conexión o intenta más tarde.";
                return View("Index", new List<LibroExterno>());
            }
            catch (HttpRequestException ex)
            {
                TempData["Error"] = $"No se pudo conectar al buscador de libros: {ex.Message}";
                return View("Index", new List<LibroExterno>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error inesperado: {ex.Message}";
                return View("Index", new List<LibroExterno>());
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ImportarLibro(
            string titulo, string autor, string urlDescarga,
            string genero, string formato)
        {
            try
            {
                byte[] archivoBytes;

                // 1. Intentar descargar archivo original
                var request = new HttpRequestMessage(HttpMethod.Get, urlDescarga);
                request.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "application/pdf,application/epub+zip,*/*");
                request.Headers.Add("Accept-Language", "es-MX,es;q=0.9,en;q=0.8");
                request.Headers.Add("Referer", "https://www.gutenberg.org/");

                var archivoResponse = await _http.SendAsync(request);

                if (!archivoResponse.IsSuccessStatusCode)
                {
                    // Fallback automático a TXT de Gutenberg
                    var match = System.Text.RegularExpressions.Regex.Match(
                        urlDescarga, @"/ebooks/(\d+)");

                    if (!match.Success)
                    {
                        TempData["Error"] = $"No se pudo descargar ({(int)archivoResponse.StatusCode}). Intenta con otro libro.";
                        return RedirectToAction("Index");
                    }

                    var libroId = match.Groups[1].Value;
                    var urlTxt = $"https://www.gutenberg.org/cache/epub/{libroId}/pg{libroId}.txt";

                    var txtRequest = new HttpRequestMessage(HttpMethod.Get, urlTxt);
                    txtRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                    var txtResponse = await _http.SendAsync(txtRequest);

                    if (!txtResponse.IsSuccessStatusCode)
                    {
                        TempData["Error"] = $"No se pudo descargar el libro ({(int)txtResponse.StatusCode}). Intenta con otro libro.";
                        return RedirectToAction("Index");
                    }

                    var txtBytes = await txtResponse.Content.ReadAsByteArrayAsync();
                    archivoBytes = await _epubConverter.ConvertirTxtAPdfAsync(txtBytes, titulo, autor);
                    formato = "pdf";
                }
                else
                {
                    archivoBytes = await archivoResponse.Content.ReadAsByteArrayAsync();
                }

                if (archivoBytes.Length < 2048)
                {
                    TempData["Error"] = "El archivo descargado está vacío. Intenta con otro libro.";
                    return RedirectToAction("Index");
                }

                // 2. Si es EPUB, convertir a PDF
                if (formato == "epub")
                {
                    try
                    {
                        archivoBytes = await _epubConverter.ConvertirEpubAPdfAsync(archivoBytes);
                        formato = "pdf";
                    }
                    catch
                    {
                        try
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(
                                urlDescarga, @"/ebooks/(\d+)");

                            if (!match.Success)
                            {
                                TempData["Error"] = "No se pudo convertir este libro. Intenta con otro resultado.";
                                return RedirectToAction("Index");
                            }

                            var libroId = match.Groups[1].Value;
                            var urlTxt = $"https://www.gutenberg.org/cache/epub/{libroId}/pg{libroId}.txt";

                            var txtRequest = new HttpRequestMessage(HttpMethod.Get, urlTxt);
                            txtRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                            var txtResponse = await _http.SendAsync(txtRequest);

                            if (!txtResponse.IsSuccessStatusCode)
                            {
                                TempData["Error"] = $"No se pudo obtener el texto ({(int)txtResponse.StatusCode}). Intenta con otro resultado.";
                                return RedirectToAction("Index");
                            }

                            var txtBytes = await txtResponse.Content.ReadAsByteArrayAsync();
                            archivoBytes = await _epubConverter.ConvertirTxtAPdfAsync(txtBytes, titulo, autor);
                            formato = "pdf";
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = $"Error convirtiendo EPUB: {ex.Message}";
                            return RedirectToAction("Index");
                        }
                    }
                }

                // 3. Verificar PDF válido
                bool esPdf = archivoBytes.Length > 4 &&
                             archivoBytes[0] == 0x25 &&
                             archivoBytes[1] == 0x50 &&
                             archivoBytes[2] == 0x44 &&
                             archivoBytes[3] == 0x46;

                if (!esPdf)
                {
                    TempData["Error"] = "El archivo no es un PDF válido tras la conversión.";
                    return RedirectToAction("Index");
                }

                // 4. Extraer texto y páginas
                string contenidoTexto = "";
                int totalPaginas = 0;
                try
                {
                    using var msPig = new MemoryStream(archivoBytes);
                    using var pdfPig = UglyToad.PdfPig.PdfDocument.Open(msPig);
                    totalPaginas = pdfPig.NumberOfPages;
                    var sb = new System.Text.StringBuilder();
                    foreach (var pagina in pdfPig.GetPages())
                        sb.AppendLine(pagina.Text);
                    contenidoTexto = sb.ToString();
                }
                catch { totalPaginas = 0; }

                // 5. Guardar en BD
                var libro = new Tb_Libro
                {
                    Titulo = titulo,
                    Autor = autor,
                    Genero = genero.Length > 50 ? genero[..50] : genero,
                    Editorial = "Dominio Publico",
                    Año = DateTime.UtcNow.Year,
                    Paginas = totalPaginas > 0 ? totalPaginas : 0,
                    RutaPdf = null,
                    ContenidoTexto = contenidoTexto,
                    PdfBytes = archivoBytes
                };

                _context.Libros.Add(libro);
                await _context.SaveChangesAsync();

                // 6. Vincular al usuario
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _context.UsuarioLibros.Add(new Tb_UsuarioLibro
                {
                    UsuarioId = usuarioId,
                    LibroId = libro.Id,
                    Progreso = 0,
                    UltimaPagina = 1,
                    UltimoAcceso = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                TempData["Exito"] = $"✅ '{titulo}' agregado con {totalPaginas} páginas.";
                return RedirectToAction("Biblioteca", "Lector");
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "La descarga tardó demasiado. Intenta con otro libro.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}