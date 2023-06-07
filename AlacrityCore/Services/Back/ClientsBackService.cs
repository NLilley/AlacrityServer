using AlacrityCore.Infrastructure;
using AlacrityCore.Models.ReqRes;
using AlacrityCore.Queries;
using AlacrityCore.Utils;
using System.Transactions;

namespace AlacrityCore.Services.Back;
internal interface IClientsBackService
{
    Task<int> AddClient(NewClientRequest newClient);
}

internal class ClientsBackService : IClientsBackService
{
    private readonly IALogger _logger;
    private readonly IClientsQuery _clientsQuery;
    private readonly IWebMessageUserQuery _webMessageUserQuery;
    public ClientsBackService(IALogger logger, IClientsQuery clientsQuery, IWebMessageUserQuery webMessageUserQuery)
        => (_logger, _clientsQuery, _webMessageUserQuery) = (logger, clientsQuery, webMessageUserQuery);

    public async Task<int> AddClient(NewClientRequest newClient)
    {
        var hashedPassword = AuthenticationUtil.HashPassword(newClient.Password);

        int clientId;
        {
            using var transaction = new TransactionScope();
            var webMessageUserId = await _webMessageUserQuery.AddWebMessageUser(newClient.FirstName);
            clientId = await _clientsQuery.AddClient(newClient, hashedPassword, webMessageUserId);
            transaction.Complete();
        }

        _logger.Info("Added new client: {clientId}", clientId);

        return clientId;
    }
}
