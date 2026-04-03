using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PlcLab.Web.Api;

namespace PlcLab.Web.Services;

public sealed class CertificatesService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public CertificatesService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    public CertificateInventoryResponse GetInventory()
    {
        var pkiRoot = ResolvePkiRoot();
        var trustedDir = EnsureDirectory(Path.Combine(pkiRoot, "trusted"));
        var rejectedDir = EnsureDirectory(Path.Combine(pkiRoot, "rejected"));
        var ownDir = EnsureDirectory(Path.Combine(pkiRoot, "own"));

        return new CertificateInventoryResponse(
            pkiRoot,
            LoadEntries(trustedDir),
            LoadEntries(rejectedDir),
            LoadOwnEntries(ownDir));
    }

    public async Task PromoteAsync(string fileName, CancellationToken ct = default)
    {
        ValidateFileName(fileName);

        var pkiRoot = ResolvePkiRoot();
        var trustedDir = EnsureDirectory(Path.Combine(pkiRoot, "trusted"));
        var rejectedDir = EnsureDirectory(Path.Combine(pkiRoot, "rejected"));

        // Search recursively — SDK stores files under a "certs/" subdirectory.
        var sourcePath = FindFileRecursive(rejectedDir, fileName)
            ?? throw new FileNotFoundException("Certificate file not found in rejected store.", fileName);

        // Place in the "certs/" subdirectory that the OPC UA SDK expects.
        var certsDir = EnsureDirectory(Path.Combine(trustedDir, "certs"));
        var targetPath = BuildSafePath(certsDir, fileName);

        await MoveFileAsync(sourcePath, targetPath, ct).ConfigureAwait(false);
    }

    public async Task RejectAsync(string fileName, CancellationToken ct = default)
    {
        ValidateFileName(fileName);

        var pkiRoot = ResolvePkiRoot();
        var trustedDir = EnsureDirectory(Path.Combine(pkiRoot, "trusted"));
        var rejectedDir = EnsureDirectory(Path.Combine(pkiRoot, "rejected"));

        var sourcePath = FindFileRecursive(trustedDir, fileName)
            ?? throw new FileNotFoundException("Certificate file not found in trusted store.", fileName);

        var certsDir = EnsureDirectory(Path.Combine(rejectedDir, "certs"));
        var targetPath = BuildSafePath(certsDir, fileName);

        await MoveFileAsync(sourcePath, targetPath, ct).ConfigureAwait(false);
    }

    public void DeleteRejected(string fileName)
    {
        ValidateFileName(fileName);

        var pkiRoot = ResolvePkiRoot();
        var rejectedDir = EnsureDirectory(Path.Combine(pkiRoot, "rejected"));

        var filePath = FindFileRecursive(rejectedDir, fileName)
            ?? throw new FileNotFoundException("Certificate file not found in rejected store.", fileName);

        File.Delete(filePath);
    }

    private static string? FindFileRecursive(string directoryPath, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return Directory
            .EnumerateFiles(directoryPath, safeName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    internal string ResolvePkiRoot()
    {
        var configured = _config["OpcUa:PkiRootPath"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(Path.Combine(_env.ContentRootPath, configured));
        }

        return "/app/pki";
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static List<CertificateEntry> LoadEntries(string directoryPath)
    {
        // The OPC UA SDK stores certs in a "certs/" subdirectory; search recursively.
        var files = Directory
            .EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .ToList();

        var results = new List<CertificateEntry>(files.Count);
        foreach (var file in files)
            results.Add(BuildEntry(file));

        return results;
    }

    private static List<CertificateEntry> LoadOwnEntries(string directoryPath)
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cer", ".crt", ".der", ".pem", ".pfx"
        };

        var files = Directory
            .EnumerateFiles(directoryPath)
            .Where(path => allowedExtensions.Contains(Path.GetExtension(path)))
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .ToList();

        var results = new List<CertificateEntry>(files.Count);
        foreach (var file in files)
            results.Add(BuildEntry(file));

        return results;
    }

    private static CertificateEntry BuildEntry(FileInfo file)
    {
        string? subject = null, thumbprint = null;
        DateTimeOffset? notBefore = null, notAfter = null;

        try
        {
            var cert = new X509Certificate2(File.ReadAllBytes(file.FullName));
            subject = cert.Subject;
            thumbprint = cert.Thumbprint;
            notBefore = cert.NotBefore;
            notAfter = cert.NotAfter;
        }
        catch (CryptographicException) { }

        return new CertificateEntry(file.Name, file.Length, file.LastWriteTimeUtc, subject, thumbprint, notBefore, notAfter);
    }

    internal static async Task MoveFileAsync(string sourcePath, string targetPath, CancellationToken ct)
    {
        if (File.Exists(targetPath))
        {
            File.Copy(targetPath, targetPath + ".bak", overwrite: true);
            File.Delete(targetPath);
        }

        await using (var source = File.OpenRead(sourcePath))
        await using (var target = File.Create(targetPath))
        {
            await source.CopyToAsync(target, ct).ConfigureAwait(false);
        }

        File.Delete(sourcePath);
    }

    internal static string BuildSafePath(string directoryPath, string fileName)
    {
        ValidateFileName(fileName);
        return Path.Combine(directoryPath, fileName);
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal))
            throw new ArgumentException("Invalid file name.", nameof(fileName));
    }
}
