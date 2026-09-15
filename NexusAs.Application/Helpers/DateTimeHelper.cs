using System;

namespace NexusAs.Application.Helpers
{
    /// <summary>
    /// Helper para obtener la fecha y hora en la zona horaria de Colombia (UTC-5)
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo ColombiaTimeZone = 
            TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); // UTC-5 Colombia

        /// <summary>
        /// Obtiene la fecha y hora actual en la zona horaria de Colombia
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaTimeZone);

        /// <summary>
        /// Obtiene solo la fecha actual en la zona horaria de Colombia
        /// </summary>
        public static DateTime Today => Now.Date;
    }
}
