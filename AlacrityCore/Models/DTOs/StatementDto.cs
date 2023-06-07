using AlacrityCore.Enums;

namespace AlacrityCore.Models.DTOs;
public record StatementDto
{
    public long StatementId { get; set; }
    public StatementKind StatementKind { get; set; }
    public byte[] Statement { get; set; }
}
