using Microsoft.AspNetCore.Mvc;
using AURORA.Servicios;
using AURORA.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AURORA.Controllers
{
    public class GroqController : Controller
    {
        private readonly GroqService _groqService;

        public GroqController(GroqService groqService)
        {
            _groqService = groqService;
        }

        [HttpGet]
        public IActionResult IA()
        {
            return View();
        }

        // ── Endpoint del chat con historial ──────────────────
        [HttpPost]
        public async Task<IActionResult> ChatApi([FromBody] ChatApiRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewMessage))
                return Json(new { error = "Mensaje vacío." });

            var reply = await _groqService.GetCompletionWithHistoryAsync(
                request.History ?? new List<ChatMessage>(),
                request.NewMessage
            );

            return Json(new { content = reply });
        }
    }

    public class ChatApiRequest
    {
        public List<ChatMessage>? History { get; set; }
        public string NewMessage { get; set; } = "";
    }
}