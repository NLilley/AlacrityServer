using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Client;
using AlacrityCore.Services.Front;
using AlacrityCore.Utils;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController : ControllerBase
{
    private readonly IClientsFrontService _clientsFrontService;
    public ClientsController(IClientsFrontService clientsFrontService)
        => (_clientsFrontService) = (clientsFrontService);

    [HttpGet]
    public async Task<GetClientResponse> GetClient([FromQuery] GetClientRequest request)
        => new()
        {
            Client = await _clientsFrontService.GetClient(this.GetClientId())
        };


    [HttpGet("settings")]
    public async Task<GetClientSettingsResponse> GetClientSettings([FromQuery] GetClientSettingsRequest request)
        => new()
        {
            ClientSettings = await _clientsFrontService.GetClientSettings(this.GetClientId())
        };


    [HttpPost("settings")]
    public async Task<SetClientSettingResponse> SetClientSetting([FromBody] SetClientSettingRequest setting)
    {
        if (
            string.IsNullOrWhiteSpace(setting.ClientSetting.Name)
            || string.IsNullOrWhiteSpace(setting.ClientSetting.Value)
            || setting.ClientSetting.Name.Length > 100
            || setting.ClientSetting.Value.Length > 100
        )
            throw new ArgumentException("Invalid Client Setting");

        await _clientsFrontService.SetClientSetting(this.GetClientId(), setting.ClientSetting.Name, setting.ClientSetting.Value);
        return new();
    }
}
