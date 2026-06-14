using System.Security.Claims;
using DroneMesh3D.Api.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace DroneMesh3D.Api.Endpoints;

public static class AuthEndpoint
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapGet("/google", (string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = $"/api/auth/google/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"
            };
            return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        });

        group.MapGet("/google/callback", async (HttpContext httpContext, ISender sender, string? returnUrl, CancellationToken ct) =>
        {
            var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }

            var googleId = result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var email = result.Principal.FindFirstValue(ClaimTypes.Email)!;
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);
            var avatar = result.Principal.FindFirstValue("urn:google:picture");

            var user = await sender.Send(new UpsertGoogleUserCommand(googleId, email, name, avatar), ct);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name ?? ""),
                new("avatar_url", user.AvatarUrl ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Results.Redirect(Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ? returnUrl : "/");
        });

        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        });

        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var email = user.FindFirstValue(ClaimTypes.Email)!;
            var name = user.FindFirstValue(ClaimTypes.Name);
            var avatarUrl = user.FindFirstValue("avatar_url");

            return Results.Ok(new { id, email, name, avatarUrl });
        }).RequireAuthorization();
    }
}
