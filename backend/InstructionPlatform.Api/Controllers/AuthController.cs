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
    IWebHostEnvironment environment) : ControllerBase
{
    private const string AccessTokenCookieName = "instruction_platform_access_token";

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

        var token = jwtTokenService.CreateToken(user);
        SetAccessTokenCookie(token);

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

        var user = new Employee
        {
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            Department = department.Name,
            DepartmentId = department.Id,
            Position = request.Position.Trim(),
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
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookieName);
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

    private void SetAccessTokenCookie(string token)
    {
        Response.Cookies.Append(AccessTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddHours(12)
        });
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
