using System.Globalization;

namespace UruFacturaSDK.Formatting;

internal static class CfeFormat
{
    internal static readonly CultureInfo PdfMonetaryCulture = new("es-UY");

    internal static string DateIso(DateTime value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static string DateCompact(DateTime value) =>
        value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    internal static string DecimalInvariant(decimal value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    internal static string MonetaryPdf(decimal value, string format = "N2") =>
        value.ToString(format, PdfMonetaryCulture);
}
