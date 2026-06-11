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
                // 1. Descargar archivo
                var request = new HttpRequestMessage(HttpMethod.Get, urlDescarga);
                request.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                var archivoResponse = await _http.SendAsync(request);

                if (!archivoResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = $"No se pudo descargar ({(int)archivoResponse.StatusCode}). Intenta con otro libro.";
                    return RedirectToAction("Index");
                }

                var archivoBytes = await archivoResponse.Content.ReadAsByteArrayAsync();

                if (archivoBytes.Length < 2048)
                {
                    TempData["Error"] = "El archivo descargado est� vac�o. Intenta con otro libro.";
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
                        // ? Extraer el ID del libro de la URL y construir URL correcta
                        // URL original: https://www.gutenberg.org/ebooks/66263.epub3.images
                        // URL TXT:      https://www.gutenberg.org/cache/epub/66263/pg66263.txt
                        try
                        {
                            // Extraer n�mero de libro de la URL
                            var match = System.Text.RegularExpressions.Regex.Match(
                                urlDescarga, @"/ebooks/(\d+)");

                            string urlTxt;
                            if (match.Success)
                            {
                                var libroId = match.Groups[1].Value;
                                urlTxt = $"https://www.gutenberg.org/cache/epub/{libroId}/pg{libroId}.txt";
                            }
                            else
                            {
                                TempData["Error"] = "No se pudo convertir este libro. Intenta con otro resultado.";
                                return RedirectToAction("Index");
                            }

                            var txtRequest = new HttpRequestMessage(HttpMethod.Get, urlTxt);
                            txtRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                            var txtResponse = await _http.SendAsync(txtRequest);

                            if (txtResponse.IsSuccessStatusCode)
                            {
                                var txtBytes = await txtResponse.Content.ReadAsByteArrayAsync();
                                archivoBytes = await _epubConverter.ConvertirTxtAPdfAsync(
                                    txtBytes, titulo, autor);
                                formato = "pdf";
                            }
                            else
                            {
                                TempData["Error"] = $"No se pudo obtener el texto ({(int)txtResponse.StatusCode}). Intenta con otro resultado.";
                                return RedirectToAction("Index");
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = $"Error: {ex.GetType().Name} - {ex.Message} - Inner: {ex.InnerException?.Message} - {ex.InnerException?.InnerException?.Message}";
                            return RedirectToAction("Index");
                        }
                    }
                }
                // 3. Verificar que sea PDF v�lido
                bool esPdf = archivoBytes.Length > 4 &&
                             archivoBytes[0] == 0x25 &&
                             archivoBytes[1] == 0x50 &&
                             archivoBytes[2] == 0x44 &&
                             archivoBytes[3] == 0x46;

                if (!esPdf)
                {
                    TempData["Error"] = "El archivo no es un PDF v�lido tras la conversi�n.";
                    return RedirectToAction("Index");
                }

                // 4. Extraer texto y guardar PDF en PostgreSQL
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
                    PdfBytes = archivoBytes      // guardado en PostgreSQL
                };

                _context.Libros.Add(libro);
                await _context.SaveChangesAsync();

                // 7. Vincular al usuario
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

                TempData["Exito"] = $"? '{titulo}' agregado con {totalPaginas} p�ginas.";
                return RedirectToAction("Biblioteca", "Lector");
            }
            catch (TaskCanceledException)
            {
                TempData["Error"] = "La descarga tard� demasiado. Intenta con otro libro.";
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