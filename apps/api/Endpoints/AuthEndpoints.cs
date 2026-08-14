using System.Security.Claims;
using System.Text.RegularExpressions;
using Api.Contracts;
using Api.Data;
using Api.Data.Entities;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", RegisterHandler);
        app.MapPost("/auth/login", LoginHandler);
        app.MapGet("/auth/me", MeHandler).RequireAuthorization();
    }

    private static async Task<IResult> RegisterHandler(RegisterRequest request, AppDbContext db, AuthService authService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex.IsMatch(request.Email))
            return Results.BadRequest(new { error = "invalid_email" });

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Results.BadRequest(new { error = "invalid_password" });

        // Normaliza para minusculas antes de comparar/persistir (CR-01) -- "email unico" so
        // vale se a comparacao ignorar caixa; sem isso "Joao@x.com" e "joao@x.com" viram
        // duas contas distintas apesar de serem o mesmo endereco.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
            return Results.Conflict(new { error = "email_taken" });

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = authService.HashPassword(request.Password),
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Results.Conflict(new { error = "email_taken" });
        }

        var token = authService.IssueToken(user);
        return Results.Created($"/auth/me", new AuthResponse(token, new UserDto(user.Id, user.Email)));
    }

    private static async Task<IResult> LoginHandler(LoginRequest request, AppDbContext db, AuthService authService)
    {
        // Mesma normalizacao do registro (CR-01) -- login precisa aceitar o email com
        // qualquer caixa que o usuario digitar, nao so a exata usada no cadastro.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || !authService.VerifyPassword(request.Password, user.PasswordHash))
            return Results.Json(new { error = "invalid_credentials" }, statusCode: StatusCodes.Status401Unauthorized);

        var token = authService.IssueToken(user);
        return Results.Ok(new AuthResponse(token, new UserDto(user.Id, user.Email)));
    }

    private static async Task<IResult> MeHandler(ClaimsPrincipal principal, AppDbContext db)
    {
        var sub = principal.FindFirstValue("sub");
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(new UserDto(user.Id, user.Email));
    }
}
