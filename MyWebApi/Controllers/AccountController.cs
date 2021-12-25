using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyWebApi.Constants;
using MyWebApi.Models;
using MyWebApi.ViewModels;

namespace MyWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly MyDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public AccountController(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        MyDbContext dbContext,
        IConfiguration configuration,
        TokenValidationParameters tokenValidationParameters)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _tokenValidationParameters = tokenValidationParameters;
    }

    [HttpPost(Name = "register")]
    public async Task<IActionResult> Register([FromBody]RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Please, provide all the required fields");
        }

        var userExists = await _userManager.FindByEmailAsync(model.EmailAddress);
        if (userExists != null)
        {
            return BadRequest($"User {model.EmailAddress} already exists");
        }

        var newUser = new ApplicationUser()
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.EmailAddress,
            UserName = model.UserName,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var result = await _userManager.CreateAsync(newUser, model.Password);

        if (!result.Succeeded) return BadRequest("User could not be created");
        
        //Add user role
        switch (model.Role)
        {
            case ApplicationRoles.Admin:
                await _userManager.AddToRoleAsync(newUser, ApplicationRoles.Admin);
                break;
            case ApplicationRoles.User:
                await _userManager.AddToRoleAsync(newUser, ApplicationRoles.User);
                break;
            default:
                break;
        }


        return Ok("User created");
    }
}