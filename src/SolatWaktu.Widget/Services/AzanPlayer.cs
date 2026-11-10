using System.Diagnostics;
using System.Reflection;
using NAudio.Wave;

namespace SolatWaktu.Widget.Services;

/// <summary>
/// Plays the bundled azan MP3 files. Audio is extracted from the executable
/// into %LocalAppData%\SolatWaktuWidget\audio on first use.
/// </summary>
internal sealed class AzanPlayer : IDisposable
{
    private const string ShortResource = "SolatWaktu.Widget.Assets.azan-short.mp3";
    private const string LongResource = "SolatWaktu.Widget.Assets.azan-long.mp3";

    private readonly string _shortPath;
    private readonly string _longPath;
    private WaveOutEvent? _output;
    private AudioFileReader? _reader;

    public AzanPlayer()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SolatWaktuWidget",
            "audio");

        Directory.CreateDirectory(directory);
        _shortPath = EnsureExtracted(ShortResource, Path.Combine(directory, "azan-short.mp3"));
        _longPath = EnsureExtracted(LongResource, Path.Combine(directory, "azan-long.mp3"));
    }

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    public void Play(string file, int volumePercent)
    {
        Stop();

        var path = string.Equals(file, "long", StringComparison.OrdinalIgnoreCase)
            ? _longPath
            : _shortPath;

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            _reader = new AudioFileReader(path);
            _output = new WaveOutEvent
            {
                Volume = Math.Clamp(volumePercent / 100f, 0f, 1f)
            };
            _output.Init(_reader);
            _output.Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Azan playback failed: {ex.Message}");
            Stop();
        }
    }

    public void Stop()
    {
        var output = _output;
        _output = null;
        if (output is not null)
        {
            try
            {
                output.Stop();
            }
            catch (Exception)
            {
            }

            output.Dispose();
        }

        _reader?.Dispose();
        _reader = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private static string EnsureExtracted(string resourceName, string destination)
    {
        try
        {
            if (File.Exists(destination) && new FileInfo(destination).Length > 0)
            {
                return destination;
            }

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return destination;
            }

            using var file = File.Create(destination);
            stream.CopyTo(file);
            return destination;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not extract azan audio: {ex.Message}");
            return destination;
        }
    }
}
