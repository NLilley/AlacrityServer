using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;
public interface IAuthenticationFrontService
{
    Task<(string error, int? clientId)> Login(string username, string password);
    Task<bool> IsPasswordCorrect(int clientId, string password);
    Task<bool> ChangePassword(int clientId, string password);
}

internal class AuthenticationFrontService : IAuthenticationFrontService
{
    private readonly IAuthenticationQuery _query;
    public AuthenticationFrontService(IAuthenticationQuery query)
        => _query = query;

    public async Task<(string error, int? clientId)> Login(string username, string password)
        => await _query.Login(username, password);

    public async Task<bool> IsPasswordCorrect(int clientId, string passowrd)
        => await _query.IsPasswordCorrect(clientId, passowrd);

    public async Task<bool> ChangePassword(int clientId, string password)
        => await _query.ChangePassword(clientId, password);
}
