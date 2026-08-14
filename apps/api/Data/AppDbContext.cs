using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<CardView> CardViews => Set<CardView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(u => u.Email).HasColumnName("email").IsRequired();
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");

            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("cards");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(c => c.Slug).HasColumnName("slug").IsRequired();
            entity.Property(c => c.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(c => c.Role).HasColumnName("role");
            entity.Property(c => c.Company).HasColumnName("company");
            entity.Property(c => c.PhotoUrl).HasColumnName("photo_url");
            entity.Property(c => c.Phone).HasColumnName("phone");
            entity.Property(c => c.Email).HasColumnName("email");
            entity.Property(c => c.WhatsappNumber).HasColumnName("whatsapp_number");
            entity.Property(c => c.PixKey).HasColumnName("pix_key");
            entity.Property(c => c.PixKeyType).HasColumnName("pix_key_type");
            entity.Property(c => c.PixConsentConfirmed).HasColumnName("pix_consent_confirmed").HasDefaultValue(false);
            entity.Property(c => c.IsBranded).HasColumnName("is_branded").HasDefaultValue(true);
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
            entity.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");

            entity.HasIndex(c => c.Slug).IsUnique();
            entity.HasIndex(c => c.UserId).IsUnique();

            entity.HasOne(c => c.User)
                .WithOne(u => u.Card)
                .HasForeignKey<Card>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SocialLink>(entity =>
        {
            entity.ToTable("social_links");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(l => l.CardId).HasColumnName("card_id").IsRequired();
            entity.Property(l => l.Platform).HasColumnName("platform").IsRequired();
            entity.Property(l => l.Url).HasColumnName("url").IsRequired();
            entity.Property(l => l.DisplayOrder).HasColumnName("display_order");

            entity.HasIndex(l => new { l.CardId, l.DisplayOrder });

            entity.HasOne(l => l.Card)
                .WithMany(c => c.SocialLinks)
                .HasForeignKey(l => l.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardView>(entity =>
        {
            entity.ToTable("card_views");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(v => v.CardId).HasColumnName("card_id").IsRequired();
            entity.Property(v => v.ViewedAt).HasColumnName("viewed_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
            entity.Property(v => v.Referrer).HasColumnName("referrer");

            entity.HasOne(v => v.Card)
                .WithMany(c => c.CardViews)
                .HasForeignKey(v => v.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
