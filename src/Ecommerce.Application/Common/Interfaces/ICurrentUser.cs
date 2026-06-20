using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Common.Interfaces;
public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    bool IsAdmin { get; }
}
