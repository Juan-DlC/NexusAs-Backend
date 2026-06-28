using Microsoft.AspNetCore.Hosting;

namespace NexusAs.Infrastructure.Services
{
    public class ReportStyleHelper
    {
        public string ColorPrincipal { get; } = "#4A4A4A";
        public string ColorAcento { get; } = "#C8956C";
        public string ColorFondoSuave { get; } = "#F5EDE8";
        public string ColorExito { get; } = "#5C8A6B";
        public string ColorAlerta { get; } = "#C0392B";
        public string LogoPath { get; }

        public ReportStyleHelper(IWebHostEnvironment env)
        {
            LogoPath = Path.Combine(env.WebRootPath, "images", "Logo_4K.png");
        }
    }
}
