using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using PlcLab.OPC;
using PlcLab.OPC.Adapters;
using PlcLab.OPC.Mock;
using Xunit;
using Xunit.Abstractions;

namespace PlcLab.Web.Tests;

public class MockOpcUaServerTests
{
    private readonly ITestOutputHelper _output;

    public MockOpcUaServerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Mock server connection is unstable - requires OPC UA stack configuration refinement")]
    public async Task MockServer_Allows_Read_Write_And_Method_Calls()
    {
        await using var server = new MockOpcUaServer();
        await server.StartAsync();
        _output.WriteLine($"Mock server started at {server.EndpointUrl}");

        var clientConfig = await CreateClientConfigAsync();
        var endpoint = new ConfiguredEndpoint(null, new EndpointDescription
        {
            EndpointUrl = server.EndpointUrl,
            SecurityMode = MessageSecurityMode.None,
            SecurityPolicyUri = SecurityPolicies.None,
            Server = new ApplicationDescription { ApplicationUri = "urn:plclab:mock:server" },
            UserIdentityTokens = new UserTokenPolicyCollection
            {
                new UserTokenPolicy(UserTokenType.Anonymous)
            }
        });

        using var session = await ConnectWithRetryAsync(async () =>
        {
            _output.WriteLine("Attempting to create session...");
            var s = await Session.Create(
                clientConfig,
                endpoint,
                updateBeforeConnect: false,
                checkDomain: false,
                "PlcLabTestSession",
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null
            ).ConfigureAwait(false);
            _output.WriteLine("Session created successfully");
            return s;
        });

        var nsIndex = session.NamespaceUris.GetIndex("urn:plclab:mock");
        Assert.NotEqual(ushort.MaxValue, nsIndex);

        var flowId = new NodeId("Flow", (ushort)nsIndex);
        var valveId = new NodeId("ValveOpen", (ushort)nsIndex);
        var processStateId = new NodeId("State", (ushort)nsIndex);
        var methodsFolderId = new NodeId("Methods", (ushort)nsIndex);
        var addMethodId = new NodeId("Add", (ushort)nsIndex);
        var resetAlarmsId = new NodeId("ResetAlarms", (ushort)nsIndex);

        // Read seeded value
        var flowValue = await session.ReadValueAsync(flowId);
        Assert.Equal(StatusCodes.Good, flowValue.StatusCode);
        Assert.True(Convert.ToDouble(flowValue.Value) >= 0.0);

        // Write boolean
        var write = new WriteValue
        {
            NodeId = valveId,
            AttributeId = Attributes.Value,
            Value = new DataValue(new Variant(true))
        };
        var writeCollection = new WriteValueCollection { write };
        var writeResponse = await session.WriteAsync(null, writeCollection, CancellationToken.None);
        Assert.True(StatusCode.IsGood(writeResponse.Results[0]));

        // Call Add method
        var addRequest = new CallMethodRequest
        {
            ObjectId = methodsFolderId,
            MethodId = addMethodId,
            InputArguments = new VariantCollection { new Variant(2.5), new Variant(4.0) }
        };
        var addResponse = await session.CallAsync(null, new CallMethodRequestCollection { addRequest }, CancellationToken.None);
        var outputs = addResponse.Results[0].OutputArguments;
        Assert.Equal(6.5, Convert.ToDouble(outputs[0]), 3);

        // Call ResetAlarms method and verify state resets
        var resetRequest = new CallMethodRequest { ObjectId = methodsFolderId, MethodId = resetAlarmsId };
        await session.CallAsync(null, new CallMethodRequestCollection { resetRequest }, CancellationToken.None);
        var stateValue = await session.ReadValueAsync(processStateId);
        var valveValue = await session.ReadValueAsync(valveId);
        Assert.Equal("Idle", stateValue.Value as string);
        Assert.False(Convert.ToBoolean(valveValue.Value));
    }

    private static async Task<Session> ConnectWithRetryAsync(Func<Task<Session>> connect, int attempts = 10, int delayMs = 1500)
    {
        ServiceResultException? last = null;
        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                return await connect();
            }
            catch (ServiceResultException ex)
            {
                last = ex;
                if (i == attempts) break;
                await Task.Delay(delayMs);
            }
        }

        if (last != null) throw last;
        throw new InvalidOperationException("Failed to connect to mock OPC UA server.");
    }

    private static async Task<ApplicationConfiguration> CreateClientConfigAsync()
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "PlcLabTestClient",
            ApplicationUri = "urn:plclab:test:client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                AutoAcceptUntrustedCertificates = true,
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = "pki/own",
                    SubjectName = "CN=PlcLabTestClient"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/trusted"
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/issuers"
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/rejected"
                }
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            TraceConfiguration = new TraceConfiguration()
        };

        await config.ValidateAsync(ApplicationType.Client).ConfigureAwait(false);
        return config;
    }
}
