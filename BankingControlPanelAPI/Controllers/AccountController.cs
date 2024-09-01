using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,RoleManager<IdentityRole> roleManager,ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // Register controller
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during user registration for {Email}.", model.Email);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }


    // Log in  controller
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Ok();
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out for {Email}.", model.Email);
                return Forbid("User account is locked out.");
            }

            _logger.LogWarning("Invalid login attempt for {Email}.", model.Email);
            return Unauthorized("Invalid login attempt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login for {Email}.", model.Email);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }


    // Controller for Assigning roles (only for Admins)
    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentModel model)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound($"User with email {model.Email} not found.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(model.Role);
            if (!roleExists)
            {
                return BadRequest($"Role {model.Role} does not exist.");
            }

            var userRoleExists = await _userManager.IsInRoleAsync(user, model.Role);
            if (userRoleExists)
            {
                return BadRequest($"User is already in role {model.Role}.");
            }

            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to assign role {Role} to user {Email}. Errors: {Errors}", model.Role, model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            return Ok($"User {model.Email} assigned to role {model.Role} successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while assigning role to user {Email}.", model.Email);
            return StatusCode(500, "An unexpected error occurred. Please try again later.");
        }
    }


}
