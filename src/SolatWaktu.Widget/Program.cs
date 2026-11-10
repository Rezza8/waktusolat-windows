using System.Drawing;
using SolatWaktu.Core.Services;
using SolatWaktu.Widget.Localization;
using SolatWaktu.Widget.Services;
using SolatWaktu.Widget.UI;

namespace SolatWaktu.Widget;

internal static class Program
{
    private const string MutexName = @"Local\SolatWaktuWidget.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            NativeMethods.NotifyExistingInstance();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new Font("Segoe UI", 9f));

#if DEBUG
        if (Environment.GetEnvironmentVariable("SOLAT_PREVIEW_TOAST") == "1")
        {
            var workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
            using var preview = new ToastForm(
                "Maghrib",
                DateTime.Today.AddHours(18).AddMinutes(58),
                "Kuala Lumpur",
                true,
                workingArea);
            Application.Run(preview);
            return;
        }
#endif

        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        var catalog = ZoneCatalog.Default;

        if (catalog.Find(settings.ZoneCode) is null)
        {
            settings.ZoneCode = "WLY01";
        }

        if (!settingsService.Exists)
        {
            using var picker = new ZonePickerForm(catalog, settings.ZoneCode);
            if (picker.ShowDialog() == DialogResult.OK && picker.SelectedZone is not null)
            {
                settings.ZoneCode = picker.SelectedZone.JakimCode;
            }

            try
            {
                settingsService.Save(settings);
            }
            catch (Exception)
            {
                // The widget can still run with defaults if settings cannot be written.
            }
        }

        using var prayerService = new PrayerTimeService();
        using var mainForm = new MainForm(settingsService, settings, prayerService);
        Application.Run(mainForm);
    }
}
