using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Tests;

public sealed class PrayerScheduleCalculatorTests
{
    private static PrayerDay Day(DateOnly date, int dayOffset = 0) => new()
    {
        Date = date.AddDays(dayOffset),
        Hijri = "1448-05-29",
        Imsak = date.AddDays(dayOffset).ToDateTime(new TimeOnly(5, 38)),
        Fajr = date.AddDays(dayOffset).ToDateTime(new TimeOnly(5, 48)),
        Syuruk = date.AddDays(dayOffset).ToDateTime(new TimeOnly(6, 57)),
        Dhuha = date.AddDays(dayOffset).ToDateTime(new TimeOnly(7, 22)),
        Dhuhr = date.AddDays(dayOffset).ToDateTime(new TimeOnly(13, 0)),
        Asr = date.AddDays(dayOffset).ToDateTime(new TimeOnly(16, 21)),
        Maghrib = date.AddDays(dayOffset).ToDateTime(new TimeOnly(18, 58)),
        Isha = date.AddDays(dayOffset).ToDateTime(new TimeOnly(20, 10))
    };

    [Fact]
    public void Afternoon_UsesZohorAsPreviousAndAsrAsNext()
    {
        var date = new DateOnly(2026, 11, 9);
        var today = Day(date);
        var now = date.ToDateTime(new TimeOnly(15, 0));

        var result = PrayerScheduleCalculator.Calculate(today, Day(date, -1), Day(date, 1), now);

        Assert.Equal(PrayerKind.Dhuhr, result.Previous!.Kind);
        Assert.Equal(PrayerKind.Asr, result.Next!.Kind);
        Assert.False(result.Next.IsTomorrow);
        Assert.Equal(TimeSpan.FromMinutes(81), result.Remaining);
        Assert.Equal(0.597, result.Progress, 3);
        Assert.Equal(5, result.TodayPrayers.Count);
    }

    [Fact]
    public void BeforeFajr_UsesYesterdayIshaForProgress()
    {
        var date = new DateOnly(2026, 11, 9);
        var today = Day(date);
        var yesterday = Day(date, -1);
        var now = date.ToDateTime(new TimeOnly(5, 0));

        var result = PrayerScheduleCalculator.Calculate(today, yesterday, Day(date, 1), now);

        Assert.Equal(PrayerKind.Isha, result.Previous!.Kind);
        Assert.Equal(yesterday.Isha, result.Previous.Time);
        Assert.Equal(PrayerKind.Fajr, result.Next!.Kind);
        Assert.Equal(TimeSpan.FromMinutes(48), result.Remaining);
        Assert.InRange(result.Progress, 0.91, 0.92);
    }

    [Fact]
    public void AfterIsha_NextIsTomorrowFajr()
    {
        var date = new DateOnly(2026, 11, 9);
        var today = Day(date);
        var tomorrow = Day(date, 1);
        var now = date.ToDateTime(new TimeOnly(21, 0));

        var result = PrayerScheduleCalculator.Calculate(today, Day(date, -1), tomorrow, now);

        Assert.Equal(PrayerKind.Isha, result.Previous!.Kind);
        Assert.Equal(PrayerKind.Fajr, result.Next!.Kind);
        Assert.True(result.Next.IsTomorrow);
        Assert.Equal(tomorrow.Fajr, result.Next.Time);
        Assert.Equal(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(48), result.Remaining);
    }

    [Fact]
    public void ExactlyAtPrayerTime_NextPrayerIsTheFollowingOne()
    {
        var date = new DateOnly(2026, 11, 9);
        var today = Day(date);
        var now = date.ToDateTime(new TimeOnly(13, 0));

        var result = PrayerScheduleCalculator.Calculate(today, Day(date, -1), Day(date, 1), now);

        Assert.Equal(PrayerKind.Dhuhr, result.Previous!.Kind);
        Assert.Equal(PrayerKind.Asr, result.Next!.Kind);
    }

    [Fact]
    public void AfterIshaWithoutTomorrow_NextIsNull()
    {
        var date = new DateOnly(2026, 11, 9);
        var today = Day(date);
        var now = date.ToDateTime(new TimeOnly(21, 0));

        var result = PrayerScheduleCalculator.Calculate(today, Day(date, -1), null, now);

        Assert.Null(result.Next);
        Assert.Equal(TimeSpan.Zero, result.Remaining);
    }
}
