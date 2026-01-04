using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile.Details;

namespace OpenProfileServer.Controllers.User;

/// <summary>
/// Handles management of profile sub-resources (Work, Education, Projects, etc.).
/// </summary>
[Authorize]
[Route("api/me")]
[ApiController]
public class ProfileDetailsController : ControllerBase
{
    private readonly IProfileDetailService _detailService;

    public ProfileDetailsController(IProfileDetailService detailService)
    {
        _detailService = detailService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    // ==========================================
    // Work Experience
    // ==========================================
    
    [HttpGet("work")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkExperienceDto>>>> GetWork()
    {
        // Owner sees everything (publicOnly = false)
        return Ok(await _detailService.GetWorkAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("work")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddWork([FromBody] UpdateWorkExperienceRequestDto dto)
    {
        var result = await _detailService.AddWorkAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("work/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateWork(Guid id, [FromBody] UpdateWorkExperienceRequestDto dto)
    {
        var result = await _detailService.UpdateWorkAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("work/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteWork(Guid id)
    {
        var result = await _detailService.DeleteWorkAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // ==========================================
    // Education
    // ==========================================

    [HttpGet("education")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EducationExperienceDto>>>> GetEducation()
    {
        return Ok(await _detailService.GetEducationAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("education")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddEducation([FromBody] UpdateEducationExperienceRequestDto dto)
    {
        var result = await _detailService.AddEducationAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("education/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateEducation(Guid id, [FromBody] UpdateEducationExperienceRequestDto dto)
    {
        var result = await _detailService.UpdateEducationAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("education/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteEducation(Guid id)
    {
        var result = await _detailService.DeleteEducationAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // ==========================================
    // Projects
    // ==========================================

    [HttpGet("projects")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetProjects()
    {
        return Ok(await _detailService.GetProjectsAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("projects")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddProject([FromBody] UpdateProjectRequestDto dto)
    {
        var result = await _detailService.AddProjectAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("projects/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateProject(Guid id, [FromBody] UpdateProjectRequestDto dto)
    {
        var result = await _detailService.UpdateProjectAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("projects/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteProject(Guid id)
    {
        var result = await _detailService.DeleteProjectAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // ==========================================
    // Social Links
    // ==========================================

    [HttpGet("socials")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SocialLinkDto>>>> GetSocials()
    {
        return Ok(await _detailService.GetSocialsAsync(GetUserId()));
    }

    [HttpPost("socials")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddSocial([FromBody] UpdateSocialLinkRequestDto dto)
    {
        var result = await _detailService.AddSocialAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("socials/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSocial(Guid id, [FromBody] UpdateSocialLinkRequestDto dto)
    {
        var result = await _detailService.UpdateSocialAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("socials/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteSocial(Guid id)
    {
        var result = await _detailService.DeleteSocialAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }
    
    // ==========================================
    // Contact Methods
    // ==========================================

    [HttpGet("contacts")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContactMethodDto>>>> GetContacts()
    {
        return Ok(await _detailService.GetContactsAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("contacts")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddContact([FromBody] UpdateContactMethodRequestDto dto)
    {
        var result = await _detailService.AddContactAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("contacts/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateContact(Guid id, [FromBody] UpdateContactMethodRequestDto dto)
    {
        var result = await _detailService.UpdateContactAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("contacts/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteContact(Guid id)
    {
        var result = await _detailService.DeleteContactAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }
    
    // ==========================================
    // Certificates
    // ==========================================

    [HttpGet("certificates")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CertificateDto>>>> GetCertificates()
    {
        return Ok(await _detailService.GetCertificatesAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("certificates")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddCertificate([FromBody] UpdateCertificateRequestDto dto)
    {
        var result = await _detailService.AddCertificateAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("certificates/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateCertificate(Guid id, [FromBody] UpdateCertificateRequestDto dto)
    {
        var result = await _detailService.UpdateCertificateAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("certificates/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteCertificate(Guid id)
    {
        var result = await _detailService.DeleteCertificateAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // ==========================================
    // Sponsorships
    // ==========================================

    [HttpGet("sponsorships")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SponsorshipItemDto>>>> GetSponsorships()
    {
        return Ok(await _detailService.GetSponsorshipsAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("sponsorships")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddSponsorship([FromBody] UpdateSponsorshipItemRequestDto dto)
    {
        var result = await _detailService.AddSponsorshipAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("sponsorships/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSponsorship(Guid id, [FromBody] UpdateSponsorshipItemRequestDto dto)
    {
        var result = await _detailService.UpdateSponsorshipAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("sponsorships/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteSponsorship(Guid id)
    {
        var result = await _detailService.DeleteSponsorshipAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // ==========================================
    // Gallery
    // ==========================================

    [HttpGet("gallery")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GalleryItemDto>>>> GetGallery()
    {
        return Ok(await _detailService.GetGalleryAsync(GetUserId(), publicOnly: false));
    }

    [HttpPost("gallery")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddGalleryItem([FromBody] UpdateGalleryItemRequestDto dto)
    {
        var result = await _detailService.AddGalleryItemAsync(GetUserId(), dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("gallery/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateGalleryItem(Guid id, [FromBody] UpdateGalleryItemRequestDto dto)
    {
        var result = await _detailService.UpdateGalleryItemAsync(GetUserId(), id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("gallery/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteGalleryItem(Guid id)
    {
        var result = await _detailService.DeleteGalleryItemAsync(GetUserId(), id);
        return result.Status ? Ok(result) : NotFound(result);
    }
}
