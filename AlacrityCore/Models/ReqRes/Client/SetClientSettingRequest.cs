namespace AlacrityCore.Models.ReqRes.Client;
public record SetClientSettingRequest
{
    public ClientSetting ClientSetting { get; set; }
}

public record ClientSetting
{
    public string Name { get; set; }
    public string Value { get; set; }
}