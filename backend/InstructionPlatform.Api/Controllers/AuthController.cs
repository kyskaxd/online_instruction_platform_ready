using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Domain.Entities;
using InstructionPlatform.Api.Domain.Enums;
using InstructionPlatform.Api.Dtos;
using InstructionPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    PasswordHashService passwordHashService,
    JwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string AccessTokenCookieName = "instruction_platform_access_token";
    private const string RefreshTokenCookieName = "instruction_platform_refresh_token";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.ToLower());

        if (user is null || !user.IsActive || !passwordHashService.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Неверный email или пароль.");
        }

        await IssueTokensAsync(user);
        return Ok(new AuthResponse(user.Id, user.Email, user.Role, user.Id));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawRefreshToken)
            || string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Unauthorized("Сессия истекла. Войдите снова.");
        }

        var refreshToken = await refreshTokenService.ValidateAsync(rawRefreshToken);
        if (refreshToken?.Employee is null)
        {
            ClearAuthCookies();
            return Unauthorized("Сессия истекла. Войдите снова.");
        }

        var (newRawToken, newRefreshEntity) = await refreshTokenService.CreateAsync(refreshToken.EmployeeId);
        await refreshTokenService.RevokeAsync(refreshToken, newRefreshEntity.TokenHash);

        var accessToken = jwtTokenService.CreateToken(refreshToken.Employee);
        SetAccessTokenCookie(accessToken);
        SetRefreshTokenCookie(newRawToken, newRefreshEntity.ExpiresAt);

        var user = refreshToken.Employee;
        return Ok(new AuthResponse(user.Id, user.Email, user.Role, user.Id));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email и пароль обязательны.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest("Фамилия и имя обязательны.");
        }

        var exists = await db.Employees.AnyAsync(x => x.Email.ToLower() == request.Email.ToLower());
        if (exists)
        {
            return Conflict("Пользователь с таким email уже существует.");
        }

        if (request.Role == UserRole.Admin)
        {
            return BadRequest("Создание второго администратора запрещено.");
        }

        var department = await db.Departments.FindAsync(request.DepartmentId);
        if (department is null)
        {
            return BadRequest("Выбранный отдел не существует.");
        }

        var position = await db.Positions.FindAsync(request.PositionId);
        if (position is null)
        {
            return BadRequest("Выбранная должность не существует.");
        }

        if (position.DepartmentId != department.Id)
        {
            return BadRequest("Выбранная должность не принадлежит выбранному отделу.");
        }

        var user = new Employee
        {
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            Department = department.Name,
            DepartmentId = department.Id,
            PositionId = position.Id,
            Email = request.Email.Trim(),
            PasswordHash = passwordHashService.Hash(request.Password),
            Role = request.Role,
            HireDate = ToUtc(request.HireDate),
            IsActive = true
        };

        db.Employees.Add(user);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Me), new { }, new AuthResponse(user.Id, user.Email, user.Role, user.Id));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetUserId();
        await refreshTokenService.RevokeAllForEmployeeAsync(userId);
        ClearAuthCookies();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var userId = User.GetUserId();
        var user = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(user.Id, user.Email, user.Role, user.Id));
    }

    private async Task IssueTokensAsync(Employee user)
    {
        var accessToken = jwtTokenService.CreateToken(user);
        var (rawRefreshToken, refreshEntity) = await refreshTokenService.CreateAsync(user.Id);

        SetAccessTokenCookie(accessToken);
        SetRefreshTokenCookie(rawRefreshToken, refreshEntity.ExpiresAt);
    }

    private void SetAccessTokenCookie(string token)
    {
        var expiresHours = configuration.GetValue("Jwt:ExpiresHours", 24);
        Response.Cookies.Append(AccessTokenCookieName, token, CreateCookieOptions(DateTimeOffset.UtcNow.AddHours(expiresHours)));
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, CreateCookieOptions(new DateTimeOffset(expiresAt, TimeSpan.Zero)));
    }

    private CookieOptions CreateCookieOptions(DateTimeOffset expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/",
            Expires = expires
        };
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookieName);
        Response.Cookies.Delete(RefreshTokenCookieName);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
