using Microsoft.AspNetCore.Mvc;
using PlcLab.Web.Services;

namespace PlcLab.Web.Api;

public sealed record CertificateInventoryResponse(string PkiRoot, List<CertificateEntry> Trusted, List<CertificateEntry> Rejected, List<CertificateEntry> Own);

public sealed record CertificateEntry(
    string FileName,
    long SizeBytes,
    DateTimeOffset LastWriteUtc,
    string? Subject,
    string? Thumbprint,
    DateTimeOffset? NotBeforeUtc,
    DateTimeOffset? NotAfterUtc);

public sealed record CertificateActionRequest(string FileName);

public static class CertificatesApi
{
    public static void MapCertificatesApi(this WebApplication app)
    {
        app.MapGet("/api/certificates", ([FromServices] CertificatesService svc) =>
            Results.Ok(svc.GetInventory()));

        app.MapPost("/api/certificates/promote", async (
            [FromServices] CertificatesService svc,
            [FromBody] CertificateActionRequest request,
            CancellationToken ct) =>
        {
            try
            {
                await svc.PromoteAsync(request.FileName, ct).ConfigureAwait(false);
                return Results.Ok(new { message = "Certificate promoted to trusted store." });
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        app.MapPost("/api/certificates/reject", async (
            [FromServices] CertificatesService svc,
            [FromBody] CertificateActionRequest request,
            CancellationToken ct) =>
        {
            try
            {
                await svc.RejectAsync(request.FileName, ct).ConfigureAwait(false);
                return Results.Ok(new { message = "Certificate moved to rejected store." });
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        app.MapDelete("/api/certificates/rejected/{fileName}", (
            [FromServices] CertificatesService svc,
            string fileName) =>
        {
            try
            {
                svc.DeleteRejected(fileName);
                return Results.Ok(new { message = "Rejected certificate removed." });
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });
    }
}
