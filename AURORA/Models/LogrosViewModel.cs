    namespace AURORA.Models
    {
        public record LogroDef(
            string Id,
            string Tipo,
            string Icono,
            string Nombre,
            string Desc,
            int Meta
        );

        public class LogroEntrada
        {
            public int Progreso { get; set; } = 0;
            public bool Completado { get; set; } = false;
            public bool Reclamado { get; set; } = false;
            public DateTime? FechaReclamo { get; set; } = null;
        }

        public class LogroCard
        {
            public LogroDef Def { get; }
            public LogroEntrada Entrada { get; }
            public double Pct => Math.Min((double)Entrada.Progreso / Def.Meta, 1.0) * 100;

            public LogroCard(LogroDef def, LogroEntrada entrada)
            {
                Def = def;
                Entrada = entrada;
            }
        }

        public class LogrosViewModel
        {
            public string FechaHoy { get; set; } = "";
            public List<LogroCard> Diarios { get; set; } = new();
            public List<LogroCard> Permanentes { get; set; } = new();
            public List<LogroCard> Completados { get; set; } = new();
            public int TotalDiarios { get; set; }
            public int CompletadosHoy { get; set; }
        }
    }