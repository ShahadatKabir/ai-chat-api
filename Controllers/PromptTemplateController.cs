using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/templates")]
[Authorize]
public class PromptTemplateController : ControllerBase
{
    private readonly IPromptTemplateService _templateService;

    public PromptTemplateController(IPromptTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var template = new PromptTemplate
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Template = request.Template,
            IsPublic = request.IsPublic
        };

        await _templateService.AddTemplateAsync(template);
        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplate(string id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        // Check if user owns it or it's public
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (template.UserId != userId && !template.IsPublic)
        {
            return Forbid();
        }

        return Ok(template);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserTemplates()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var templates = await _templateService.GetTemplatesByUserAsync(userId);
        return Ok(templates);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicTemplates()
    {
        var templates = await _templateService.GetPublicTemplatesAsync();
        return Ok(templates);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(string id, [FromBody] UpdateTemplateRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null || template.UserId != userId)
        {
            return NotFound();
        }

        template.Name = request.Name ?? template.Name;
        template.Description = request.Description ?? template.Description;
        template.Template = request.Template ?? template.Template;
        template.IsPublic = request.IsPublic ?? template.IsPublic;

        var success = await _templateService.UpdateTemplateAsync(template);
        if (!success)
        {
            return StatusCode(500, new { error = "Failed to update template." });
        }

        return Ok(template);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null || template.UserId != userId)
        {
            return NotFound();
        }

        var success = await _templateService.DeleteTemplateAsync(id);
        if (!success)
        {
            return StatusCode(500, new { error = "Failed to delete template." });
        }

        return NoContent();
    }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = false;
}

public class UpdateTemplateRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Template { get; set; }
    public bool? IsPublic { get; set; }
}