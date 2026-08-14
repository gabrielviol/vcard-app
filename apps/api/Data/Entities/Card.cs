namespace Api.Data.Entities;

public class Card
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Slug { get; set; }
    public required string FullName { get; set; }
    public string? Role { get; set; }
    public string? Company { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WhatsappNumber { get; set; }
    public string? PixKey { get; set; }

    // Restrito no app aos valores: cpf, cnpj, email, telefone, aleatoria
    public string? PixKeyType { get; set; }

    // Consentimento explicito de exposicao publica do CPF (CARD-07) — coluna deliberada,
    // nao faz parte da spec 01 original; ver 01-RESEARCH.md Security Domain.
    public bool PixConsentConfirmed { get; set; } = false;

    public bool IsBranded { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public ICollection<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();
    public ICollection<CardView> CardViews { get; set; } = new List<CardView>();
}
