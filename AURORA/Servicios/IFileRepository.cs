using System.IO;
using System.Threading.Tasks;

namespace AURORA.Servicios
{
    public interface IFileRepository
    {
        /// <summary>
        /// Sube un archivo al repositorio y devuelve la URL pública.
        /// </summary>
        Task<string> UploadFileAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Elimina un archivo del repositorio usando su URL o nombre.
        /// </summary>
        Task DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Obtiene la URL pública de un archivo en el bucket.
        /// </summary>
        string GetFileUrl(string fileName);
    }
}
