using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace PlcLab.OPC.Mock;

/// <summary>
/// Lightweight in-process OPC UA server for demos/tests.
/// </summary>
public sealed class MockOpcUaServer : IAsyncDisposable
{
    private const string Namespace = "urn:plclab:mock";
    private readonly string _baseAddress;
    private ApplicationInstance? _application;
    private StandardServer? _server;

    public MockOpcUaServer(string baseAddress = "opc.tcp://localhost:4841")
    {
        _baseAddress = baseAddress;
    }

    public string EndpointUrl => _baseAddress;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_server != null) return;

        var config = new ApplicationConfiguration
        {
            ApplicationName = "PlcLabMockServer",
            ApplicationUri = "urn:plclab:mock:server",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = "pki/own",
                    SubjectName = "CN=PlcLabMockServer"
                },
                AutoAcceptUntrustedCertificates = true,
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = "pki/rejected" },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "pki/trusted" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "pki/trusted" }
            },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { _baseAddress },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new() { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None }
                },
                UserTokenPolicies = new UserTokenPolicyCollection
                {
                    new UserTokenPolicy(UserTokenType.Anonymous)
                },
                MinRequestThreadCount = 1,
                MaxRequestThreadCount = 10,
                MaxQueuedRequestCount = 100
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000, MaxMessageSize = 4194304 },
            TraceConfiguration = new TraceConfiguration { OutputFilePath = "MockServer.log.txt", TraceMasks = 0 }
        };

        await config.ValidateAsync(ApplicationType.Server, ct).ConfigureAwait(false);
        config.CertificateValidator.CertificateValidation += (_, e) => e.Accept = true;

        _application = new ApplicationInstance(SerilogTelemetry.Create())
        {
            ApplicationName = config.ApplicationName,
            ApplicationType = config.ApplicationType,
            ApplicationConfiguration = config
        };

        _server = new MockStandardServer();
        await _application.StartAsync(_server).ConfigureAwait(false);

        var mock = (MockStandardServer)_server;
        mock.NodeManager.SetNamespace(Namespace);
        mock.NodeManager.Bootstrap();
        
        // Give server a moment to fully initialize endpoints
        await Task.Delay(1000, ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_server != null)
        {
            await _server.StopAsync(CancellationToken.None).ConfigureAwait(false);
            _server.Dispose();
        }
    }
}

file sealed class MockStandardServer : StandardServer
{
    internal MockNodeManager NodeManager { get; private set; } = default!;

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        NodeManager = new MockNodeManager(server, configuration, new string[0]);
        return new MasterNodeManager(server, configuration, null, new[] { NodeManager });
    }
}

