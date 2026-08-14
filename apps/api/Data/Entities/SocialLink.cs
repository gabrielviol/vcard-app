namespace Api.Data.Entities;

public class SocialLink
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }

    // Restrito no app aos valores: instagram, linkedin, twitter, tiktok, youtube, site
    public required string Platform { get; set; }
    public required string Url { get; set; }
    public int DisplayOrder { get; set; }

    public Card? Card { get; set; }
}
