
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.OPC
{
    public static class OpcConnector
    {
        public static async Task<Session> ConnectAsync(string discoveryUrl, bool useSecurity = true, CancellationToken ct = default)
        {
            // 1) Build application configuration
            var config = new ApplicationConfiguration
            {
                ApplicationName = "PlcLabClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true,
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "X509Store",
                        StorePath = "CurrentUser\\My",
                        SubjectName = "CN=PlcLabClient"
                    }
                },
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            // 2) Validate configuration (async, recommended)
            await config.ValidateAsync(ApplicationType.Client, ct).ConfigureAwait(false);

            // 3) Discover endpoints and select one manually (avoids deprecated CoreClientUtils overloads)
            using var discoveryClient = await DiscoveryClient.CreateAsync(config, new Uri(discoveryUrl), ct: ct);
            var sc = new StringCollection { discoveryUrl };
            var endpoints = await discoveryClient.GetEndpointsAsync(sc, ct).ConfigureAwait(false);
            if (endpoints == null || endpoints.Count == 0)
                throw new ServiceResultException(StatusCodes.BadNoData, "No endpoints returned by server.");

            EndpointDescription selected = useSecurity
                    ? endpoints.OrderByDescending(e => e.SecurityLevel).First()
                : endpoints.FirstOrDefault(e => e.SecurityPolicyUri == SecurityPolicies.None)
                  ?? endpoints.OrderBy(e => e.SecurityLevel).First();

            // 4) Wrap into ConfiguredEndpoint
            var configured = new ConfiguredEndpoint(null, selected);

            // 5) Identity + locales (newer overloads accept IList<string>)
            var identity = new UserIdentity(new AnonymousIdentityToken());
            var locales = new List<string> { "en-US", "cs-CZ" };

            // 6) Create session 
#pragma warning disable CS0618 // Session.Create is obsolete in v1.5, but functional
            var session = await Session.Create(
                config,
                configured,
                updateBeforeConnect: false,
                sessionName: "PlcLabSession",
                sessionTimeout: 60000u,
                identity: identity,
                preferredLocales: ["en-US", "cs-CZ"],
                ct: CancellationToken.None
            ).ConfigureAwait(false);

            // 8) (Optional) KeepAlive handler
            session.KeepAlive += (sender, e) =>
            {
                if (ServiceResult.IsBad(e.Status))
                {
                    // log / trigger reconnect
                    Console.Error.WriteLine($"KeepAlive status: {e.Status}");
                }
            };

            return session;
        }
    }
}