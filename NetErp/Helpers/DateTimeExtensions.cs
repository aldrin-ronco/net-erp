using System;
using System.Globalization;

namespace NetErp.Helpers
{
    /// <summary>
    /// Métodos de extensión centralizados para formateo de fechas.
    /// Uso: <c>myDate.ToIsoDate()</c>, <c>myDate.ToIsoDatetime()</c>, etc.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Formato ISO 8601 solo fecha: <c>2026-04-08</c>.
        /// Corresponde al scalar <c>IsoDate</c> del schema GraphQL.
        /// </summary>
        public static string ToIsoDate(this DateTime date) =>
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formato ISO 8601 solo fecha desde nullable.
        /// Lanza <see cref="ArgumentNullException"/> si el valor es null.
        /// </summary>
        public static string ToIsoDate(this DateTime? date) =>
            date?.ToIsoDate() ?? throw new ArgumentNullException(nameof(date), "La fecha no puede ser nula.");

        /// <summary>
        /// Formato ISO 8601 fecha + hora UTC: <c>2026-04-08T14:30:00Z</c>.
        /// Corresponde al scalar <c>IsoDatetime</c> del schema GraphQL.
        /// Convierte a UTC si el Kind no es Utc.
        /// </summary>
        public static string ToIsoDatetime(this DateTime date) =>
            date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formato ISO 8601 fecha + hora UTC desde nullable.
        /// </summary>
        public static string ToIsoDatetime(this DateTime? date) =>
            date?.ToIsoDatetime() ?? throw new ArgumentNullException(nameof(date), "La fecha no puede ser nula.");

        /// <summary>
        /// Formato largo legible en español: <c>08 de abril de 2026</c>.
        /// </summary>
        public static string ToLongDateEs(this DateTime date) =>
            date.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("es-CO"));

        /// <summary>
        /// Formato corto día/mes/año: <c>08/04/2026</c>.
        /// </summary>
        public static string ToShortDateDMY(this DateTime date) =>
            date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formato corto día/mes/año + hora 12h: <c>08/04/2026 02:30 PM</c>.
        /// </summary>
        public static string ToShortDateTimeDMY(this DateTime date) =>
            date.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formato año-mes: <c>2026-04</c>. Útil para reportes mensuales.
        /// </summary>
        public static string ToYearMonth(this DateTime date) =>
            date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }
}
