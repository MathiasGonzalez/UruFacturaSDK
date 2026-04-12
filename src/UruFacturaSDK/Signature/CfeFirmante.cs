using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using UruFacturaSDK.Exceptions;

namespace UruFacturaSDK.Signature;

/// <summary>
/// Firmante digital XAdES-BES de documentos CFE según los requerimientos de la DGI de Uruguay.
/// Implementa la firma XML enveloped con referencia a todo el documento y soporte para
/// propiedades XAdES (SigningTime, SigningCertificate) que constituyen el nivel XAdES-BES.
/// </summary>
public class CfeFirmante : IDisposable
{
    private readonly X509Certificate2 _certificado;
    private bool _disposed;

    /// <summary>
    /// Inicializa el firmante con el certificado cargado en memoria.
    /// </summary>
    /// <param name="certificado">Certificado X.509 con clave privada.</param>
    public CfeFirmante(X509Certificate2 certificado)
    {
        if (!certificado.HasPrivateKey)
            throw new FirmaDigitalException("El certificado no contiene clave privada.");
        _certificado = certificado;
    }

    /// <summary>
    /// Inicializa el firmante desde un archivo .p12/.pfx.
    /// </summary>
    /// <param name="rutaArchivo">Ruta al archivo del certificado.</param>
    /// <param name="password">Contraseña del certificado.</param>
    public CfeFirmante(string rutaArchivo, string password)
        : this(X509CertificateLoader.LoadPkcs12FromFile(
               rutaArchivo, password,
               X509KeyStorageFlags.EphemeralKeySet))
    {
    }

    /// <summary>
    /// Firma digitalmente el XML del CFE con esquema XAdES-BES.
    /// </summary>
    /// <param name="xmlSinFirmar">XML sin firmar del CFE (UTF-8).</param>
    /// <returns>XML firmado.</returns>
    /// <exception cref="FirmaDigitalException">Si ocurre algún error durante la firma.</exception>
    public string Firmar(string xmlSinFirmar)
    {
        try
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xmlSinFirmar);

            var signedXml = new SignedXml(doc)
            {
                SigningKey = ObtenerClavePrivada(),
            };

            // Referencia al documento completo (enveloped signature)
            var reference = new Reference
            {
                Uri = "",
                DigestMethod = SignedXml.XmlDsigSHA256Url,
            };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            signedXml.AddReference(reference);

            // Información del firmante
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(_certificado));
            signedXml.KeyInfo = keyInfo;

            // Propiedad XAdES: SigningTime y SigningCertificate (nivel BES)
            var xadesObject = CrearObjetoXades(signedXml);
            signedXml.AddObject(xadesObject);

            signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            signedXml.ComputeSignature();

            // Insertar el elemento Signature en el documento
            var signatureElement = signedXml.GetXml();
            doc.DocumentElement!.AppendChild(doc.ImportNode(signatureElement, true));

            return PrettyPrint(doc);
        }
        catch (FirmaDigitalException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FirmaDigitalException("Error al firmar el XML del CFE.", ex);
        }
    }

    /// <summary>
    /// Verifica la firma digital de un XML CFE firmado.
    /// </summary>
    /// <param name="xmlFirmado">XML firmado a verificar.</param>
    /// <returns>True si la firma es válida.</returns>
    public static bool VerificarFirma(string xmlFirmado)
    {
        try
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xmlFirmado);

            var signedXml = new SignedXml(doc);
            var signatureNodes = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

            if (signatureNodes.Count == 0)
                return false;

            signedXml.LoadXml((XmlElement)signatureNodes[0]!);
            return signedXml.CheckSignature();
        }
        catch
        {
            return false;
        }
    }

    private AsymmetricAlgorithm ObtenerClavePrivada()
    {
        var rsa = _certificado.GetRSAPrivateKey()
            ?? throw new FirmaDigitalException(
                "No se pudo obtener la clave RSA privada del certificado.");
        return rsa;
    }

    private DataObject CrearObjetoXades(SignedXml signedXml)
    {
        const string xadesNs = "http://uri.etsi.org/01903/v1.3.2#";
        var objectId = "XadesObject";

        var doc = new XmlDocument();

        // QualifyingProperties
        var qualProps = doc.CreateElement("xades", "QualifyingProperties", xadesNs);
        qualProps.SetAttribute("Target", $"#{signedXml.Signature.Id ?? "Signature"}");

        var signedProps = doc.CreateElement("xades", "SignedProperties", xadesNs);
        signedProps.SetAttribute("Id", "SignedProperties");

        var signedSigProps = doc.CreateElement("xades", "SignedSignatureProperties", xadesNs);

        // SigningTime
        var signingTime = doc.CreateElement("xades", "SigningTime", xadesNs);
        signingTime.InnerText = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        signedSigProps.AppendChild(signingTime);

        // SigningCertificate
        var signingCert = doc.CreateElement("xades", "SigningCertificate", xadesNs);
        var cert = doc.CreateElement("xades", "Cert", xadesNs);
        var certDigest = doc.CreateElement("xades", "CertDigest", xadesNs);

        var digestMethod = doc.CreateElement("ds", "DigestMethod", SignedXml.XmlDsigNamespaceUrl);
        digestMethod.SetAttribute("Algorithm", SignedXml.XmlDsigSHA256Url);
        certDigest.AppendChild(digestMethod);

        var digestValue = doc.CreateElement("ds", "DigestValue", SignedXml.XmlDsigNamespaceUrl);
        digestValue.InnerText = Convert.ToBase64String(SHA256.HashData(_certificado.RawData));
        certDigest.AppendChild(digestValue);

        cert.AppendChild(certDigest);

        var issuerSerial = doc.CreateElement("xades", "IssuerSerial", xadesNs);
        var x509IssuerName = doc.CreateElement("ds", "X509IssuerName", SignedXml.XmlDsigNamespaceUrl);
        x509IssuerName.InnerText = _certificado.Issuer;
        var x509SerialNumber = doc.CreateElement("ds", "X509SerialNumber", SignedXml.XmlDsigNamespaceUrl);
        x509SerialNumber.InnerText = _certificado.SerialNumber;
        issuerSerial.AppendChild(x509IssuerName);
        issuerSerial.AppendChild(x509SerialNumber);

        cert.AppendChild(issuerSerial);
        signingCert.AppendChild(cert);
        signedSigProps.AppendChild(signingCert);

        signedProps.AppendChild(signedSigProps);
        qualProps.AppendChild(signedProps);
        doc.AppendChild(qualProps);

        var dataObject = new DataObject();
        dataObject.Id = objectId;
        dataObject.Data = qualProps.ChildNodes;

        return dataObject;
    }

    private static string PrettyPrint(XmlDocument doc)
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
        };
        using var writer = XmlWriter.Create(sb, settings);
        doc.Save(writer);
        return sb.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _certificado.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
