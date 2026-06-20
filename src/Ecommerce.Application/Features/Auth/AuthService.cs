using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Features.Auth.Dtos;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new DomainException("A user with this email already exists.");

        var role = UserRole.Customer;
        if (request.Role == UserRole.Admin)
        {
            if (!_currentUser.IsAdmin)
                throw new DomainException("Only an administrator can create an admin account.");
            role = UserRole.Admin;
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expires) = _jwt.Generate(user);
        return new AuthResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), token, expires);
    }
}
