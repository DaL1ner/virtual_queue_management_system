using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Converters;

/// <summary>
/// Конвертер для преобразования DateTime в UTC timestamp и обратно.
/// Преобразует DateTime с Kind=Unspecified в UTC при записи в PostgreSQL.
/// </summary>
public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : DateTime.SpecifyKind(v.ToUniversalTime(), DateTimeKind.Utc)),
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified))
    {
    }
}

/// <summary>
/// Конвертер для преобразования nullable DateTime в UTC timestamp и обратно.
/// Преобразует DateTime? с Kind=Unspecified в UTC при записи в PostgreSQL.
/// </summary>
public class NullableDateTimeUtcConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableDateTimeUtcConverter()
        : base(
            v => v.HasValue 
                ? (v.Value.Kind == DateTimeKind.Utc ? v : 
                   (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : 
                   DateTime.SpecifyKind(v.Value.ToUniversalTime(), DateTimeKind.Utc))) 
                : null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : null)
    {
    }
}
