using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Allure.Xunit.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlcLab.Web.Api;
using Xunit;

namespace PlcLab.Web.Tests;

[AllureSuite("CertificatesApi")]
public class CertificatesApiTests
{
    [Fact]
    [AllureFeature("Inventory")]
    public async Task GetCertificates_ReturnsTrustedAndRejectedEntries()
    {
        var pkiRoot = CreateTempPkiRoot();
        try
        {
            var trustedPath = Path.Combine(pkiRoot, "trusted", "trusted-test.cer");
            var rejectedPath = Path.Combine(pkiRoot, "rejected", "rejected-test.cer");
            WriteCertificateFile(trustedPath, "CN=TrustedTest");
            WriteCertificateFile(rejectedPath, "CN=RejectedTest");

            await using var app = await CreateTestAppAsync(pkiRoot);
            using var client = app.GetTestClient();

            var response = await client.GetAsync("/api/certificates");
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(pkiRoot, doc.RootElement.GetProperty("pkiRoot").GetString());

            var trusted = doc.RootElement.GetProperty("trusted");
            var rejected = doc.RootElement.GetProperty("rejected");

            Assert.Equal(1, trusted.GetArrayLength());
            Assert.Equal(1, rejected.GetArrayLength());
            Assert.Contains("CN=TrustedTest", trusted[0].GetProperty("subject").GetString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CN=RejectedTest", rejected[0].GetProperty("subject").GetString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            SafeDeleteDirectory(pkiRoot);
        }
    }

    [Fact]
    [AllureFeature("Promote")]
    public async Task Promote_MovesFileFromRejectedToTrusted()
    {
        var pkiRoot = CreateTempPkiRoot();
        try
        {
            var rejectedPath = Path.Combine(pkiRoot, "rejected", "move-me.cer");
            var trustedPath = Path.Combine(pkiRoot, "trusted", "certs", "move-me.cer");
            WriteCertificateFile(rejectedPath, "CN=PromoteMe");

            await using var app = await CreateTestAppAsync(pkiRoot);
            using var client = app.GetTestClient();

            var response = await client.PostAsJsonAsync("/api/certificates/promote", new CertificateActionRequest("move-me.cer"));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, body);

            Assert.False(File.Exists(rejectedPath));
            Assert.True(File.Exists(trustedPath));
        }
        finally
        {
            SafeDeleteDirectory(pkiRoot);
        }
    }

    [Fact]
    [AllureFeature("Reject")]
    public async Task Reject_MovesFileFromTrustedToRejected()
    {
        var pkiRoot = CreateTempPkiRoot();
        try
        {
            var trustedPath = Path.Combine(pkiRoot, "trusted", "reject-me.cer");
            var rejectedPath = Path.Combine(pkiRoot, "rejected", "certs", "reject-me.cer");
            WriteCertificateFile(trustedPath, "CN=RejectMe");

            await using var app = await CreateTestAppAsync(pkiRoot);
            using var client = app.GetTestClient();

            var response = await client.PostAsJsonAsync("/api/certificates/reject", new CertificateActionRequest("reject-me.cer"));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, body);

            Assert.False(File.Exists(trustedPath));
            Assert.True(File.Exists(rejectedPath));
        }
        finally
        {
            SafeDeleteDirectory(pkiRoot);
        }
    }

    [Fact]
    [AllureFeature("Delete")]
    public async Task DeleteRejected_RemovesCertificateFile()
    {
        var pkiRoot = CreateTempPkiRoot();
        try
        {
            var rejectedPath = Path.Combine(pkiRoot, "rejected", "delete-me.cer");
            WriteCertificateFile(rejectedPath, "CN=DeleteMe");

            await using var app = await CreateTestAppAsync(pkiRoot);
            using var client = app.GetTestClient();

            var response = await client.DeleteAsync("/api/certificates/rejected/delete-me.cer");
            response.EnsureSuccessStatusCode();

            Assert.False(File.Exists(rejectedPath));
        }
        finally
        {
            SafeDeleteDirectory(pkiRoot);
        }
    }

    [Fact]
    [AllureFeature("Validation")]
    public async Task Promote_PathTraversalFilename_ReturnsBadRequest()
    {
        var pkiRoot = CreateTempPkiRoot();
        try
        {
            await using var app = await CreateTestAppAsync(pkiRoot);
            using var client = app.GetTestClient();

            var response = await client.PostAsJsonAsync("/api/certificates/promote", new CertificateActionRequest("../evil.cer"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            SafeDeleteDirectory(pkiRoot);
        }
    }

    private static async Task<WebApplication> CreateTestAppAsync(string pkiRoot)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpcUa:PkiRootPath"] = pkiRoot
        });

        builder.Services.AddScoped<PlcLab.Web.Services.CertificatesService>();

        var app = builder.Build();
        CertificatesApi.MapCertificatesApi(app);

        await app.StartAsync();
        return app;
    }

    private static string CreateTempPkiRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PlcLab-CertsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "trusted"));
        Directory.CreateDirectory(Path.Combine(root, "rejected"));
        return root;
    }

    private static void WriteCertificateFile(string path, string subject)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var certBytes = CreateCertificateBytes(subject);
        File.WriteAllBytes(path, certBytes);
    }

    private static byte[] CreateCertificateBytes(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return cert.Export(X509ContentType.Cert);
    }

    private static void SafeDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Ignore cleanup race/locks from test host finalizers.
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore cleanup race/locks from test host finalizers.
        }
    }

    private sealed record CertificateActionRequest(string FileName);
}
