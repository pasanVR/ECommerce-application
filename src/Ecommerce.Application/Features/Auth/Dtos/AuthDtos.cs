using System.ComponentModel.DataAnnotations;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Features.Auth.Dtos;

public record RegisterRequest
{
    [Required, MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    public UserRole? Role { get; init; }
}

public record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAtUtc);