file sealed class MockNodeManager : CustomNodeManager2
{
    private readonly Timer _updateTimer;
    private BaseDataVariableState? _processState;
    private BaseDataVariableState? _flow;
    private BaseDataVariableState? _valveOpen;
    private int _tick;
    private string _namespaceUri = string.Empty;

    public MockNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris)
        : base(server, configuration, namespaceUris)
    {
        SystemContext.NodeIdFactory = this;
        _updateTimer = new Timer(UpdateValues, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void SetNamespace(string ns)
    {
        _namespaceUri = ns;
        NamespaceUris = new List<string> { ns };
        SetNamespaces(new[] { ns });
    }

    public void Bootstrap()
    {
        CreateAddressSpace(null);
        _updateTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public override NodeId New(ISystemContext context, NodeState node)
    {
        var nsIndex = (ushort)Server.NamespaceUris.GetIndex(_namespaceUri);
        return new NodeId(node.BrowseName.Name, nsIndex);
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>>? externalReferences)
    {
        var objectsFolder = FindPredefinedNode(ObjectIds.ObjectsFolder, typeof(FolderState)) as FolderState;
        if (objectsFolder == null) return;

        var root = CreateFolder(objectsFolder, "PlcLab", "PlcLab");

        var process = CreateFolder(root, "Process", "Process");
        _processState = CreateVariable(process, "State", "State", DataTypeIds.String, "Idle");

        var analog = CreateFolder(root, "Analog", "Analog");
        _flow = CreateVariable(analog, "Flow", "Flow", DataTypeIds.Double, 0.0);

        var digital = CreateFolder(root, "Digital", "Digital");
        _valveOpen = CreateVariable(digital, "ValveOpen", "ValveOpen", DataTypeIds.Boolean, false);

        var methods = CreateFolder(root, "Methods", "Methods");
        CreateAddMethod(methods);
        CreateResetAlarmsMethod(methods);

        AddPredefinedNode(SystemContext, root);
    }

    private FolderState CreateFolder(NodeState parent, string name, string displayName)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = New(SystemContext, new FolderState(parent) { BrowseName = new QualifiedName(name, NamespaceIndex) }),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText(displayName),
            EventNotifier = EventNotifiers.None
        };

        parent.AddReference(ReferenceTypeIds.Organizes, false, folder.NodeId);
        folder.AddReference(ReferenceTypeIds.Organizes, true, parent.NodeId);
        return folder;
    }

    private BaseDataVariableState CreateVariable(NodeState parent, string name, string displayName, NodeId dataType, object defaultValue)
    {
        var variable = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = New(SystemContext, new BaseDataVariableState(parent) { BrowseName = new QualifiedName(name, NamespaceIndex) }),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText(displayName),
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value = defaultValue,
            StatusCode = StatusCodes.Good,
            Timestamp = DateTime.UtcNow
        };

        parent.AddReference(ReferenceTypeIds.Organizes, false, variable.NodeId);
        variable.AddReference(ReferenceTypeIds.Organizes, true, parent.NodeId);

        AddPredefinedNode(SystemContext, variable);
        return variable;
    }

    private void CreateAddMethod(NodeState parent)
    {
        var method = new MethodState(parent)
        {
            SymbolicName = "Add",
            BrowseName = new QualifiedName("Add", NamespaceIndex),
            DisplayName = new LocalizedText("Add"),
            UserExecutable = true,
            Executable = true
        };

        method.NodeId = New(SystemContext, method);

        method.InputArguments = new PropertyState<Argument[]>(method)
        {
            NodeId = New(SystemContext, method),
            BrowseName = BrowseNames.InputArguments,
            TypeDefinitionId = VariableTypeIds.PropertyType,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            DataType = DataTypeIds.Argument,
            ValueRank = ValueRanks.OneDimension,
            Value = new[]
            {
                new Argument { Name = "a", DataType = DataTypeIds.Double, ValueRank = ValueRanks.Scalar },
                new Argument { Name = "b", DataType = DataTypeIds.Double, ValueRank = ValueRanks.Scalar }
            }
        };

        method.OutputArguments = new PropertyState<Argument[]>(method)
        {
            NodeId = New(SystemContext, method),
            BrowseName = BrowseNames.OutputArguments,
            TypeDefinitionId = VariableTypeIds.PropertyType,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            DataType = DataTypeIds.Argument,
            ValueRank = ValueRanks.OneDimension,
            Value = new[] { new Argument { Name = "sum", DataType = DataTypeIds.Double, ValueRank = ValueRanks.Scalar } }
        };

        method.OnCallMethod = OnAddCall;
        AddMethod(parent, method);
    }

    private void CreateResetAlarmsMethod(NodeState parent)
    {
        var method = new MethodState(parent)
        {
            SymbolicName = "ResetAlarms",
            BrowseName = new QualifiedName("ResetAlarms", NamespaceIndex),
            DisplayName = new LocalizedText("ResetAlarms"),
            UserExecutable = true,
            Executable = true
        };

        method.NodeId = New(SystemContext, method);
        method.OnCallMethod = OnResetAlarmsCall;
        AddMethod(parent, method);
    }

    private ServiceResult OnAddCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        if (inputArguments.Count < 2) return StatusCodes.BadInvalidArgument;
        var a = Convert.ToDouble(inputArguments[0]);
        var b = Convert.ToDouble(inputArguments[1]);
        outputArguments.Add(a + b);
        return ServiceResult.Good;
    }

    private ServiceResult OnResetAlarmsCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        _processState!.Value = "Idle";
        _valveOpen!.Value = false;
        _processState.Timestamp = _valveOpen.Timestamp = DateTime.UtcNow;
        return ServiceResult.Good;
    }

    private void AddMethod(NodeState parent, MethodState method)
    {
        parent.AddReference(ReferenceTypeIds.HasComponent, false, method.NodeId);
        method.AddReference(ReferenceTypeIds.HasComponent, true, parent.NodeId);
        AddPredefinedNode(SystemContext, method);
    }

    private void UpdateValues(object? state)
    {
        _tick++;
        if (_flow != null)
        {
            _flow.Value = 1.0 + (_tick % 10) * 0.5;
            _flow.Timestamp = DateTime.UtcNow;
        }

        if (_processState != null)
        {
            _processState.Value = (_tick % 20) switch
            {
                < 5 => "Idle",
                < 10 => "Starting",
                < 15 => "Running",
                _ => "Stopping"
            };
            _processState.Timestamp = DateTime.UtcNow;
        }

        if (_valveOpen != null)
        {
            _valveOpen.Value = _tick % 8 < 4;
            _valveOpen.Timestamp = DateTime.UtcNow;
        }
    }
}
