using AlacrityCore.Enums;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using Microsoft.Extensions.Logging;

namespace AlacrityCore.Services.Front;
public interface IWebMessageFrontService
{
    Task<List<WebMessageThreadDto>> GetWebMessageThreads(int clientId);
    Task<string> SubmitMessage(int clientId, long? rootMessageId, string title, string message);
    Task<bool> SetMessageRead(int clientId, long webMessageId, bool isRead);
    Task<bool> SetMessageFinalized(int clientId, long webMessageId, bool isFinalized);
}

internal class WebMessagesFrontService : IWebMessageFrontService
{
    private readonly ILogger<WebMessagesFrontService> _logger;
    private readonly IWebMessageQuery _query;
    public WebMessagesFrontService(ILogger<WebMessagesFrontService> logger, IWebMessageQuery query)
        => (_logger, _query) = (logger, query);

    public async Task<List<WebMessageThreadDto>> GetWebMessageThreads(int clientId)
        => await _query.GetWebMessageThreads(clientId);

    public async Task<string> SubmitMessage(int clientId, long? rootMessageId, string title, string message)
    {
        if (rootMessageId != null && title != null)
            return "Cannot supply a title when replying to a thread";

        if (rootMessageId == null && title == null)
            return "New threads require a title";

        if (message == null || title?.Length > 500 || message.Length > 5000)
            return "Message details are either too long or too short";

        if (rootMessageId != null && !await _query.DoesMessageBelongsToClient(clientId, rootMessageId.Value))
            return "Cannot reply to a thread that doesn't belong to the client";

        await HandleSupportThread(clientId, rootMessageId, title, message);

        return null;
    }

    public async Task<bool> SetMessageFinalized(int clientId, long rootMessageId, bool isFinalized)
    {
        var ownsMessage = await _query.DoesMessageBelongsToClient(clientId, rootMessageId);
        if (!ownsMessage)
        {
            _logger.LogError("Client: {clientId} tried to finalize web message: {webMessageId} which does not belong to it",
                clientId, rootMessageId);
            return false;
        }

        try
        {
            await _query.SetMessageFinalized(rootMessageId, isFinalized);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to set message finalized");
            return false;
        }
    }

    public async Task<bool> SetMessageRead(int clientId, long rootMessageId, bool isRead)
    {
        var ownsMessage = await _query.DoesMessageBelongsToClient(clientId, rootMessageId);
        if (!ownsMessage)
        {
            _logger.LogError("Client: {clientId} tried to mark as read web message: {webMessageId} which does not belong to it",
                clientId, rootMessageId);
            return false;
        }

        try
        {
            await _query.SetMessageRead(rootMessageId, isRead);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to set message read");
            return false;
        }
    }

    private async Task HandleSupportThread(int clientId, long? rootMessageId, string title, string message)
    {
        var userId = await _query.GetWebMessageUserId(clientId)
            ?? throw new ArgumentException($"Cannot recover web_message_user_id for client: {clientId}");

        var newMessageId = await _query.AddMessage(
            rootMessageId: rootMessageId,
            title: title,
            message: message,
            messageKind: WebMessageKind.SupportTicket,
            ownerId: userId,
            toId: (long)SpecialWebMessageUsers.AlacritySupport,
            fromId: userId,
            read: true
        );

        if (rootMessageId == null)
        {
            var autoResponseMessage = @"Hello From Alacrity!

Your message has been received, and is in the process of being addressed by our support team.
We aim to resolve all opened tickets within a single business day.

In the meantime, you may wish to look at our help pages.
They can be accessed from the footer on this site.

If you have any other questions, don't hesitate to get in touch.

Best Wishes,

- The Alacrity Support Team";

            await _query.AddMessage(
                rootMessageId: newMessageId,
                title: title,
                message: autoResponseMessage,
                messageKind: WebMessageKind.SupportTicket,
                ownerId: userId,
                toId: userId,
                fromId: (long)SpecialWebMessageUsers.AlacritySupport
            );
        }
    }
}
