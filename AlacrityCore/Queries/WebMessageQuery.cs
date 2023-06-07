using AlacrityCore.Enums;
using AlacrityCore.Models.DTOs;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IWebMessageQuery
{
    Task<bool> DoesMessageBelongsToClient(int clientId, long webMessageId);
    Task<long?> GetWebMessageUserId(int clientId);
    Task<List<WebMessageThreadDto>> GetWebMessageThreads(int clientId);
    Task<long> AddMessage(long? rootMessageId, string title, string message, WebMessageKind messageKind, long ownerId, long toId, long fromId, bool finalized = false, bool read = false);
    Task SetMessageRead(long webMessageId, bool isRead);
    Task SetMessageFinalized(long webMessageId, bool isFinalized);
}

internal class WebMessageQuery : IWebMessageQuery
{
    public SqliteConnection _connection;
    public WebMessageQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<WebMessageThreadDto>> GetWebMessageThreads(int clientId)
    {
        var userId = await GetWebMessageUserId(clientId)
            ?? throw new ArgumentException($"Cannot recover web_message_user_id for client: {clientId}");

        var messages = _connection.Query<WebMessageDto>(
            @"SELECT 
                w.created_date              CreatedDate,
                w.web_message_id            WebMessageId,
                w.root_message_id           RootMessageId,
                w.message_kind              MessageKind,
                CASE WHEN to_id = @UserId THEN 1 ELSE 0 END Incomming,
                (SELECT wu.name FROM web_message_users wu WHERE wu.web_message_user_id = w.to_id) 'To',
                (SELECT wu.name FROM web_message_users wu WHERE wu.web_message_user_id = w.from_id) 'From',
                title                       Title,
                message                     Message,
                read                        Read,
                finalized                   Finalized
            FROM web_messages w
            WHERE w.to_id = @UserId OR w.from_id = @UserId
            ORDER BY web_message_id DESC",
            new { UserId = userId }
        );

        var threads = messages
            .GroupBy(m => m.RootMessageId)
            .Select(thread => new WebMessageThreadDto { WebMessages = thread.OrderByDescending(t => t.CreatedDate).ToList() })
            .ToList();

        return threads;
    }

    public async Task<long> AddMessage(
        long? rootMessageId,
        string title,
        string message,
        WebMessageKind messageKind,
        long ownerId,
        long toId,
        long fromId,
        bool finalized = false,
        bool read = false
    )
    {
        if (rootMessageId != null)
        {
            title ??= _connection.ExecuteScalar<string>(
                "SELECT title FROM web_messages WHERE web_message_id = @Id",
                new { Id = rootMessageId }
            );
        }

        return _connection.ExecuteScalar<long>(
            @"
            INSERT INTO web_messages
            (created_date, edited_date, root_message_id, owner_id, to_id, from_id, message_kind, title, message, finalized, read)
            VALUES
            (@Now, @Now, @RootMessageId, @OwnerId, @ToId, @FromId, @MessageKind, @Title, @Message, @Finalized, @Read)
            RETURNING web_message_id
            ",
            new
            {
                Now = DateTime.UtcNow,
                RootMessageId = rootMessageId,
                OwnerId = ownerId,
                ToId = toId,
                FromId = fromId,
                MessageKind = messageKind,
                Title = title,
                Message = message,
                Finalized = finalized ? 1 : 0,
                Read = read ? 1 : 0
            }
        );
    }

    public async Task SetMessageRead(long rootMessageId, bool isRead)
        => _connection.Execute(
            @"UPDATE web_messages 
            SET read = @IsRead 
            WHERE 
                root_message_id = @RootMessageId
                AND read != @IsRead",
            new { RootMessageId = rootMessageId, IsRead = isRead ? 1 : 0 }
        );

    public async Task SetMessageFinalized(long rootMessageId, bool isFinalized)
        => _connection.Execute(
            @"UPDATE web_messages 
            SET finalized = @IsFinalized 
            WHERE 
                root_message_id = @WebMessageId 
                AND finalized != @IsFinalized",
            new { WebMessageId = rootMessageId, IsFinalized = isFinalized ? 1 : 0 }
        );

    public async Task<bool> DoesMessageBelongsToClient(int clientId, long webMessageId)
    {
        var clientOwnerId = _connection.Query<long?>(
            @"SELECT c.client_id 
            FROM web_messages w
            INNER JOIN clients c ON c.web_message_user_id = w.owner_id
            WHERE w.web_message_id = @WebMessageId",
            new { WebMessageId = webMessageId }
        ).FirstOrDefault();

        return clientOwnerId == clientId;
    }

    public async Task<long?> GetWebMessageUserId(int clientId)
        => _connection.Query<long?>(
            "SELECT web_message_user_id FROM clients WHERE client_id = @ClientId",
            new { ClientId = clientId }
        ).FirstOrDefault();
}
