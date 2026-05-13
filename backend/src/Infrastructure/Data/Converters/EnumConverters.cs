using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Domain.Enums;

namespace Infrastructure.Data.Converters;

/// <summary>
/// Конвертер для преобразования enum DistributionMode в строку верхнего регистра и обратно.
/// </summary>
public class DistributionModeToStringConverter : ValueConverter<DistributionMode, string>
{
    public DistributionModeToStringConverter()
        : base(
            v => v.ToString().ToUpperInvariant(),
            v => (DistributionMode)Enum.Parse(typeof(DistributionMode), v, ignoreCase: true))
    {
    }
}

/// <summary>
/// Конвертер для преобразования enum SessionStatus в строку верхнего регистра и обратно.
/// </summary>
public class SessionStatusToStringConverter : ValueConverter<SessionStatus, string>
{
    public SessionStatusToStringConverter()
        : base(
            v => v.ToString().ToUpperInvariant(),
            v => (SessionStatus)Enum.Parse(typeof(SessionStatus), v, ignoreCase: true))
    {
    }
}

/// <summary>
/// Конвертер для преобразования enum TicketStatus в строку верхнего регистра и обратно.
/// </summary>
public class TicketStatusToStringConverter : ValueConverter<TicketStatus, string>
{
    public TicketStatusToStringConverter()
        : base(
            v => v.ToString().ToUpperInvariant(),
            v => (TicketStatus)Enum.Parse(typeof(TicketStatus), v, ignoreCase: true))
    {
    }
}