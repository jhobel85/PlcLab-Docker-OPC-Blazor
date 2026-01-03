
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.OPC
{
  /// <summary>
  ///  Use ITelemetryContext as a singleton in the DI container.
  ///   
  /// </summary>
  /// <param name="telemetry"></param>
  public class OpcUaClientFactory(ITelemetryContext telemetry) : IOpcUaClientFactory
  {
    private const string APP_NAME = "PlcLabClient";

    private readonly ITelemetryContext _telemetry = telemetry;

    public string GetApplicationName() => APP_NAME;

    public async Task<Session> CreateSessionAsync(string discoveryUrl, bool useSecurity = true, CancellationToken ct = default)
    {
      var config = new ApplicationConfiguration
      {
        ApplicationName = APP_NAME,
        ApplicationType = ApplicationType.Client,
        SecurityConfiguration = new SecurityConfiguration
        {
          AutoAcceptUntrustedCertificates = true,
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

      // 2) Validate configuration (async, recommended)
      await config.ValidateAsync(ApplicationType.Client, ct).ConfigureAwait(false);

      // 3) Discover and select endpoint from the server
      var endpointConfiguration = EndpointConfiguration.Create(config);
      endpointConfiguration.OperationTimeout = 15000;

      EndpointDescription endpoint;
      
      if (!useSecurity)
      {
        // Use async DiscoveryClient with telemetry
        var discoveryClient = await DiscoveryClient.CreateAsync(config, new Uri(discoveryUrl), DiagnosticsMasks.None, ct);
        var endpoints = await discoveryClient.GetEndpointsAsync(null, ct);
        await discoveryClient.CloseAsync();

        // Log available endpoints for debugging
        Console.WriteLine($"Found {endpoints.Count} endpoints:");
        foreach (var ep in endpoints)
        {
          Console.WriteLine($"  - SecurityMode: {ep.SecurityMode}, SecurityPolicy: {ep.SecurityPolicyUri}");
        }

        endpoint = endpoints.FirstOrDefault(e =>
            e.SecurityMode == MessageSecurityMode.None &&
            (e.SecurityPolicyUri == SecurityPolicies.None ||
             e.SecurityPolicyUri == "http://opcfoundation.org/UA/SecurityPolicy#None" ||
             string.IsNullOrEmpty(e.SecurityPolicyUri)))
          ?? throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected,
              $"Server does not support unsecured endpoint. Available: {string.Join(", ", endpoints.Select(e => $"{e.SecurityMode}/{e.SecurityPolicyUri}"))}");

        // Fix endpoint URL - replace container hostname with correct hostname for the environment
        if (endpoint.EndpointUrl.Contains("opc.tcp://") && !endpoint.EndpointUrl.Contains("opcua-refserver") && !endpoint.EndpointUrl.Contains("localhost"))
        {
          var discoveryUri = new Uri(discoveryUrl);
          string hostname = discoveryUrl.Contains("localhost") ? "localhost" : "opcua-refserver";
          var newUrl = $"opc.tcp://{hostname}:{discoveryUri.Port}{new Uri(endpoint.EndpointUrl).PathAndQuery}";
          endpoint = new EndpointDescription
          {
            EndpointUrl = newUrl,
            SecurityMode = endpoint.SecurityMode,
            SecurityPolicyUri = endpoint.SecurityPolicyUri,
            TransportProfileUri = endpoint.TransportProfileUri,
            SecurityLevel = endpoint.SecurityLevel,
            ServerCertificate = endpoint.ServerCertificate,
            Server = endpoint.Server,
            UserIdentityTokens = endpoint.UserIdentityTokens
          };
        }
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

      // 5) Identity + locales (newer overloads accept IList<string>)
      var identity = new UserIdentity(new AnonymousIdentityToken());
      var locales = new List<string> { "en-US", "cs-CZ" };

      // 6) Create session 
      //ITelemetryContext telemetry = SerilogTelemetry.Create(); // If DI not used
      var session = await new DefaultSessionFactory(_telemetry).CreateAsync(
          config,
          configured,
          updateBeforeConnect: false,
          checkDomain: useSecurity,  // Only check domain when using security
          sessionName: APP_NAME + "Session",
          sessionTimeout: 60_000u,
          identity: new UserIdentity(new AnonymousIdentityToken()),
          preferredLocales: ["en-US", "cs-CZ"],
          ct: ct
      );


      // 7) (Optional) KeepAlive handler
      session.KeepAlive += (sender, e) =>
      {
        if (ServiceResult.IsBad(e.Status))
        {
          // log / trigger reconnect
          Console.Error.WriteLine($"KeepAlive status: {e.Status}");
        }
      };

      return (Session)session;
    }

    // Browsing
    public async Task<NodeId> ResolveNodeIdAsync(Session session, string path, CancellationToken ct = default)
    {
      var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
      var currentId = ObjectIds.RootFolder;

      foreach (var part in parts)
      {
        var browseResult = await BrowseAsync(session, currentId, ct);
        var reference = browseResult.FirstOrDefault(r => r.DisplayName.Text == part);
        if (reference == null)
          throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, $"Node '{part}' not found in path '{path}'");
        currentId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
      }

      return currentId;
    }

    public async Task<ReferenceDescriptionCollection> BrowseAsync(Session session, NodeId nodeId, CancellationToken ct = default)
    {
      var browseDescription = new BrowseDescription
      {
        NodeId = nodeId,
        BrowseDirection = BrowseDirection.Forward,
        ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
        IncludeSubtypes = true,
        NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable | (uint)NodeClass.Method,
        ResultMask = (uint)BrowseResultMask.All
      };

      var browseResponse = await session.BrowseAsync(null, null, 0, new BrowseDescriptionCollection { browseDescription }, ct);
      if (StatusCode.IsBad(browseResponse.Results[0].StatusCode))
        throw new ServiceResultException(browseResponse.Results[0].StatusCode);

      return browseResponse.Results[0].References;
    }

    // Subscriptions
    public async Task<Subscription> CreateSubscriptionAsync(Session session, CancellationToken ct = default)
    {
      var subscription = new Subscription(session.DefaultSubscription)
      {
        PublishingInterval = 1000,
        KeepAliveCount = 10,
        LifetimeCount = 100,
        MaxNotificationsPerPublish = 1000,
        PublishingEnabled = true,
        TimestampsToReturn = TimestampsToReturn.Both
      };

      session.AddSubscription(subscription);
      await subscription.CreateAsync(ct);
      return subscription;
    }

    public async Task AddMonitoredItemAsync(Subscription subscription, NodeId nodeId, Action<MonitoredItem, MonitoredItemNotificationEventArgs> callback, CancellationToken ct = default)
    {
      var monitoredItem = new MonitoredItem(subscription.DefaultItem)
      {
        StartNodeId = nodeId,
        AttributeId = Attributes.Value,
        MonitoringMode = MonitoringMode.Reporting,
        SamplingInterval = 1000,
        QueueSize = 0,
        DiscardOldest = true
      };

      monitoredItem.Notification += new MonitoredItemNotificationEventHandler(callback);

      subscription.AddItem(monitoredItem);
      await subscription.ApplyChangesAsync(ct);
    }

    // Read/Write
    public async Task<object> ReadValueAsync(Session session, NodeId nodeId, CancellationToken ct = default)
    {
      var readValueId = new ReadValueId
      {
        NodeId = nodeId,
        AttributeId = Attributes.Value
      };

      var readResponse = await session.ReadAsync(null, 0, TimestampsToReturn.Neither, new ReadValueIdCollection { readValueId }, ct);
      if (StatusCode.IsBad(readResponse.Results[0].StatusCode))
        throw new ServiceResultException(readResponse.Results[0].StatusCode);

      // Return the value as object, not as Variant
      return readResponse.Results[0].Value;
    }

    public async Task WriteValueAsync(Session session, NodeId nodeId, Variant value, CancellationToken ct = default)
    {
      var writeValue = new WriteValue
      {
        NodeId = nodeId,
        AttributeId = Attributes.Value,
        Value = new DataValue { Value = value }
      };

      var writeResponse = await session.WriteAsync(null, new WriteValueCollection { writeValue }, ct);
      if (StatusCode.IsBad(writeResponse.Results[0]))
        throw new ServiceResultException(writeResponse.Results[0]);
    }

    // Methods
    public async Task<Variant[]> CallMethodAsync(Session session, NodeId objectId, NodeId methodId, Variant[] inputArgs, CancellationToken ct = default)
    {
      var callMethodRequest = new CallMethodRequest
      {
        ObjectId = objectId,
        MethodId = methodId,
        InputArguments = new VariantCollection(inputArgs)
      };

      var callResponse = await session.CallAsync(null, new CallMethodRequestCollection { callMethodRequest }, ct);
      if (StatusCode.IsBad(callResponse.Results[0].StatusCode))
        throw new ServiceResultException(callResponse.Results[0].StatusCode);

      return callResponse.Results[0].OutputArguments.ToArray();
    }
  }
}