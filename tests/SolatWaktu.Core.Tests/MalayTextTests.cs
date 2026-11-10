using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Tests;

public sealed class MalayTextTests
{
    [Fact]
    public void FormatHijri_UsesMalayMonthNames()
    {
        Assert.Equal("29 Jamadilawal 1448", MalayText.FormatHijri("1448-05-29"));
        Assert.Equal("1 Ramadan 1448", MalayText.FormatHijri("1448-09-01"));
    }

    [Fact]
    public void FormatHijri_ReturnsOriginalWhenInvalid()
    {
        Assert.Equal("tidak-sah", MalayText.FormatHijri("tidak-sah"));
        Assert.Equal(string.Empty, MalayText.FormatHijri(""));
    }

    [Fact]
    public void FormatLongDate_UsesMalayWeekdayAndMonth()
    {
        var date = new DateOnly(2026, 11, 9);
        Assert.Equal("Isnin, 9 November 2026", MalayText.FormatLongDate(date));
    }

    [Fact]
    public void FormatLongDate_UsesEnglishWeekdayAndMonth()
    {
        var date = new DateOnly(2026, 11, 9);
        Assert.Equal(
            "Monday, 9 November 2026",
            MalayText.FormatLongDate(date, AppLanguage.English));
    }

    [Fact]
    public void FormatHijri_UsesEnglishMonthNames()
    {
        Assert.Equal(
            "29 Jumada al-Awwal 1448",
            MalayText.FormatHijri("1448-05-29", AppLanguage.English));
    }

    [Theory]
    [InlineData(5, 48, true, "05:48")]
    [InlineData(5, 48, false, "5:48 AM")]
    [InlineData(0, 15, false, "12:15 AM")]
    [InlineData(12, 0, false, "12:00 PM")]
    [InlineData(13, 0, false, "1:00 PM")]
    [InlineData(13, 0, true, "13:00")]
    public void FormatTime_Handles24And12HourFormats(int hour, int minute, bool use24Hour, string expected)
    {
        var time = new DateTime(2026, 11, 9, hour, minute, 0);
        Assert.Equal(expected, MalayText.FormatTime(time, use24Hour));
    }

    [Fact]
    public void FormatCountdown_FormatsHoursMinutesSeconds()
    {
        Assert.Equal("dalam 1j 42m 13s", MalayText.FormatCountdown(new TimeSpan(1, 42, 13)));
        Assert.Equal("sekarang", MalayText.FormatCountdown(TimeSpan.Zero));
        Assert.Equal("dalam 2 hari 3j 4m", MalayText.FormatCountdown(new TimeSpan(2, 3, 4, 0)));
    }

    [Theory]
    [InlineData(0, 0, 0, "00:00:00")]
    [InlineData(1, 42, 13, "01:42:13")]
    [InlineData(23, 59, 59, "23:59:59")]
    public void FormatClockCountdown_FormatsHoursMinutesSeconds(
        int hours,
        int minutes,
        int seconds,
        string expected)
    {
        Assert.Equal(expected, MalayText.FormatClockCountdown(new TimeSpan(hours, minutes, seconds)));
    }

    [Fact]
    public void FormatClockCountdown_ClampsNegativeAndKeepsLargeHours()
    {
        Assert.Equal("00:00:00", MalayText.FormatClockCountdown(TimeSpan.FromSeconds(-1)));
        Assert.Equal("01:42:13", MalayText.FormatClockCountdown(new TimeSpan(0, 1, 42, 13, 900)));
        Assert.Equal("100:00:00", MalayText.FormatClockCountdown(TimeSpan.FromHours(100)));
    }

    [Fact]
    public void FormatCountdown_UsesEnglishUnits()
    {
        Assert.Equal(
            "in 1h 42m 13s",
            MalayText.FormatCountdown(new TimeSpan(1, 42, 13), AppLanguage.English));
        Assert.Equal("now", MalayText.FormatCountdown(TimeSpan.Zero, AppLanguage.English));
        Assert.Equal(
            "in 2d 3h 4m",
            MalayText.FormatCountdown(new TimeSpan(2, 3, 4, 0), AppLanguage.English));
    }

    [Fact]
    public void ShortZoneName_UsesKnownCityNamesAndFirstDistrict()
    {
        Assert.Equal("Kuala Lumpur", MalayText.ShortZoneName(new Zone("WLY01", "W.P. Kuala Lumpur", "Kuala Lumpur")));
        Assert.Equal("Johor Bahru", MalayText.ShortZoneName(new Zone("JHR02", "Johor", "Johor Bahru, Kota Tinggi, Mersing, Kulai")));
    }
}
