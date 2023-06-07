using AlacrityCore.Enums.Client;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IClientsQuery
{
    Task<ClientDto> GetClient(int clientId);
    Task<int> AddClient(NewClientRequest request, string hashedPassword, long webMessageUserId);

    Task<ClientSettingsDto> GetClientSettings(int clientId);
    Task SetClientSetting(int clientId, string name, string value);
}

internal class ClientsQuery : IClientsQuery
{
    private readonly SqliteConnection _connection;
    public ClientsQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<ClientDto> GetClient(int clientId)
        => _connection.QuerySingle<ClientDto>(
            @"SELECT
                c.first_name        FirstName,
                c.other_names       OtherNames
            FROM clients c
            WHERE c.client_id = @ClientId",
            new { ClientId = clientId }
        );

    public async Task<int> AddClient(NewClientRequest request, string hashedPassword, long webMessageUserId)
    {
        var clientId = _connection.ExecuteScalar<int>(
            @"INSERT INTO clients (username, hashed_password, email, first_name, other_names, web_message_user_id)
                VALUES (@UserName, @HashedPassword, @Email, @FirstName, @OtherNames, @WebMessageUserId)
                RETURNING client_id",
            new
            {
                UserName = request.UserName.ToLowerInvariant(),
                HashedPassword = hashedPassword,
                Email = request.Email,
                FirstName = request.FirstName,
                OtherNames = request.OtherNames,
                WebMessageUserId = webMessageUserId
            }
        );

        // Insert Default Client Setting Values
        _connection.Execute(
            @"INSERT INTO client_settings (client_id, name, value) 
                VALUES
                (@ClientId, 'session_duration', '1440'),
                (@ClientId, 'visualization_quality', 'high'),
                (@ClientId, 'telemetry_preference', 'disabled')",
            new { ClientId = clientId }
        );

        return clientId;
    }

    public async Task<ClientSettingsDto> GetClientSettings(int clientId)
    {
        var settings = _connection.Query<(string name, string value)>(
            @"SELECT name, value FROM client_settings WHERE client_id = @ClientId",
            new { ClientId = clientId }
        ).ToList();

        var sessionDurationMins = settings.FirstOrDefault(s => s.name == "session_duration").value;
        var visualizationQuality = settings.FirstOrDefault(s => s.name == "visualization_quality").value;
        var isTelemetryEnabled = settings.FirstOrDefault(s => s.name == "telemetry_preference").value;

        return new ClientSettingsDto
        {
            SessionDurationMins = int.TryParse(sessionDurationMins, out int mins) ? mins : 1440,
            IsTelemetryEnabled = isTelemetryEnabled == "enabled",
            VisualizationQuality = visualizationQuality switch
            {
                "disabled" => VisualizationQuality.Disabled,
                "low" => VisualizationQuality.Low,
                _ => VisualizationQuality.High
            }
        };
    }

    private HashSet<string> _validClientSettingNames = new() { "session_duration", "visualization_quality", "telemetry_preference" };
    private HashSet<int> _validSessionDurationLengths = new()
    {
        15, 30, 60, 6 * 60, 24 * 60, 7 * 24 * 60, 30 * 24 * 60
    };
    private HashSet<string> _validVisualizationQualitities = new() { "disabled", "low", "high" };
    private HashSet<string> _validTelemetryPreferences = new() { "enabled", "disabled" };
    public async Task SetClientSetting(int clientId, string name, string value)
    {
        if (!_validClientSettingNames.Contains(name))
            throw new ArgumentException("Invalid Client Setting Name");

        if (name == "session_duration")
        {
            var duration = int.Parse(value);
            if (!_validSessionDurationLengths.Contains(duration))
                throw new ArgumentException("Invalid Session Duration Value");
        }

        if (name == "visualization_quality" && !_validVisualizationQualitities.Contains(value))
            throw new ArgumentException("Invalid Visualization Quality");

        if (name == "telementry_preference" && !_validTelemetryPreferences.Contains(value))
            throw new ArgumentException("Invalid Telementry Preference");

        _connection.Execute(
            @"INSERT INTO client_settings (client_id, name, value) 
              VALUES (@ClientId, @Name, @Value)
                ON CONFLICT (client_id, name) DO UPDATE SET value = @Value",
            new { ClientId = clientId, Name = name, Value = value }
        );
    }
}
