using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.Entities;

namespace OpenProfileServer.Controllers.User;

[Authorize]
[Route("api/me/invitations")]
[ApiController]
public class InvitationController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public InvitationController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationInvitationDto>>>> GetMyInvitations()
    {
        return Ok(await _orgService.GetMyInvitationsAsync(GetUserId()));
    }

    [HttpPost("{id}/accept")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Accept(Guid id)
    {
        var result = await _orgService.RespondToInvitationAsync(GetUserId(), id, true);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/decline")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Decline(Guid id)
    {
        var result = await _orgService.RespondToInvitationAsync(GetUserId(), id, false);
        return result.Status ? Ok(result) : BadRequest(result);
    }
}