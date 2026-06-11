using VersOne.Epub;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using System.Text.RegularExpressions;

namespace AURORA.Servicios
{
    public class EpubConverterService
    {
        public async Task<byte[]> ConvertirEpubAPdfAsync(byte[] epubBytes)
        {
            using var epubStream = new MemoryStream(epubBytes);
            var book = await EpubReader.ReadBookAsync(epubStream);

            using var pdfStream = new MemoryStream();
            var writer = new PdfWriter(pdfStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            var font = PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            document.SetFont(font).SetFontSize(11).SetMargins(60, 60, 60, 60);

            // ── Título ────────────────────────────────────────────
            document.Add(
                new Paragraph(SanitizarTexto(book.Title ?? "Sin título"))
                    .SetFont(fontBold).SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

            // ── Autor ─────────────────────────────────────────────
            if (!string.IsNullOrEmpty(book.Author))
                document.Add(
                    new Paragraph(SanitizarTexto(book.Author))
                        .SetFont(font).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(30));

            // ── Capítulos ─────────────────────────────────────────
            foreach (var chapter in book.ReadingOrder)
            {
                try
                {
                    var htmlContent = chapter.Content ?? "";
                    var texto = LimpiarHtml(htmlContent);
                    if (string.IsNullOrWhiteSpace(texto)) continue;

                    var tituloCapitulo = ExtraerTituloHtml(htmlContent);
                    if (!string.IsNullOrEmpty(tituloCapitulo))
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                        document.Add(
                            new Paragraph(SanitizarTexto(tituloCapitulo))
                                .SetFont(fontBold).SetFontSize(15)
                                .SetMarginBottom(15));
                    }

                    foreach (var parrafo in texto.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var limpio = SanitizarTexto(parrafo.Trim());
                        if (limpio.Length < 2) continue;
                        document.Add(
                            new Paragraph(limpio)
                                .SetFont(font).SetMarginBottom(5)
                                .SetTextAlignment(TextAlignment.JUSTIFIED));
                    }
                }
                catch { continue; }
            }

            document.Close();
            return pdfStream.ToArray();
        }

        public async Task<byte[]> ConvertirTxtAPdfAsync(byte[] txtBytes, string titulo, string autor)
        {
            string texto;
            try { texto = System.Text.Encoding.UTF8.GetString(txtBytes); }
            catch { texto = System.Text.Encoding.Latin1.GetString(txtBytes); }

            using var pdfStream = new MemoryStream();
            var writer = new PdfWriter(pdfStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            var font = PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            document.SetFont(font).SetFontSize(11).SetMargins(60, 60, 60, 60);

            // ── Título ────────────────────────────────────────────
            document.Add(new Paragraph(SanitizarTexto(titulo))
                .SetFont(fontBold).SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // ── Autor ─────────────────────────────────────────────
            if (!string.IsNullOrEmpty(autor))
                document.Add(new Paragraph(SanitizarTexto(autor))
                    .SetFont(font).SetFontSize(13)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(30));

            // ── Saltar encabezado de Gutenberg ────────────────────
            var inicio = texto.IndexOf("*** START", StringComparison.OrdinalIgnoreCase);
            if (inicio > 0)
            {
                var finLinea = texto.IndexOf('\n', inicio);
                texto = finLinea > 0 ? texto[(finLinea + 1)..] : texto[inicio..];
            }

            // ── Saltar pie de Gutenberg ───────────────────────────
            var fin = texto.IndexOf("*** END", StringComparison.OrdinalIgnoreCase);
            if (fin > 0) texto = texto[..fin];

            // ── Párrafos ──────────────────────────────────────────
            var parrafos = texto.Split(
                new[] { "\r\n\r\n", "\n\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var parrafo in parrafos)
            {
                var limpio = SanitizarTexto(parrafo
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Trim());

                if (limpio.Length < 2) continue;

                document.Add(new Paragraph(limpio)
                    .SetFont(font).SetMarginBottom(6)
                    .SetTextAlignment(TextAlignment.JUSTIFIED));
            }

            document.Close();
            return await Task.FromResult(pdfStream.ToArray());
        }

        // ── Helpers ───────────────────────────────────────────────

        private string ExtraerTituloHtml(string html)
        {
            var match = Regex.Match(html,
                @"<h[1-3][^>]*>(.*?)</h[1-3]>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return "";
            return Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "").Trim();
        }

        private string LimpiarHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</p>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</h[1-6]>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<[^>]+>", "");
            html = html.Replace("&amp;", "&")
                       .Replace("&lt;", "<")
                       .Replace("&gt;", ">")
                       .Replace("&quot;", "\"")
                       .Replace("&apos;", "'")
                       .Replace("&nbsp;", " ")
                       .Replace("&#160;", " ");
            html = Regex.Replace(html, @" {2,}", " ");
            html = Regex.Replace(html, @"\n{3,}", "\n\n");
            return html.Trim();
        }

        private string LimpiarTexto(string texto) => SanitizarTexto(texto);

        private string SanitizarTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            var sb = new System.Text.StringBuilder(texto.Length);
            foreach (var c in texto)
            {
                if (c < 0x0020 && c != '\n' && c != '\r' && c != '\t') continue;
                if (c <= 0x00FF) { sb.Append(c); continue; }
                sb.Append(c switch
                {
                    '\u2018' or '\u2019' => '\'',
                    '\u201C' or '\u201D' => '"',
                    '\u2013' => '-',
                    '\u2014' => '-',
                    '\u2026' => '.',
                    '\u00AB' or '\u00BB' => '"',
                    _ => '?'
                });
            }
            return sb.ToString().Trim();
        }
    }
}