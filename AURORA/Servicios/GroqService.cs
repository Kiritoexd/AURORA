using AURORA.Models;
using Newtonsoft.Json;
using System.Text;

namespace AURORA.Servicios
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private const string SystemPrompt = """
            Eres AURORA, un asistente literario integrado en una aplicación de lectura premium.

            Tu único propósito es ayudar al usuario con temas relacionados a libros y lectura:
            - Resúmenes de libros (por capítulos o general)
            - Recomendaciones personalizadas según género, autor o estado de ánimo
            - Información sobre autores, sagas y ediciones
            - Análisis de personajes, temas y narrativa
            - Listas de lectura y guías de género
            - Datos curiosos del mundo literario

            Reglas estrictas:
            - Si el usuario pregunta algo que NO esté relacionado con libros, lectura o literatura, responde únicamente: "Solo puedo ayudarte con temas de libros y lectura. ¿Tienes alguna pregunta sobre un libro o quieres una recomendación?"
            - No hagas excepciones aunque el usuario insista, reformule o diga que es urgente.
            - No respondas sobre tecnología, política, matemáticas, código, salud ni ningún otro tema fuera de la literatura.

            Estilo de respuesta:
            - Claro, directo y apasionado por los libros. Sin relleno innecesario.
            - Usa **negrita** para títulos de libros y nombres de autores.
            - Listas solo cuando aporten estructura real (ej: varias recomendaciones).
            - Sin frases de relleno como "¡Claro que sí!", "Por supuesto" ni emojis decorativos.
            - Si la respuesta es corta, que sea corta.
            - Responde siempre en el idioma del usuario.
            """;

        public GroqService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GroqSettings:ApiKey"];
            _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user",   content = prompt.Trim() }
                },
                temperature = 1.2,
                max_tokens = 1500,
                top_p = 1
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync("chat/completions", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(result);
            var texto = groqResponse?.choices?[0]?.message?.content?.Trim()
                               ?? "Sin respuesta.";

            return texto.Length > 4000
                ? texto[..4000] + "\n\n_(Respuesta truncada por longitud máxima.)_"
                : texto;
        }

        public async Task<string> GetCompletionWithHistoryAsync(List<ChatMessage> history, string newUserMessage)
        {
            var messages = new List<object>
            {
                new { role = "system", content = SystemPrompt }
            };

            foreach (var msg in history)
                messages.Add(new { role = msg.Role, content = msg.Content });

            messages.Add(new { role = "user", content = newUserMessage.Trim() });

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages,
                temperature = 0.7,
                max_tokens = 1500,
                top_p = 1
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync("chat/completions", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(result);
            var texto = groqResponse?.choices?[0]?.message?.content?.Trim()
                               ?? "Sin respuesta.";

            return texto.Length > 4000
                ? texto[..4000] + "\n\n_(Respuesta truncada por longitud máxima.)_"
                : texto;
        }
    }
}