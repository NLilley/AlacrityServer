using AlacrityCore.Models.ReqRes.WebMessages;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("webmessages")]
public class WebMessagesController : ControllerBase
{
    private IWebMessageFrontService _webMessageFrontService;
    public WebMessagesController(IWebMessageFrontService webMessageFrontService)
        => (_webMessageFrontService) = (webMessageFrontService);

    [HttpGet]
    public async Task<GetWebMessageThreadsResponse> GetWebMessageThreads([FromQuery] GetWebMessageThreadsRequest request)
        => new()
        {
            WebMessageThreads = await _webMessageFrontService.GetWebMessageThreads(this.GetClientId())
        };

    [HttpPost]
    public async Task<SubmitMessageResponse> SubmitMessage([FromBody] SubmitMessageRequest request)
    {
        var errorMessage = await _webMessageFrontService.SubmitMessage(
            this.GetClientId(),
            request.RootMessageId,
            request.Title,
            request.Message
        );

        return new()
        {
            Succeeded = errorMessage == null,
            ErrorMessage = errorMessage
        };
    }

    [HttpPut("read")]
    public async Task<SetMessageReadResponse> SetMessageRead([FromBody] SetMessageReadRequest request)
        => new()
        {
            Succeeded = await _webMessageFrontService.SetMessageRead(this.GetClientId(), request.RootMessageId, request.IsRead)
        };

    [HttpPut("finalized")]
    public async Task<SetMessageFinalizedResponse> SetMessageFinalized([FromBody] SetMessageFinalizedRequest request)
        => new()
        {
            Succeeded = await _webMessageFrontService.SetMessageFinalized(this.GetClientId(), request.RootMessageId, request.IsFinalized)
        };
}
