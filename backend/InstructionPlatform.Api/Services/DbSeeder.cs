using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Domain.Entities;
using InstructionPlatform.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Services;

public class DbSeeder(AppDbContext db, PasswordHashService passwordHashService)
{
    public async Task SeedAsync()
    {
        if (await db.Employees.AnyAsync(x => x.Role == UserRole.Admin))
        {
            return;
        }

        var admin = new Employee
        {
            LastName = "System",
            FirstName = "Admin",
            Department = "Administration",
            Position = "Administrator",
            Email = "admin@local.test",
            PasswordHash = passwordHashService.Hash("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true
        };

        db.Employees.Add(admin);
        await db.SaveChangesAsync();
    }
}
