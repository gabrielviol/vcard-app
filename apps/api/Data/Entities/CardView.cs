namespace Api.Data.Entities;

public class CardView
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public DateTime ViewedAt { get; set; }
    public string? Referrer { get; set; }

    public Card? Card { get; set; }
}
