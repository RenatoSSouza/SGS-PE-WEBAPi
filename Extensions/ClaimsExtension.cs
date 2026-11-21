using System.Security.Claims;

namespace sgspe.Extensions;

public static class ClaimsExtension
{
    public static Guid GetUserId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("UserId não encontrado no token"));

    public static Guid GetCompanyId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue("companyId")
            ?? throw new UnauthorizedAccessException("CompanyId não encontrado no token"));

    public static bool IsAdmin(this ClaimsPrincipal user)
        => user.IsInRole("Admin");

    public static bool IsManager(this ClaimsPrincipal user)
        => user.IsInRole("Manager");
}