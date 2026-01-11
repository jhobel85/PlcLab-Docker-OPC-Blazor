using Microsoft.Extensions.Hosting;
using Opc.Ua.Client;

namespace PlcLab.Infrastructure
{
    /// <summary>
    /// Generic PLC service interface for fetching data and calling various methods of various types.
    /// </summary>
    public interface IPlcService<TSession, TData> : IHostedService
        where TSession : class?
    {
        Task<TSession?> GetSessionAsync(CancellationToken cancellationToken);

        Task<TData> GetDataAsync(TSession session, CancellationToken cancellationToken);

        /// <summary>
        /// Generic method call for OPC UA methods. Accepts method name and arguments, returns result as object.
        /// </summary>
        Task<TResult> CallMethodAsync<TResult>(TSession session, string methodName, params object[] args);
    }
}
