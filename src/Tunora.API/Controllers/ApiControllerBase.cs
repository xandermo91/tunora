using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tunora.API.Controllers;

[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CompanyId
    {
        get
        {
            var raw = User.FindFirstValue("companyId") ?? throw new UnauthorizedAccessException("companyId claim missing.");
            return int.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("companyId claim is not a valid integer.");
        }
    }

    protected int UserId
    {
        get
        {
            var raw = User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("sub claim missing.");
            return int.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("sub claim is not a valid integer.");
        }
    }
}
