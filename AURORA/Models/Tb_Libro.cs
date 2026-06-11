public class Tb_Libro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public int Año { get; set; }
    public string Editorial { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public int Paginas { get; set; }

    public string? PortadaUrl { get; set; }
    public string? RutaPdf { get; set; }
    public string? ContenidoTexto { get; set; }

    // Bytes del PDF guardados en PostgreSQL
    public byte[]? PdfBytes { get; set; }
}
