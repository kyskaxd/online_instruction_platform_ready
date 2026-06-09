using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Services;

public static class InstructionLabels
{
    public static string Category(InstructionCategory category) => category switch
    {
        InstructionCategory.FireSafety => "Пожарная безопасность",
        InstructionCategory.ElectricalSafety => "Электробезопасность",
        InstructionCategory.OccupationalSafety => "Охрана труда",
        _ => category.ToString()
    };

    public static string Type(InstructionType type) => type switch
    {
        InstructionType.Introductory => "Вводный",
        InstructionType.Primary => "Первичный на рабочем месте",
        InstructionType.Repeated => "Повторный",
        InstructionType.Unscheduled => "Внеплановый",
        InstructionType.Targeted => "Целевой",
        _ => type.ToString()
    };
}
