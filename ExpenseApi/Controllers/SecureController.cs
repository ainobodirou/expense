using ExpenseApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExpenseApi.ViewModels.Authentication;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ExpenseApi.Controllers;

[Route("api/[controller]")]
[ApiController]

public class AuthController: ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ExpenseContext _context;
    private readonly IConfiguration _configuration; //?

    // Refresh tokens

    private readonly TokenValidationParameters _tokenValidationParameters;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ExpenseContext context,
        IConfiguration configuration,
        TokenValidationParameters tokenValidationParameters)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;

        // Refresh tokens 

        _tokenValidationParameters = tokenValidationParameters; //??
    }

    [HttpPost("register-user")]
    public async Task<IActionResult> Register([FromBody]RegisterVM payload)
    {
        var userExists = await _userManager.FindByNameAsync(payload.UserName);

        if (userExists != null)
        {
            return BadRequest("User already exists");
        }

        ApplicationUser newUser = new ApplicationUser()
        {
            Email = payload.Email,
            UserName = payload.UserName,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(newUser, payload.Password);

        if (!result.Succeeded)
        {
            return BadRequest("User was not created");
        }

        return Created(nameof(Register), "User created"); //?

        
    }

    [HttpPost("login-user")]
    public async Task<IActionResult> Login([FromBody]LoginVM payload)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Please provide all required fields.");
        }
        var user = await _userManager.FindByEmailAsync(payload.Email);

        if (user != null && await _userManager.CheckPasswordAsync(user, payload.Password))
        {
            var tokenValue = await GenerateJwtTokenAsync(user, "");

            return Ok(tokenValue);
        }
        return Unauthorized();
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequestVM payload)
    {
        try
        {
            var result = await VerifyAndGenerateTokenAsync(payload);

            if (result == null) return BadRequest("Invalid Token");

            return Ok(result);
        }
        catch (Exception ex)
        {
            
            return BadRequest(ex.Message);
        }
    }

    private async Task<AuthResultVM> VerifyAndGenerateTokenAsync(TokenRequestVM payload)
    {
        var refreshValidationParameters = _tokenValidationParameters.Clone();
        refreshValidationParameters.ValidateLifetime = false;
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        // Check 1 - Check JWT token format
        var tokenInVerification = jwtTokenHandler.ValidateToken(payload.Token, refreshValidationParameters, out var validatedToken);

        // Check 2 - Encryption algorithm

        if (validatedToken is JwtSecurityToken jwtSecurityToken)
        {
            var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
            if (result == false) return null;
        }
        // Check 3 - Validate Expiry date
        var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
        var expiryDate = UnixTimeStampToDateTimeInUTC(utcExpiryDate);
        if (expiryDate > DateTime.UtcNow) throw new Exception("Token not expired yet");

        // Check 4 - refresh token exists in the DB
        var DbRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(n => n.Token == payload.RefreshToken);
        if (DbRefreshToken == null) throw new Exception("Refresh token not in DB");
        else
        {
            //Check 5 - validate the token ID
            var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            if(DbRefreshToken.JwtId != jti) throw new Exception("Token does not match");

            // Check 6 - refresh token expiration
            if(DbRefreshToken.DateExpire <= DateTime.UtcNow) throw new Exception("Your refresh token has expired, please re-authenticate!");

            // Check 7 - refresh token revoked
            if(DbRefreshToken.IsRevoked) throw new Exception("RefreshToken is revoked!");

            // Generate new token with existing refresh token
            var dbUserData = await _userManager.FindByIdAsync(DbRefreshToken.userId);
            var newTokenResponse = GenerateJwtTokenAsync(dbUserData, payload.RefreshToken);
            return await newTokenResponse;

        }
    }

    private async Task<AuthResultVM> GenerateJwtTokenAsync(ApplicationUser user, string existingRefreshToken)
    {
        var authClaims = new List<Claim> // ?
        {
            new Claim(ClaimValueTypes.DnsName, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

        };

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SigningKey"])); //?

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            expires: DateTime.UtcNow.AddMinutes(1), // typical time 5-10 mins
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = new RefreshToken();
    
        if (string.IsNullOrEmpty(existingRefreshToken))
        {
            refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                IsRevoked = false,
                userId = user.Id,
                DateAdded = DateTime.UtcNow,
                DateExpire = DateTime.UtcNow.AddMonths(6),
                Token = Guid.NewGuid().ToString()+"-"+ Guid.NewGuid().ToString()
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        var response = new AuthResultVM()
        {
            Token = jwtToken,
            RefreshToken = string.IsNullOrEmpty(existingRefreshToken) ? refreshToken.Token : existingRefreshToken,
            ExpiresAt = token.ValidTo
        };
        return response;
    }
    private DateTime UnixTimeStampToDateTimeInUTC(long UnixTimeStamp)
    {
        var dateTimeVal = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc);
        dateTimeVal = dateTimeVal.AddSeconds(UnixTimeStamp);
        return dateTimeVal;
    }

}