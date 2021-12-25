using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        MyDbContext dbContext,
        IConfiguration configuration,
        TokenValidationParameters tokenValidationParameters, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _tokenValidationParameters = tokenValidationParameters;
        _logger = logger;
    }

    [HttpPost("register")]
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Please, provide all required fields");
        }

        var userExists = await _userManager.FindByEmailAsync(model.Email);
        if (userExists == null || !await _userManager.CheckPasswordAsync(userExists, model.Password))
            return Unauthorized();
        var tokenValue = await GenerateJwtTokenAsync(userExists, null);
        return Ok(tokenValue);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Please, provide all required fields");
        }

        var result = await VerifyAndGenerateTokenAsync(model);
        return Ok(result);
    }


    private async Task<JwtResponseViewModel> GenerateJwtTokenAsync(ApplicationUser user, RefreshToken? refreshToken)
    {
        var authClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        //Add User Role Claims
        var userRoles = await _userManager.GetRolesAsync(user);
        authClaims.AddRange(userRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole)));


        var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

        if (refreshToken != null)
        {
            var refreshTokenResponse = new JwtResponseViewModel()
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = token.ValidTo
            };
            return refreshTokenResponse;
        }

        var newRefreshToken = new RefreshToken()
        {
            JwtId = token.Id,
            IsRevoked = false,
            UserId = user.Id,
            DateAdded = DateTime.UtcNow,
            DateExpire = DateTime.UtcNow.AddMonths(6),
            Token = Guid.NewGuid() + "-" + Guid.NewGuid()
        };
        await _dbContext.RefreshTokens.AddAsync(newRefreshToken);
        await _dbContext.SaveChangesAsync();


        var response = new JwtResponseViewModel()
        {
            Token = jwtToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = token.ValidTo
        };

        return response;

    }
    private async Task<JwtResponseViewModel> VerifyAndGenerateTokenAsync(TokenRequestViewModel model)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == model.RefreshToken);
        var dbUser = await _userManager.FindByIdAsync(storedToken?.UserId);

        try
        {
            var tokenCheckResult = jwtTokenHandler
                .ValidateToken(model.Token, _tokenValidationParameters, out var validatedToken);

            return await GenerateJwtTokenAsync(dbUser, storedToken);
        }
        catch (SecurityTokenExpiredException)
        {
            if (storedToken?.DateExpire >= DateTime.UtcNow)
            {
                return await GenerateJwtTokenAsync(dbUser, storedToken);
            }
            else
            {
                return await GenerateJwtTokenAsync(dbUser, null);
            }
        }
    }

}