
using Opc.Ua; 
using Opc.Ua.Client; 
using Opc.Ua.Configuration;

namespace PlcLab.OPC
{
  public class OpcUaClientFactory
  {
    public async Task<(ApplicationConfiguration, Session)> ConnectAsync(string endpointUrl, string appName = "PlcLab")
    {
      var config = new ApplicationConfiguration
      {
        ApplicationName = appName,
        ApplicationType = ApplicationType.Client,
        SecurityConfiguration = new SecurityConfiguration
        {
          ApplicationCertificate = new CertificateIdentifier
          {
            StoreType = "X509Store", StorePath = "admin" + Path.DirectorySeparatorChar + "certs", SubjectName = $"CN={appName}"
          },
          AutoAcceptUntrustedCertificates = false,
        },
        ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
      };
      await config.Validate(ApplicationType.Client);
      var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: true);
      var endpoint = new ConfiguredEndpoint(null, selectedEndpoint);
      var session = await Session.Create(config, endpoint, false, appName, 60000, new UserIdentity(new AnonymousIdentityToken()), null);
      return (config, session);
    }
  }
}
