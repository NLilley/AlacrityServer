using AlacrityCore.Utils;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IAuthenticationQuery
{
    Task<(string error, int? clientId)> Login(string username, string password);
    Task<bool> IsPasswordCorrect(int clientId, string password);
    Task<bool> ChangePassword(int clientId, string password);
}

internal class AuthenticationQuery : IAuthenticationQuery
{
    private readonly SqliteConnection _connection;
    public AuthenticationQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<(string error, int? clientId)> Login(string username, string password)
    {
        var (clientId, hashedPassword) = _connection.Query<(int clientId, string hashedPassword)>(
            @"SELECT c.client_id, c.hashed_password FROM clients c WHERE c.username = @Username",
            new { Username = username.ToLowerInvariant() }
        ).FirstOrDefault();

        if (hashedPassword == null)
            return ($"Unable to find client with username {username}", null);

        var result = AuthenticationUtil.VerifyPassword(password, hashedPassword);

        if (result == PasswordVerificationResult.Failed)
            return ("Provided password incorrect", null);
        else if (result == PasswordVerificationResult.SuccessRehashNeeded)
            SetNewHashedPassword(clientId, password);

        _connection.Execute(
            "INSERT INTO logon_history (client_id) VALUES (@ClientId)",
            new { ClientId = clientId }
        );

        return (null, clientId);
    }

    public async Task<bool> IsPasswordCorrect(int clientId, string password)
    {
        var hashedPassword = _connection.Query<string>(
            @"SELECT c.hashed_password FROM clients c WHERE c.client_id = @ClientId",
            new { ClientId = clientId }
        ).FirstOrDefault();

        return hashedPassword != null && AuthenticationUtil.VerifyPassword(password, hashedPassword) != PasswordVerificationResult.Failed;
    }

    public async Task<bool> ChangePassword(int clientId, string newPassword)
    {
        await SetNewHashedPassword(clientId, newPassword);
        return true;
    }

    private async Task SetNewHashedPassword(int clientId, string password)
    {
        var newHashedPassword = AuthenticationUtil.HashPassword(password);
        _connection.Execute(
            "UPDATE clients SET hashed_password = @NewHashedPassword WHERE client_id = @ClientId",
            new { NewHashedPassword = newHashedPassword, ClientId = clientId }
        );
    }
}
