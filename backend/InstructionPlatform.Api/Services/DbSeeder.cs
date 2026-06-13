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

        var administrationDepartment = await db.Departments.FirstOrDefaultAsync(x => x.Name == "Administration");
        if (administrationDepartment is null)
        {
            administrationDepartment = new Department { Name = "Administration" };
            db.Departments.Add(administrationDepartment);
            await db.SaveChangesAsync();
        }

        var administratorPosition = await db.Positions.FirstOrDefaultAsync(x => x.Name == "Administrator" && x.DepartmentId == administrationDepartment.Id);
        if (administratorPosition is null)
        {
            administratorPosition = new Position { Name = "Administrator", DepartmentId = administrationDepartment.Id };
            db.Positions.Add(administratorPosition);
            await db.SaveChangesAsync();
        }

        var admin = new Employee
        {
            LastName = "System",
            FirstName = "Admin",
            Department = administrationDepartment.Name,
            DepartmentId = administrationDepartment.Id,
            PositionId = administratorPosition.Id,
            Email = "admin@local.test",
            PasswordHash = passwordHashService.Hash("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true
        };

        db.Employees.Add(admin);
        await db.SaveChangesAsync();
    }
}
