using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Rendering;

namespace UruFacturaSDK.Pdf;

/// <summary>
/// Genera la representación impresa (PDF) de los CFE en formato A4 o térmico (ticket),
/// incluyendo el código QR y los datos requeridos por normativa DGI.
/// </summary>
public class CfePdfGenerator
{
    private readonly UruFacturaConfig _config;

    static CfePdfGenerator()
    {
        // Configurar la licencia comunitaria de QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public CfePdfGenerator(UruFacturaConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Genera el PDF en formato A4 del CFE.
    /// </summary>
    /// <param name="cfe">El CFE con datos completos.</param>
    /// <returns>Array de bytes del PDF generado.</returns>
    public byte[] GenerarA4(Cfe cfe)
    {
        try
        {
            var qrBytes = GenerarQrCode(cfe, _config.Ambiente);
            var document = new CfeDocumentoA4(cfe, _config, qrBytes);
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            throw new PdfGenerationException("Error al generar el PDF A4 del CFE.", ex);
        }
    }

    /// <summary>
    /// Genera el PDF en formato térmico (ticket de 80mm) del CFE.
    /// </summary>
    /// <param name="cfe">El CFE con datos completos.</param>
    /// <returns>Array de bytes del PDF generado.</returns>
    public byte[] GenerarTermico(Cfe cfe)
    {
        try
        {
            var qrBytes = GenerarQrCode(cfe, _config.Ambiente);
            var document = new CfeDocumentoTermico(cfe, _config, qrBytes);
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            throw new PdfGenerationException("Error al generar el PDF térmico del CFE.", ex);
        }
    }

    /// <summary>
    /// Genera el código QR del CFE según normativa DGI.
    /// El contenido del QR es la URL de consulta con los datos del comprobante.
    /// Usa el ambiente de <b>Producción</b> por defecto.
    /// </summary>
    /// <param name="cfe">El CFE.</param>
    public static byte[] GenerarQrCode(Cfe cfe) =>
        GenerarQrCode(cfe, Ambiente.Produccion);

    /// <summary>
    /// Genera el código QR del CFE según normativa DGI.
    /// El contenido del QR es la URL de consulta con los datos del comprobante.
    /// </summary>
    /// <param name="cfe">El CFE.</param>
    /// <param name="ambiente">Ambiente de operación (Homologación o Producción).</param>
    public static byte[] GenerarQrCode(Cfe cfe, Ambiente ambiente)
    {
        var contenidoQr = ConstruirContenidoQr(cfe, ambiente);

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = 300,
                Height = 300,
                Margin = 1,
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            },
        };

        var pixelData = writer.Write(contenidoQr);

        // Convert raw BGRA pixel data to PNG using SkiaSharp
        using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        System.Runtime.InteropServices.Marshal.Copy(
            pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static string ConstruirContenidoQr(Cfe cfe, Ambiente ambiente)
    {
        // Formato de URL de verificación DGI Uruguay
        var baseUrl = ambiente == Ambiente.Produccion
            ? "https://efactura.dgi.gub.uy/efactura/valida"
            : "https://efacturahomologacion.dgi.gub.uy/efactura/valida";

        return $"{baseUrl}?" +
               $"ruc={Uri.EscapeDataString(cfe.RutEmisor)}" +
               $"&tipoCfe={(int)cfe.Tipo}" +
               $"&serie={Uri.EscapeDataString(cfe.Serie ?? string.Empty)}" +
               $"&nro={cfe.Numero}" +
               $"&fecha={cfe.FechaEmision:yyyyMMdd}" +
               $"&monto={cfe.MontoTotal:F2}";
    }
}

// ---------------------------------------------------------------------------
// Documento PDF A4
// ---------------------------------------------------------------------------

internal class CfeDocumentoA4 : IDocument
{
    private readonly Cfe _cfe;
    private readonly UruFacturaConfig _config;
    private readonly byte[] _qrBytes;

