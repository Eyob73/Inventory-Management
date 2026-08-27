using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.User;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Users")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [EndpointSummary("Retrieve all users")]
    [EndpointDescription("Fetches a complete list of users registered in the system or tenant.")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("paged")]
    [EndpointSummary("Retrieve users with pagination")]
    [EndpointDescription("Fetches a paginated list of users with optional search filtering.")]
    [ProducesResponseType(typeof(PagedResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Retrieve a user by ID")]
    [EndpointDescription("Fetches detailed user profile and assigned roles by user ID.")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new { detail = "User not found." });

        return Ok(user);
    }

    [HttpPost]
    [EndpointSummary("Create a new user")]
    [EndpointDescription("Registers a new user account with initial role and tenant association.")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var (success, user, errors) = await _userService.CreateAsync(dto, cancellationToken);
        if (!success)
            return BadRequest(new { errors });

        return CreatedAtAction(nameof(GetById), new { id = user!.Id }, user);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update an existing user")]
    [EndpointDescription("Updates user profile details, tenant assignment, or assigned roles.")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var (success, user, errors) = await _userService.UpdateAsync(id, dto, cancellationToken);
        if (!success)
        {
            if (errors?.Contains("User not found.") == true)
                return NotFound(new { detail = "User not found." });

            return BadRequest(new { errors });
        }

        return Ok(user);
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a user account")]
    [EndpointDescription("Removes a user account permanently from the system.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var (success, errors) = await _userService.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            if (errors?.Contains("User not found.") == true)
                return NotFound(new { detail = "User not found." });

            return BadRequest(new { errors });
        }

        return NoContent();
    }

    [HttpPost("{id}/toggle-lock")]
    [EndpointSummary("Toggle user lockout status")]
    [EndpointDescription("Locks or unlocks a user account to grant or revoke system access.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleLock(string id, CancellationToken cancellationToken = default)
    {
        var (success, isLockedOut, errors) = await _userService.ToggleLockoutAsync(id, cancellationToken);
        if (!success)
        {
            if (errors?.Contains("User not found.") == true)
                return NotFound(new { detail = "User not found." });

            return BadRequest(new { errors });
        }

        return Ok(new { isLockedOut, message = isLockedOut ? "User account locked." : "User account unlocked." });
    }
}
