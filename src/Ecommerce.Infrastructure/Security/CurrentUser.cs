using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Infrastructure.Security;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // "sub" is mapped to NameIdentifier by the JWT handler unless mapping is cleared.
            var value = Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    public bool IsAdmin => Role == UserRole.Admin;
}
