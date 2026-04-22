using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;
using UruFacturaSDK.Signature;
using UruFacturaSDK.Xml;
using Xunit;

namespace UruFacturaSDK.Tests;

/// <summary>
/// Tests para la compatibilidad DGI de la firma XAdES-BES.
/// Se utiliza un certificado auto-firmado generado en memoria (sólo para tests).
/// </summary>
public class CfeFirmanteTests : IDisposable
{
    private readonly X509Certificate2 _certificadoTest;
    private readonly CfeFirmante _firmante;
    private readonly CfeXmlBuilder _builder = new();

    public CfeFirmanteTests()
    {
        _certificadoTest = GenerarCertificadoAutoFirmado();
        _firmante = new CfeFirmante(_certificadoTest);
    }

    public void Dispose()
    {
        _firmante.Dispose();
        _certificadoTest.Dispose();
    }

    private static X509Certificate2 GenerarCertificadoAutoFirmado()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=UruFacturaTest",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        // Exportar e importar para que HasPrivateKey sea true en todos los runtimes
        return X509CertificateLoader.LoadPkcs12(
            cert.Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.EphemeralKeySet);
    }

    private static Cfe CrearCfeBasico() => new()
    {
        Tipo = TipoCfe.ETicket,
        Serie = "A",
        Numero = 1,
        FechaEmision = new DateTime(2025, 6, 15),
        RutEmisor = "210000000012",
        RazonSocialEmisor = "Empresa Test S.A.",
        DomicilioFiscalEmisor = "Av. 18 de Julio 1234",
        CiudadEmisor = "Montevideo",
        DepartamentoEmisor = "Montevideo",
        Detalle =
        {
            new LineaDetalle
            {
                NroLinea = 1,
                NombreItem = "Servicio",
                Cantidad = 1,
                PrecioUnitario = 100m,
                IndFactIva = TipoIva.Basico,
            }
        }
    };

    // -----------------------------------------------------------------------
    // Tests de estructura XAdES-BES
    // -----------------------------------------------------------------------

    [Fact]
    public void Firmar_XmlFirmado_ContieneElementoSignature()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        Assert.Contains("<Signature", xmlFirmado);
    }

    [Fact]
    public void Firmar_XmlFirmado_ContieneQualifyingPropertiesConTarget()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlFirmado);

        // QualifyingProperties debe existir y tener el atributo Target
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xades", "http://uri.etsi.org/01903/v1.3.2#");

        var qualProps = doc.SelectSingleNode("//xades:QualifyingProperties", nsMgr);
        Assert.NotNull(qualProps);

        var target = qualProps?.Attributes?["Target"]?.Value;
        Assert.NotNull(target);
        Assert.StartsWith("#", target);
    }

    [Fact]
    public void Firmar_XmlFirmado_ContieneSignedPropertiesConId()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlFirmado);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xades", "http://uri.etsi.org/01903/v1.3.2#");

        var signedProps = doc.SelectSingleNode("//xades:SignedProperties", nsMgr);
        Assert.NotNull(signedProps);

        var id = signedProps?.Attributes?["Id"]?.Value;
        Assert.Equal("SignedProperties", id);
    }

    [Fact]
    public void Firmar_XmlFirmado_SignedInfoContieneReferenciaASignedProperties()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlFirmado);

        // Debe haber una Reference en SignedInfo con URI="#SignedProperties"
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

        var refs = doc.SelectNodes("//ds:SignedInfo/ds:Reference", nsMgr);
        Assert.NotNull(refs);
        Assert.True(refs!.Count >= 2, "Debe haber al menos 2 referencias: al documento y a SignedProperties.");

        bool tieneRefSignedProperties = false;
        foreach (XmlNode r in refs)
        {
            var uri = r.Attributes?["URI"]?.Value;
            var type = r.Attributes?["Type"]?.Value;
            if (uri == "#SignedProperties"
                && type == "http://uri.etsi.org/01903#SignedProperties")
            {
                tieneRefSignedProperties = true;
                break;
            }
        }
        Assert.True(tieneRefSignedProperties,
            "SignedInfo debe contener una Reference a #SignedProperties de tipo XAdES-BES.");
    }

    [Fact]
    public void Firmar_XmlFirmado_ContieneSigningTime()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlFirmado);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xades", "http://uri.etsi.org/01903/v1.3.2#");

        var signingTime = doc.SelectSingleNode("//xades:SigningTime", nsMgr);
        Assert.NotNull(signingTime);
        Assert.NotEmpty(signingTime!.InnerText);
    }

    [Fact]
    public void Firmar_XmlFirmado_ContieneSigningCertificate()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xmlFirmado);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xades", "http://uri.etsi.org/01903/v1.3.2#");

        var signingCert = doc.SelectSingleNode("//xades:SigningCertificate", nsMgr);
        Assert.NotNull(signingCert);
    }

    [Fact]
    public void VerificarFirma_XmlFirmado_RetornaTrue()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        Assert.True(CfeFirmante.VerificarFirma(xmlFirmado));
    }

    [Fact]
    public void VerificarFirma_XmlModificadoPostFirma_RetornaFalse()
    {
        var xml = _builder.Generar(CrearCfeBasico());
        var xmlFirmado = _firmante.Firmar(xml);

        // Modificar el contenido firmado
        var xmlManipulado = xmlFirmado.Replace(
            "Empresa Test S.A.",
            "Empresa Falsa S.A.");

        Assert.False(CfeFirmante.VerificarFirma(xmlManipulado));
    }
}
