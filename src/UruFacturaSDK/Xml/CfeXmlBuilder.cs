using System.Text;
using System.Xml;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Formatting;

namespace UruFacturaSDK.Xml;

/// <summary>
/// Genera el XML de un CFE según el esquema de la DGI de Uruguay (versión 23.01).
/// </summary>
public class CfeXmlBuilder : ICfeXmlBuilder
{
    private const string NsUri = "http://cfe.dgi.gub.uy";

    /// <summary>
    /// Genera el XML sin firmar del CFE.
    /// </summary>
    /// <param name="cfe">El CFE a serializar.</param>
    /// <returns>Cadena XML UTF-8 sin firmar.</returns>
    /// <exception cref="CfeValidationException">Si el CFE tiene errores de validación.</exception>
    public string Generar(Cfe cfe)
    {
        var errores = cfe.Validar();
        if (errores.Count > 0)
            throw new CfeValidationException(errores);

        cfe.CalcularTotales();

        // Use MemoryStream so XmlWriter writes the declaration as encoding="utf-8".
        // Writing to a StringBuilder always produces encoding="utf-16" regardless of
        // XmlWriterSettings.Encoding, because StringBuilder is internally UTF-16.
        // Use UTF8Encoding without BOM so the resulting string is directly parseable
        // by XmlDocument.LoadXml (which rejects a leading BOM character).
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using var writer = XmlWriter.Create(ms, settings);
        EscribirCfe(writer, cfe);
        writer.Flush();

        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    private static void EscribirCfe(XmlWriter w, Cfe cfe)
    {
        w.WriteStartDocument();
        w.WriteStartElement("CFE", NsUri);
        w.WriteAttributeString("version", "23.01");

        // Identificación del documento
        w.WriteStartElement("Encabezado", NsUri);
        EscribirIdDoc(w, cfe);
        EscribirEmisor(w, cfe);

        if (cfe.Receptor != null)
            EscribirReceptor(w, cfe);

        EscribirTotales(w, cfe);
        w.WriteEndElement(); // Encabezado

        // Detalle de líneas
        w.WriteStartElement("Detalle", NsUri);
        for (int i = 0; i < cfe.Detalle.Count; i++)
        {
            EscribirLinea(w, cfe.Detalle[i], i + 1);
        }
        w.WriteEndElement(); // Detalle

        // Referencias
        if (cfe.Referencias.Count > 0)
        {
            w.WriteStartElement("Referencia", NsUri);
            foreach (var refCfe in cfe.Referencias)
                EscribirReferencia(w, refCfe);
            w.WriteEndElement(); // Referencia
        }

        w.WriteEndElement(); // CFE
        w.WriteEndDocument();
    }

    private static void EscribirIdDoc(XmlWriter w, Cfe cfe)
    {
        w.WriteStartElement("IdDoc", NsUri);
        WriteElement(w, "TipoCFE", ((int)cfe.Tipo).ToString());
        WriteElement(w, "Serie", cfe.Serie ?? string.Empty);
        WriteElement(w, "Nro", cfe.Numero.ToString());
        WriteElement(w, "FchEmis", CfeFormat.DateIso(cfe.FechaEmision));
        WriteElement(w, "FmaPago", ((int)cfe.FormaPago).ToString());
        WriteElement(w, "MntBruto", "0"); // precios netos (sin IVA) en el detalle
        if (cfe.Moneda != Moneda.PesoUruguayo)
        {
            WriteElement(w, "TipoMoneda", ObtenerCodigoMoneda(cfe.Moneda));
            if (cfe.TipoCambio.HasValue)
                WriteElement(w, "TpoCambio", CfeFormat.DecimalInvariant(cfe.TipoCambio.Value, "F4"));
        }

        // e-Remito y e-Remito Despachante requieren el motivo de traslado
        if ((cfe.Tipo == TipoCfe.ERemito || cfe.Tipo == TipoCfe.ERemitoDespachante)
            && cfe.IndTraslado.HasValue)
        {
            WriteElement(w, "IndTraslado", ((int)cfe.IndTraslado.Value).ToString());
        }

        w.WriteEndElement(); // IdDoc
    }

    private static void EscribirEmisor(XmlWriter w, Cfe cfe)
    {
        w.WriteStartElement("Emisor", NsUri);
        WriteElement(w, "RUCEmisor", cfe.RutEmisor);
        WriteElement(w, "RznSoc", cfe.RazonSocialEmisor);
        if (!string.IsNullOrWhiteSpace(cfe.NombreComercialEmisor))
            WriteElement(w, "NomComercial", cfe.NombreComercialEmisor);
        if (!string.IsNullOrWhiteSpace(cfe.Giro))
            WriteElement(w, "GiroNegocio", cfe.Giro);
        WriteElement(w, "DomFiscal", cfe.DomicilioFiscalEmisor);
        WriteElement(w, "Ciudad", cfe.CiudadEmisor);
        WriteElement(w, "Departamento", cfe.DepartamentoEmisor);
        w.WriteEndElement(); // Emisor
    }

    private static void EscribirReceptor(XmlWriter w, Cfe cfe)
    {
        var receptor = cfe.Receptor!;
        w.WriteStartElement("Receptor", NsUri);

        if (!string.IsNullOrWhiteSpace(receptor.Documento))
        {
            WriteElement(w, "TipoDocRecep", ((int)receptor.TipoDocumento).ToString());
            WriteElement(w, "DocRecep", receptor.Documento);
        }

        if (!string.IsNullOrWhiteSpace(receptor.RazonSocial))
            WriteElement(w, "RznSocRecep", receptor.RazonSocial);

        if (!string.IsNullOrWhiteSpace(receptor.Direccion))
            WriteElement(w, "DirRecep", receptor.Direccion);

        if (!string.IsNullOrWhiteSpace(receptor.Ciudad))
            WriteElement(w, "CiudadRecep", receptor.Ciudad);

        if (!string.IsNullOrWhiteSpace(receptor.Departamento))
            WriteElement(w, "DeptoRecep", receptor.Departamento);

        if (!string.IsNullOrWhiteSpace(receptor.Pais))
            WriteElement(w, "PaisRecep", receptor.Pais);

        if (!string.IsNullOrWhiteSpace(receptor.Email))
            WriteElement(w, "Email", receptor.Email);

        w.WriteEndElement(); // Receptor
    }

    private static void EscribirTotales(XmlWriter w, Cfe cfe)
    {
        w.WriteStartElement("Totales", NsUri);

        if (cfe.MontoNetoExento > 0)
            WriteElement(w, "MntExe", CfeFormat.DecimalInvariant(cfe.MontoNetoExento, "F2"));

        if (cfe.MontoNetoSuspendido > 0)
            WriteElement(w, "MntSuspenso", CfeFormat.DecimalInvariant(cfe.MontoNetoSuspendido, "F2"));

        if (cfe.MontoNetoMinimo > 0)
        {
            WriteElement(w, "MntNetoIvaTasaMin", CfeFormat.DecimalInvariant(cfe.MontoNetoMinimo, "F2"));
            WriteElement(w, "IVATasaMin", "10.000");
            WriteElement(w, "MntIVATasaMin", CfeFormat.DecimalInvariant(cfe.IvaMinimo, "F2"));
        }

        if (cfe.MontoNetoBasico > 0)
        {
            WriteElement(w, "MntNetoIVA", CfeFormat.DecimalInvariant(cfe.MontoNetoBasico, "F2"));
            WriteElement(w, "IVATasa", "22.000");
            WriteElement(w, "MntIVA", CfeFormat.DecimalInvariant(cfe.IvaBasico, "F2"));
        }

        WriteElement(w, "MntTotal", CfeFormat.DecimalInvariant(cfe.MontoTotal, "F2"));
        WriteElement(w, "MntPagar", CfeFormat.DecimalInvariant(cfe.MontoTotal, "F2"));
        WriteElement(w, "CantLinDet", cfe.Detalle.Count.ToString());

        w.WriteEndElement(); // Totales
    }

    private static void EscribirLinea(XmlWriter w, LineaDetalle linea, int nro)
    {
        w.WriteStartElement("Item", NsUri);

        WriteElement(w, "NroLinDet", nro.ToString());

        if (!string.IsNullOrWhiteSpace(linea.UnidadMedida))
            WriteElement(w, "UniMed", linea.UnidadMedida);

        WriteElement(w, "NomItem", linea.NombreItem);
        WriteElement(w, "Cantidad", CfeFormat.DecimalInvariant(linea.Cantidad, "F4"));
        WriteElement(w, "PrecioUnitario", CfeFormat.DecimalInvariant(linea.PrecioUnitario, "F4"));

        if (linea.DescuentoMonto > 0)
            WriteElement(w, "DescuentoMonto", CfeFormat.DecimalInvariant(linea.DescuentoMonto, "F2"));

        if (linea.RecargoMonto > 0)
            WriteElement(w, "RecargoMonto", CfeFormat.DecimalInvariant(linea.RecargoMonto, "F2"));

        WriteElement(w, "IndFact", ((int)linea.IndFactIva).ToString());
        WriteElement(w, "MontoItem", CfeFormat.DecimalInvariant(linea.MontoTotal, "F2"));

        w.WriteEndElement(); // Item
    }

    private static void EscribirReferencia(XmlWriter w, RefCfe refCfe)
    {
        w.WriteStartElement("RefDoc", NsUri);
        WriteElement(w, "TpoDocRef", ((int)refCfe.TipoCfe).ToString());
        WriteElement(w, "Serie", refCfe.Serie);
        WriteElement(w, "NroCFERef", refCfe.NroCfe.ToString());
        WriteElement(w, "FechaCFERef", CfeFormat.DateIso(refCfe.FechaCfe));
        if (!string.IsNullOrWhiteSpace(refCfe.Razon))
            WriteElement(w, "RazonRef", refCfe.Razon);
        w.WriteEndElement(); // RefDoc
    }

    private static void WriteElement(XmlWriter w, string name, string value)
    {
        w.WriteStartElement(name, NsUri);
        w.WriteString(value);
        w.WriteEndElement();
    }

    /// <summary>
    /// Retorna el código ISO 4217 alfabético de 3 letras que usa la DGI
    /// para el elemento <c>TipoMoneda</c> (sólo para monedas distintas al peso uruguayo).
    /// </summary>
    private static string ObtenerCodigoMoneda(Moneda moneda) => moneda switch
    {
        Moneda.DolarAmericano => "USD",
        Moneda.Euro           => "EUR",
        _ => throw new ArgumentOutOfRangeException(nameof(moneda),
                 $"Moneda '{moneda}' sin código ISO 4217 definido."),
    };
}