    public CfeDocumentoA4(Cfe cfe, UruFacturaConfig config, byte[] qrBytes)
    {
        _cfe = cfe;
        _config = config;
        _qrBytes = qrBytes;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"CFE {(int)_cfe.Tipo} - {_cfe.Serie}{_cfe.Numero}",
        Author = _config.RazonSocialEmisor,
        Creator = "UruFacturaSDK",
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Encabezado del emisor
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Column(emisor =>
                {
                    emisor.Item().Text(_config.RazonSocialEmisor).Bold().FontSize(14);
                    if (!string.IsNullOrWhiteSpace(_config.NombreComercialEmisor))
                        emisor.Item().Text(_config.NombreComercialEmisor).FontSize(10);
                    emisor.Item().Text($"RUT: {_config.RutEmisor}");
                    emisor.Item().Text(_config.DomicilioFiscal);
                    emisor.Item().Text($"{_config.Ciudad}, {_config.Departamento}");
                });

                row.RelativeItem(2).AlignRight().Column(tipoDoc =>
                {
                    tipoDoc.Item()
                        .Border(1)
                        .Padding(8)
                        .AlignCenter()
                        .Column(c =>
                        {
                            c.Item().Text(ObtenerNombreTipo()).Bold().FontSize(12);
                            c.Item().Text($"N° {_cfe.Serie}{_cfe.Numero:D8}").Bold().FontSize(11);
                            c.Item().Text($"Fecha: {_cfe.FechaEmision:dd/MM/yyyy}");
                        });
                });
            });

            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Receptor
            if (_cfe.Receptor != null)
            {
                col.Item().PaddingVertical(5).Column(rec =>
                {
                    rec.Item().Text("Receptor:").Bold();
                    if (!string.IsNullOrWhiteSpace(_cfe.Receptor.RazonSocial))
                        rec.Item().Text(_cfe.Receptor.RazonSocial);
                    if (!string.IsNullOrWhiteSpace(_cfe.Receptor.Documento))
                        rec.Item().Text($"RUT/Doc: {_cfe.Receptor.Documento}");
                    if (!string.IsNullOrWhiteSpace(_cfe.Receptor.Direccion))
                        rec.Item().Text(_cfe.Receptor.Direccion);
                });

                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
            }

