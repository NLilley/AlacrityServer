using AlacrityCore.Enums;

namespace AlacrityCore.Models.DTOs;
public record WebMessageDto
{
    public DateTime CreatedDate { get; set; }
    public long WebMessageId { get; set; }
    public long RootMessageId { get; set; }
    public WebMessageKind MessageKind { get; set; }
    public bool Incomming { get; set; }
    public string To { get; set; }
    public string From { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public bool Read { get; set; }
    public bool Finalized { get; set; }
}

public class WebMessageThreadDto
{    
    public List<WebMessageDto> WebMessages { get; set; }
}
