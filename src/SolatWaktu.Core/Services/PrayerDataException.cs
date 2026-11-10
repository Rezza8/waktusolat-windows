namespace SolatWaktu.Core.Services;

public sealed class PrayerDataException : Exception
{
    public PrayerDataException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
