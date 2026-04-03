using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using PlcLab.Application.Ports;

namespace PlcLab.OPC.Adapters
{
    public class OpcSessionAdapter : IOpcSessionPort
    {
        private readonly ITelemetryContext _telemetry;
        private const string APP_NAME = "PlcLabClient";
        private const string APP_URI = "urn:plclab:client:PlcLabClient";
        private const string OwnStorePath = "/app/pki/own";
        private const string ApplicationStorePath = "/app/pki/clientapp";

        public OpcSessionAdapter(ITelemetryContext telemetry)
        {
            _telemetry = telemetry;
        }

        public string GetApplicationName() => APP_NAME;

        public async Task<Session> CreateSessionAsync(string discoveryUrl, bool useSecurity = true, CancellationToken ct = default)
        {
            var appCert = new CertificateIdentifier
            {
                StoreType = "Directory",
                StorePath = ApplicationStorePath,
                SubjectName = "CN=" + APP_NAME,
                CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
            };

            var config = new ApplicationConfiguration
            {
                ApplicationName = APP_NAME,
                ApplicationUri = APP_URI,
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    // Server certificates not in the trusted store are rejected and saved to the rejected store.
                    // Users promote them via the Certificates page in the web app.
                    AutoAcceptUntrustedCertificates = false,
                    ApplicationCertificate = appCert,
                    ApplicationCertificates =
                    [
                        appCert
                    ],
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "/app/pki/trusted"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "/app/pki/trusted"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "/app/pki/rejected"
                    }
                },
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            if (useSecurity)
            {
                await EnsureApplicationCertificateAsync(config, ct).ConfigureAwait(false);
            }

            await config.ValidateAsync(ApplicationType.Client, ct).ConfigureAwait(false);

            var endpointConfiguration = EndpointConfiguration.Create(config);
            endpointConfiguration.OperationTimeout = 15000;

            EndpointDescription endpoint;
            if (!useSecurity)
            {
                var discoveryClient = await DiscoveryClient.CreateAsync(config, new Uri(discoveryUrl), DiagnosticsMasks.None, ct);
                var endpoints = await discoveryClient.GetEndpointsAsync(null, ct);
                await discoveryClient.CloseAsync(ct);
                endpoint = endpoints.FirstOrDefault(e =>
                    e.SecurityMode == MessageSecurityMode.None &&
                    (e.SecurityPolicyUri == SecurityPolicies.None ||
                     e.SecurityPolicyUri == "http://opcfoundation.org/UA/SecurityPolicy#None" ||
                     string.IsNullOrEmpty(e.SecurityPolicyUri)))
                  ?? throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected,
                      $"Server does not support unsecured endpoint. Available: {string.Join(", ", endpoints.Select(e => $"{e.SecurityMode}/{e.SecurityPolicyUri}"))}");
            }
            else
            {
                endpoint = await SelectSecureEndpointAsync(config, discoveryUrl, ct).ConfigureAwait(false);
                await EnsureCertificateAvailableForPolicyAsync(config, endpoint.SecurityPolicyUri, ct).ConfigureAwait(false);
            }

            var configured = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);
            var session = await new DefaultSessionFactory(_telemetry).CreateAsync(
                config,
                configured,
                updateBeforeConnect: useSecurity,
                checkDomain: false, // hostname not validated — server cert SAN rarely matches Docker service names
                sessionName: APP_NAME + "Session",
                sessionTimeout: 60_000u,
                identity: new UserIdentity(new AnonymousIdentityToken()),
                preferredLocales: ["en-US", "cs-CZ"],
                ct: ct
            );
            session.KeepAlive += (sender, e) =>
            {
                if (ServiceResult.IsBad(e.Status))
                {
                    Console.Error.WriteLine($"KeepAlive status: {e.Status}");
                }
            };
            return (Session)session;
        }

        private async Task EnsureApplicationCertificateAsync(ApplicationConfiguration config, CancellationToken ct)
        {
            async Task<bool> CheckAsync()
            {
                var app = new ApplicationInstance(_telemetry)
                {
                    ApplicationName = APP_NAME,
                    ApplicationType = ApplicationType.Client,
                    ApplicationConfiguration = config
                };

                return await app.CheckApplicationInstanceCertificatesAsync(
                    silent: true,
                    lifeTimeInMonths: null,
                    ct: ct).ConfigureAwait(false);
            }

            bool ok;
            try
            {
                ok = await CheckAsync().ConfigureAwait(false);
            }
            catch (ServiceResultException ex) when (IsPrivateKeyAccessFailure(ex))
            {
                ResetApplicationCertificateStore();
                ok = await CheckAsync().ConfigureAwait(false);
            }

            if (!ok)
            {
                throw new ServiceResultException(StatusCodes.BadConfigurationError, "Application certificate check failed.");
            }
        }

        private static bool IsPrivateKeyAccessFailure(Exception ex)
        {
            var message = ex.Message;
            return message.Contains("Cannot access private key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Private key", StringComparison.OrdinalIgnoreCase);
        }

        private static void ResetApplicationCertificateStore()
        {
            if (!Directory.Exists(ApplicationStorePath))
            {
                return;
            }

            foreach (var path in Directory.EnumerateFiles(ApplicationStorePath, "*", SearchOption.AllDirectories))
            {
                File.Delete(path);
            }
        }

        private async Task EnsureCertificateAvailableForPolicyAsync(ApplicationConfiguration config, string securityPolicyUri, CancellationToken ct)
        {
            var cert = await config.SecurityConfiguration.FindApplicationCertificateAsync(
                securityPolicyUri,
                privateKey: true,
                _telemetry,
                ct).ConfigureAwait(false);

            if (cert?.HasPrivateKey == true)
            {
                return;
            }

            cert = await config.SecurityConfiguration.FindApplicationCertificateAsync(
                securityPolicyUri,
                privateKey: true,
                _telemetry,
                ct).ConfigureAwait(false);

            if (cert?.HasPrivateKey != true)
            {
                throw new ServiceResultException(
                    StatusCodes.BadConfigurationError,
                    $"Application certificate with private key for security profile {securityPolicyUri} is not available in {ApplicationStorePath}.");
            }
        }

        private static async Task<EndpointDescription> SelectSecureEndpointAsync(ApplicationConfiguration config, string discoveryUrl, CancellationToken ct)
        {
            var discoveryClient = await DiscoveryClient.CreateAsync(config, new Uri(discoveryUrl), DiagnosticsMasks.None, ct).ConfigureAwait(false);
            var endpoints = await discoveryClient.GetEndpointsAsync(null, ct).ConfigureAwait(false);
            await discoveryClient.CloseAsync(ct).ConfigureAwait(false);

            var secureEndpoints = endpoints
                .Where(e => e.SecurityMode != MessageSecurityMode.None &&
                            !string.IsNullOrWhiteSpace(e.SecurityPolicyUri) &&
                            !string.Equals(e.SecurityPolicyUri, SecurityPolicies.None, StringComparison.Ordinal))
                .ToList();

            if (secureEndpoints.Count == 0)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected, "Server does not support secure endpoints.");
            }

            // Prefer secure endpoints that match a classic RSA app certificate and avoid RSA-PSS-only profile requirements.
            var preferred = secureEndpoints
                .Where(e => !e.SecurityPolicyUri.Contains("RsaPss", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.SecurityMode)
                .ThenByDescending(e => e.SecurityLevel)
                .FirstOrDefault();

            return preferred ?? secureEndpoints
                .OrderByDescending(e => e.SecurityMode)
                .ThenByDescending(e => e.SecurityLevel)
                .First();
        }
    }
}