            // Detalle
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(40);  // Cant
                    cols.RelativeColumn(5);   // Descripción
                    cols.RelativeColumn(2);   // P.Unit
                    cols.RelativeColumn(1);   // IVA
                    cols.RelativeColumn(2);   // Total
                });

                // Encabezado de tabla
                static IContainer CeldaHeader(IContainer c) =>
                    c.DefaultTextStyle(x => x.Bold()).Background(Colors.Grey.Lighten2)
                     .Padding(4).AlignCenter();

                table.Header(h =>
                {
                    h.Cell().Element(CeldaHeader).Text("Cant.");
                    h.Cell().Element(CeldaHeader).Text("Descripción");
                    h.Cell().Element(CeldaHeader).Text("P.Unit.");
                    h.Cell().Element(CeldaHeader).Text("IVA");
                    h.Cell().Element(CeldaHeader).Text("Total");
                });

                // Filas de detalle
                static IContainer Celda(IContainer c) => c.Padding(4).AlignRight();
                static IContainer CeldaLeft(IContainer c) => c.Padding(4);

                foreach (var linea in _cfe.Detalle)
                {
                    table.Cell().Element(Celda).Text(linea.Cantidad.ToString("F2"));
                    table.Cell().Element(CeldaLeft).Text(linea.NombreItem);
                    table.Cell().Element(Celda).Text(linea.PrecioUnitario.ToString("N2"));
                    table.Cell().Element(Celda).Text(ObtenerEtiquetaIva(linea.IndFactIva));
                    table.Cell().Element(Celda).Text(linea.MontoTotal.ToString("N2"));
                }
            });

            // Totales
            col.Item().PaddingTop(10).AlignRight().Column(tot =>
            {
                if (_cfe.MontoNetoExento > 0)
                    tot.Item().Text($"Exento: {_cfe.MontoNetoExento:N2}");
                if (_cfe.MontoNetoMinimo > 0)
                {
                    tot.Item().Text($"Neto IVA 10%: {_cfe.MontoNetoMinimo:N2}");
                    tot.Item().Text($"IVA 10%: {_cfe.IvaMinimo:N2}");
                }
                if (_cfe.MontoNetoBasico > 0)
                {
                    tot.Item().Text($"Neto IVA 22%: {_cfe.MontoNetoBasico:N2}");
                    tot.Item().Text($"IVA 22%: {_cfe.IvaBasico:N2}");
                }
                tot.Item().Text($"TOTAL: {_cfe.MontoTotal:N2}").Bold().FontSize(12);
            });

            // QR y sello de seguridad
            col.Item().PaddingTop(20).Row(row =>
            {
                row.AutoItem().Width(80).Height(80).Image(_qrBytes);

                row.RelativeItem().PaddingLeft(10).Column(qrInfo =>
                {
                    qrInfo.Item().Text("Representación impresa de CFE").Italic().FontSize(8);
                    qrInfo.Item().Text("Verifique la vigencia en: efactura.dgi.gub.uy").FontSize(8);
                    qrInfo.Item().Text($"Ambiente: {_config.Ambiente}").FontSize(8);
                    if (_cfe.AceptadoPorDgi)
                        qrInfo.Item().Text("✅ Aceptado por DGI").FontSize(8).FontColor(Colors.Green.Darken2);
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generado con UruFacturaSDK - ").FontSize(7).FontColor(Colors.Grey.Medium);
                text.Span("Página ").FontSize(7).FontColor(Colors.Grey.Medium);
                text.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                text.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                text.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private string ObtenerNombreTipo() => _cfe.Tipo switch
    {
        Enums.TipoCfe.ETicket => "e-Ticket",
        Enums.TipoCfe.NotaCreditoETicket => "Nota de Crédito e-Ticket",
        Enums.TipoCfe.NotaDebitoETicket => "Nota de Débito e-Ticket",
        Enums.TipoCfe.EFactura => "e-Factura",
        Enums.TipoCfe.NotaCreditoEFactura => "Nota de Crédito e-Factura",
        Enums.TipoCfe.NotaDebitoEFactura => "Nota de Débito e-Factura",
        Enums.TipoCfe.EFacturaExportacion => "e-Factura Exportación",
        Enums.TipoCfe.ERemito => "e-Remito",
        _ => $"CFE Tipo {(int)_cfe.Tipo}",
    };

    private static string ObtenerEtiquetaIva(Enums.TipoIva iva) => iva switch
    {
        Enums.TipoIva.Exento => "EXE",
        Enums.TipoIva.Minimo => "10%",
        Enums.TipoIva.Basico => "22%",
        Enums.TipoIva.Suspendido => "SUS",
        _ => "N/A",
    };
}

// ---------------------------------------------------------------------------
// Documento PDF Térmico (80mm)
// ---------------------------------------------------------------------------

internal class CfeDocumentoTermico : IDocument
{
    private readonly Cfe _cfe;
    private readonly UruFacturaConfig _config;
    private readonly byte[] _qrBytes;

    // Ancho térmico estándar: 80mm ≈ 227 puntos
    private static readonly PageSize TermicoSize = new(227, 800);

    public CfeDocumentoTermico(Cfe cfe, UruFacturaConfig config, byte[] qrBytes)
    {
        _cfe = cfe;
        _config = config;
        _qrBytes = qrBytes;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"CFE {(int)_cfe.Tipo} - {_cfe.Serie}{_cfe.Numero}",
        Author = _config.RazonSocialEmisor,
        Creator = "UruFacturaSDK",
    };

    public DocumentSettings GetSettings() => new()
    {
        ContentDirection = ContentDirection.LeftToRight,
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(TermicoSize);
            page.Margin(5);
            page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));
            page.Content().Element(ComposeContent);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Encabezado
            col.Item().AlignCenter().Text(_config.RazonSocialEmisor).Bold().FontSize(9);
            col.Item().AlignCenter().Text($"RUT: {_config.RutEmisor}");
            col.Item().AlignCenter().Text(_config.DomicilioFiscal);
            col.Item().LineHorizontal(0.5f);

            // Tipo y número
            col.Item().AlignCenter().Text(ObtenerNombreTipo()).Bold().FontSize(8);
            col.Item().AlignCenter().Text($"N° {_cfe.Serie}{_cfe.Numero:D8}").Bold();
            col.Item().AlignCenter().Text($"Fecha: {_cfe.FechaEmision:dd/MM/yyyy}");
            col.Item().LineHorizontal(0.5f);

            // Receptor
            if (_cfe.Receptor?.RazonSocial != null)
            {
                col.Item().Text($"Cliente: {_cfe.Receptor.RazonSocial}");
                col.Item().LineHorizontal(0.5f);
            }

            // Detalle
            foreach (var linea in _cfe.Detalle)
            {
                col.Item().Text($"{linea.NombreItem}");
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"  {linea.Cantidad:F2} x {linea.PrecioUnitario:N2}");
                    row.AutoItem().Text($"{linea.MontoTotal:N2}");
                });
            }

            col.Item().LineHorizontal(0.5f);

            // Totales
            if (_cfe.MontoNetoExento > 0)
                col.Item().Row(r => { r.RelativeItem().Text("Exento:"); r.AutoItem().Text($"{_cfe.MontoNetoExento:N2}"); });
            if (_cfe.IvaMinimo > 0)
                col.Item().Row(r => { r.RelativeItem().Text("IVA 10%:"); r.AutoItem().Text($"{_cfe.IvaMinimo:N2}"); });
            if (_cfe.IvaBasico > 0)
                col.Item().Row(r => { r.RelativeItem().Text("IVA 22%:"); r.AutoItem().Text($"{_cfe.IvaBasico:N2}"); });

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("TOTAL:").Bold();
                r.AutoItem().Text($"{_cfe.MontoTotal:N2}").Bold();
            });

            col.Item().LineHorizontal(0.5f);

            // QR
            col.Item().PaddingTop(5).AlignCenter().Width(70).Height(70).Image(_qrBytes);
            col.Item().AlignCenter().Text("Verifique en efactura.dgi.gub.uy").FontSize(6);
        });
    }

    private string ObtenerNombreTipo() => _cfe.Tipo switch
    {
        Enums.TipoCfe.ETicket => "e-Ticket",
        Enums.TipoCfe.EFactura => "e-Factura",
        Enums.TipoCfe.NotaCreditoETicket => "NC e-Ticket",
        Enums.TipoCfe.NotaDebitoETicket => "ND e-Ticket",
        _ => $"CFE {(int)_cfe.Tipo}",
    };
}
