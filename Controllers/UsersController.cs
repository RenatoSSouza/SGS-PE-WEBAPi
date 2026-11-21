using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Common.Exceptions;
using sgspe.Extensions;
using sgspe.Models.Enum;
using sgspe.Models.Users;
using sgspe.Services.Users;

namespace sgspe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, TokenService tokenService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginUserRequest request)
    {
        try
        {
            var user = await _userService.LoginAsync(request);
            if (user is null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            var token = _tokenService.GenerateToken(user);
            return Ok(new LoginResponse(UserService.MapToResponse(user), token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login para {Email}", request.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        // Gestor só cadastra colaboradores da própria empresa; Admin não tem restrição.
        if (!User.IsAdmin() && (request.Role != EnumRole.Employee || request.CompanyId != User.GetCompanyId()))
            return Forbid();

        try
        {
            var user = await _userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, UserService.MapToResponse(user));
        }
        catch (DuplicateEmailException ex) { return Conflict(new { message = ex.Message }); }
        catch (InvalidUserOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        if (User.IsAdmin())
        {
            var all = await _userService.GetAllAsync();
            return Ok(all.Select(UserService.MapToResponse).ToList());
        }

        var companyId = User.GetCompanyId();
        var users = await _userService.GetByCompanyAsync(companyId);
        return Ok(users.Select(UserService.MapToResponse).ToList());
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound(new { message = "Usuário não encontrado" });

        if (!User.IsAdmin() && user.CompanyId != User.GetCompanyId())
            return Forbid();

        return Ok(UserService.MapToResponse(user));
    }

    [Authorize]
    [HttpPut("{email}")]
    public async Task<ActionResult<UserResponse>> Update(string email, UpdateUserRequest request)
    {
        try
        {

            if (!User.IsAdmin() && User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value != email)
                return Forbid();

            var user = await _userService.UpdateAsync(email, request);
            return Ok(UserService.MapToResponse(user));
        }
        catch (UserNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar usuário {Email}", email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result) return NotFound(new { message = "Usuário não encontrado" });
        return NoContent();
    }
}
public record LoginResponse(UserResponse User, string Token);