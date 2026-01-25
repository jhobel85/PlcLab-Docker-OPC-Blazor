using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Application.Ports;

namespace PlcLab.OPC.Adapters
{
    public class OpcSessionAdapter : IOpcSessionPort
    {
        private readonly ITelemetryContext _telemetry;
        private const string APP_NAME = "PlcLabClient";

        public OpcSessionAdapter(ITelemetryContext telemetry)
        {
            _telemetry = telemetry;
        }

        public string GetApplicationName() => APP_NAME;

        public async Task<Session> CreateSessionAsync(string discoveryUrl, bool useSecurity = true, CancellationToken ct = default)
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = APP_NAME,
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = false,
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "/app/pki",
                        SubjectName = "CN="+APP_NAME
                    },
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
                var selectedEndpoint = await CoreClientUtils.SelectEndpointAsync(
                    config,
                    discoveryUrl,
                    useSecurity,
                    15000,
                    _telemetry,
                    ct
                ) ?? throw new ServiceResultException(StatusCodes.BadConfigurationError, "No suitable endpoint found.");
                endpoint = selectedEndpoint;
            }

            var configured = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);
            var session = await new DefaultSessionFactory(_telemetry).CreateAsync(
                config,
                configured,
                updateBeforeConnect: false,
                checkDomain: useSecurity,
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
    }
}
