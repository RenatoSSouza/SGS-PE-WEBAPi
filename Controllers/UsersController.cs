using Microsoft.AspNetCore.Mvc;
using sgspe.Common.Exceptions;
using sgspe.Models.Users;
using sgspe.Services.Users;

namespace sgspe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserResponse>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetById(Guid id)
        {
           try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                return Ok(UserService.MapToResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<UserResponse>> GetByName(string name)
        {
            try
            {
                var user = await _userService.GetByNameAsync(name);
                if (user == null)
                    return NotFound(new { message = $"User with name '{name}' not found" });

                return Ok(UserService.MapToResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by name: {UserName}", name);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserResponse>> GetByEmail(string email)
        {
            try
            {
                var user = await _userService.GetByEmailAsync(email);
                if (user == null)
                    return NotFound(new { message = $"User with email '{email}' not found" });

                return Ok(UserService.MapToResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {UserEmail}", email);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
        {
             try
            {
                _logger.LogInformation("Received create user request for email: {Email}", request.Email);
                
                var user = await _userService.CreateAsync(request);
                var userResponse = UserService.MapToResponse(user);
                _logger.LogInformation("User created successfully: {UserId}", user.Id);
                return CreatedAtAction(
                    nameof(GetById), 
                    new { id = user.Id }, 
                    userResponse
                );
            }
            catch (DuplicateEmailException ex)
            {
                _logger.LogWarning("Duplicate email attempt: {Email}", request.Email);
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidUserOperationException ex)
            {
                _logger.LogError(ex, "Error creating user");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpPut("{email}")]
        public async Task<ActionResult<UserResponse>> Update(string email, UpdateUserRequest request)
        {
            try
            {
                var user = await _userService.UpdateAsync(email, request);
                return Ok(UserService.MapToResponse(user));
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning("User not found for update: {Email}", email);
                return NotFound(new { message = ex.Message });
            }
            catch (DuplicateNameException ex)
            {
                _logger.LogWarning("Duplicate name attempt: {Name}", request.Name);
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidUserOperationException ex)
            {
                _logger.LogError(ex, "Error updating user: {Email}", email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user: {Email}", email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{email}/company")]
        public async Task<ActionResult<UserResponse>> UpdateCompany(string email, UpdateUserCompanyRequest request)
        {
            try
            {
                var user = await _userService.UpdateCompanyAsync(email, request);
                return Ok(UserService.MapToResponse(user));
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning("User not found for company update: {Email}", email);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidUserOperationException ex)
            {
                _logger.LogError(ex, "Error updating user company: {Email}", email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user company: {Email}", email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }


        [HttpPost("login")]
        public async Task<ActionResult<UserResponse>> Login(LoginUserRequest request)
        {
            try
            {
                var user = await _userService.LoginAsync(request);
                if (user == null)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                return Ok(UserService.MapToResponse(user));
            }
            catch (InvalidUserOperationException ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for email: {Email}", request.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _userService.DeleteAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Attempt to delete non-existent user: {UserId}", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _logger.LogInformation("User deleted successfully: {UserId}", id);
                return NoContent();
            }
            catch (InvalidUserOperationException ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting user: {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}