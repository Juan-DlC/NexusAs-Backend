using Microsoft.AspNetCore.Hosting;

namespace NexusAs.Infrastructure.Services
{
    public class ReportStyleHelper
    {
        // Paleta de colores profesional en escala de grises + acento del logo
        public string ColorPrincipal { get; } = "#2D3748";      // Gris muy oscuro (casi negro)
        public string ColorAcento { get; } = "#C8956C";          // Dorado/beige de la S del logo
        public string ColorTextoSecundario { get; } = "#6B7280"; // Gris medio
        public string ColorExito { get; } = "#A3D9A5";           // Verde pastel
        public string ColorAlerta { get; } = "#F6A4A4";          // Rojo pastel
        public string ColorBordes { get; } = "#D1D5DB";          // Gris claro para bordes
        public string ColorFilasAlternas { get; } = "#F9FAFB";   // Gris muy claro para filas
        public string ColorFondoSuave { get; } = "#F9FAFB";      // Alias para compatibilidad con otros PDFs
        public string ColorTablaHeader { get; } = "#4B5563";     // Gris oscuro para headers de tabla
        public string LogoPath { get; }

        public ReportStyleHelper(IWebHostEnvironment env)
        {
            LogoPath = Path.Combine(env.WebRootPath, "images", "Logo_4K.png");
        }
    }
}
