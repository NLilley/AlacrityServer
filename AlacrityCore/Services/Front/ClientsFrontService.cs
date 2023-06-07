using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;

public interface IClientsFrontService
{
    Task<ClientDto> GetClient(int clientId);
    Task<ClientSettingsDto> GetClientSettings(int clientId);
    Task SetClientSetting(int clientId, string name, string value);
}

internal class ClientsFrontService : IClientsFrontService
{
    private readonly IClientsQuery _query;
    public ClientsFrontService(IClientsQuery query)
        => _query = query;

    public async Task<ClientDto> GetClient(int clientId)
        => await _query.GetClient(clientId);

    public async Task<ClientSettingsDto> GetClientSettings(int clientId)
        => await _query.GetClientSettings(clientId);

    public async Task SetClientSetting(int clientId, string name, string value)
        => await _query.SetClientSetting(clientId, name, value);
}
