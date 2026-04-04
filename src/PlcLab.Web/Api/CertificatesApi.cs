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
            Results.Ok(svc.GetInventory()))
            .WithTags("Certificates")
            .WithName("GetCertificateInventory")
            .WithSummary("Returns certificate inventory for trusted, rejected, and own stores.")
            .WithDescription("Lists certificate files from the configured PKI root and includes parsed certificate metadata when available.")
            .Produces<CertificateInventoryResponse>(StatusCodes.Status200OK);

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
        })
        .WithTags("Certificates")
        .WithName("PromoteCertificate")
        .WithSummary("Moves a rejected certificate into the trusted store.")
        .WithDescription("Promotes a certificate file from the rejected certificate store to the trusted certificate store.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

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
        })
        .WithTags("Certificates")
        .WithName("RejectCertificate")
        .WithSummary("Moves a trusted certificate into the rejected store.")
        .WithDescription("Demotes a certificate file from the trusted certificate store to the rejected certificate store.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

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
        })
        .WithTags("Certificates")
        .WithName("DeleteRejectedCertificate")
        .WithSummary("Deletes a certificate file from the rejected store.")
        .WithDescription("Permanently removes a certificate from the rejected certificate store.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
