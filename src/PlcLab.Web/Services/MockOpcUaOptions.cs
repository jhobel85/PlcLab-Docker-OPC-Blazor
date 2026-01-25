namespace PlcLab.Web.Services;

public sealed class MockOpcUaOptions
{
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "opc.tcp://localhost:4841/PlcLabMock";
}
