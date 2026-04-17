using System.Globalization;
using System.Text;
using System.Xml;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;
using UruFacturaSDK.Exceptions;

namespace UruFacturaSDK.Xml;

/// <summary>
/// Genera el XML de un CFE según el esquema de la DGI de Uruguay (versión 23.01).
/// </summary>
public class CfeXmlBuilder
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

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using var writer = XmlWriter.Create(sb, settings);
        EscribirCfe(writer, cfe);
        writer.Flush();

        return sb.ToString();
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
        WriteElement(w, "FchEmis", cfe.FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        WriteElement(w, "FmaPago", ((int)cfe.FormaPago).ToString());
        WriteElement(w, "MntBruto", "0"); // precios netos (sin IVA) en el detalle
        if (cfe.Moneda != Moneda.PesoUruguayo)
        {
            WriteElement(w, "TipoMoneda", ObtenerCodigoMoneda(cfe.Moneda));
            if (cfe.TipoCambio.HasValue)
                WriteElement(w, "TpoCambio", cfe.TipoCambio.Value.ToString("F4", CultureInfo.InvariantCulture));
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
            WriteElement(w, "TipoDocRecep", receptor.TipoDocumento.ToString());
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
            WriteElement(w, "MntExe", cfe.MontoNetoExento.ToString("F2", CultureInfo.InvariantCulture));

        if (cfe.MontoNetoMinimo > 0)
        {
            WriteElement(w, "MntNetoIvaTasaMin", cfe.MontoNetoMinimo.ToString("F2", CultureInfo.InvariantCulture));
            WriteElement(w, "IVATasaMin", "10.000");
            WriteElement(w, "MntIVATasaMin", cfe.IvaMinimo.ToString("F2", CultureInfo.InvariantCulture));
        }

        if (cfe.MontoNetoBasico > 0)
        {
            WriteElement(w, "MntNetoIVA", cfe.MontoNetoBasico.ToString("F2", CultureInfo.InvariantCulture));
            WriteElement(w, "IVATasa", "22.000");
            WriteElement(w, "MntIVA", cfe.IvaBasico.ToString("F2", CultureInfo.InvariantCulture));
        }

        WriteElement(w, "MntTotal", cfe.MontoTotal.ToString("F2", CultureInfo.InvariantCulture));
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
        WriteElement(w, "Cantidad", linea.Cantidad.ToString("F4", CultureInfo.InvariantCulture));
        WriteElement(w, "PrecioUnitario", linea.PrecioUnitario.ToString("F4", CultureInfo.InvariantCulture));

        if (linea.DescuentoMonto > 0)
            WriteElement(w, "DescuentoMonto", linea.DescuentoMonto.ToString("F2", CultureInfo.InvariantCulture));

        if (linea.RecargoMonto > 0)
            WriteElement(w, "RecargoMonto", linea.RecargoMonto.ToString("F2", CultureInfo.InvariantCulture));

        WriteElement(w, "IndFact", ((int)linea.IndFactIva).ToString());
        WriteElement(w, "MontoItem", linea.MontoTotal.ToString("F2", CultureInfo.InvariantCulture));

        w.WriteEndElement(); // Item
    }

    private static void EscribirReferencia(XmlWriter w, RefCfe refCfe)
    {
        w.WriteStartElement("RefDoc", NsUri);
        WriteElement(w, "TpoDocRef", ((int)refCfe.TipoCfe).ToString());
        WriteElement(w, "Serie", refCfe.Serie);
        WriteElement(w, "NroCFERef", refCfe.NroCfe.ToString());
        WriteElement(w, "FechaCFERef", refCfe.FechaCfe.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
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
