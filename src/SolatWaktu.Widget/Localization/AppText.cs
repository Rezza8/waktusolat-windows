using System.Globalization;
using SolatWaktu.Core.Models;

namespace SolatWaktu.Widget.Localization;

/// <summary>
/// Central UI text catalogue. Prayer names stay in their Malaysian form in both
/// languages; dates, countdowns, status messages and interface labels change.
/// </summary>
internal static class AppText
{
    private static readonly IReadOnlyDictionary<string, string> Malay =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["common.tomorrow"] = "Esok",
            ["common.waitingData"] = "Menunggu data",
            ["common.retry"] = "Cuba semula",
            ["common.save"] = "Simpan",
            ["common.cancel"] = "Batal",
            ["common.select"] = "Pilih",
            ["common.off"] = "Mati",
            ["common.left"] = "Kiri",
            ["common.center"] = "Tengah",
            ["common.right"] = "Kanan",
            ["common.short"] = "Pendek",
            ["common.long"] = "Panjang",
            ["common.small"] = "Kecil",
            ["common.medium"] = "Sederhana",
            ["common.large"] = "Besar",
            ["common.format24"] = "24 jam",
            ["common.format12"] = "12 jam",
            ["common.testNotification"] = "Uji notifikasi",
            ["common.testSound"] = "Uji bunyi",
            ["common.stopSound"] = "Hentikan bunyi",
            ["common.source"] = "Sumber: JAKIM",
            ["common.updated"] = "Dikemas kini",
            ["common.offline"] = "Mod offline",
            ["common.updateFailed"] = "Kemas kini gagal",
            ["common.trayTitle"] = "Waktu Solat",
            ["common.errorTitle"] = "Solat Waktu",

            ["main.hero.next"] = "SETERUSNYA",
            ["main.hero.nextTomorrow"] = "SETERUSNYA  •  ESOK",
            ["main.hero.refresh"] = "Sila segarkan data",
            ["main.loading.title"] = "Memuatkan waktu solat...",
            ["main.loading.body"] = "Menyambung ke sumber JAKIM",
            ["main.error.title"] = "Tidak dapat memuatkan data",
            ["main.error.body"] = "Sila semak sambungan internet anda.",
            ["main.error.load"] = "Tidak dapat memuatkan waktu solat.",
            ["main.status.updated"] = "Dikemas kini  {0}",
            ["main.status.offline"] = "Mod offline",
            ["main.status.offlineUpdated"] = "Mod offline  •  {0}",
            ["main.tray.noNext"] = "Waktu Solat  •  {0}",
            ["main.menu.show"] = "Tunjukkan Widget",
            ["main.menu.refresh"] = "Segarkan Data",
            ["main.menu.changeZone"] = "Tukar Zon...",
            ["main.menu.settings"] = "Tetapan...",
            ["main.menu.startWithWindows"] = "Mula bersama Windows",
            ["main.menu.exit"] = "Keluar",
            ["main.menu.hide"] = "Sembunyi Widget",
            ["main.balloon.title"] = "Widget masih berjalan",
            ["main.balloon.text"] = "Klik dua kali ikon tray untuk membuka widget waktu solat.",
            ["main.error.saveSettings"] = "Tetapan tidak dapat disimpan. Sila semak kebenaran folder.",

            ["settings.title"] = "Tetapan",
            ["settings.subtitle"] = "Widget waktu solat Malaysia  •  Perubahan dipratonton serta-merta",
            ["settings.section.zone"] = "ZON",
            ["settings.section.language"] = "BAHASA",
            ["settings.section.display"] = "PAPARAN",
            ["settings.theme"] = "Tema",
            ["settings.timeFormat"] = "Format masa",
            ["settings.size"] = "Saiz",
            ["settings.opacity"] = "Kegelapan widget",
            ["settings.alwaysOnTop"] = "Sentiasa di atas",
            ["settings.startWithWindows"] = "Mula bersama Windows",
            ["settings.showImsak"] = "Tunjuk Imsak",
            ["settings.showDhuha"] = "Tunjuk Dhuha",
            ["settings.taskbar"] = "Timer di taskbar",
            ["settings.taskbarHint"] = "Kiraan detik ke solat seterusnya",
            ["settings.section.notifications"] = "NOTIFIKASI",
            ["settings.notifications"] = "Notifikasi waktu solat",
            ["settings.azan"] = "Main azan",
            ["settings.azanTimes"] = "Waktu azan",
            ["settings.volume"] = "Kelantangan azan",
            ["settings.azanType"] = "Jenis azan",
            ["settings.error.audio"] = "Fail audio azan tidak dapat dimainkan.",
            ["settings.language.malay"] = "Bahasa Melayu",
            ["settings.language.english"] = "English",

            ["zone.title"] = "Pilih Zon",
            ["zone.subtitle"] = "Pilih negeri dan daerah mengikut kod JAKIM.",
            ["zone.search"] = "Cari negeri, daerah atau kod...",
            ["zone.none"] = "Tiada zon ditemui.",
            ["zone.found"] = "{0} zon dijumpai",

            ["toast.title"] = "Waktu {0}",
            ["toast.open"] = "Klik untuk membuka widget",
            ["toast.test"] = "Ujian notifikasi  •  Klik untuk tutup"
        };

    private static readonly IReadOnlyDictionary<string, string> English =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["common.tomorrow"] = "Tomorrow",
            ["common.waitingData"] = "Waiting for data",
            ["common.retry"] = "Retry",
            ["common.save"] = "Save",
            ["common.cancel"] = "Cancel",
            ["common.select"] = "Select",
            ["common.off"] = "Off",
            ["common.left"] = "Left",
            ["common.center"] = "Center",
            ["common.right"] = "Right",
            ["common.short"] = "Short",
            ["common.long"] = "Long",
            ["common.small"] = "Small",
            ["common.medium"] = "Medium",
            ["common.large"] = "Large",
            ["common.format24"] = "24-hour",
            ["common.format12"] = "12-hour",
            ["common.testNotification"] = "Test notification",
            ["common.testSound"] = "Test sound",
            ["common.stopSound"] = "Stop sound",
            ["common.source"] = "Source: JAKIM",
            ["common.updated"] = "Updated",
            ["common.offline"] = "Offline mode",
            ["common.updateFailed"] = "Update failed",
            ["common.trayTitle"] = "Prayer Times",
            ["common.errorTitle"] = "Solat Waktu",

            ["main.hero.next"] = "NEXT",
            ["main.hero.nextTomorrow"] = "NEXT  •  TOMORROW",
            ["main.hero.refresh"] = "Please refresh data",
            ["main.loading.title"] = "Loading prayer times...",
            ["main.loading.body"] = "Connecting to JAKIM",
            ["main.error.title"] = "Unable to load data",
            ["main.error.body"] = "Please check your internet connection.",
            ["main.error.load"] = "Unable to load prayer times.",
            ["main.status.updated"] = "Updated  {0}",
            ["main.status.offline"] = "Offline mode",
            ["main.status.offlineUpdated"] = "Offline mode  •  {0}",
            ["main.tray.noNext"] = "Prayer Times  •  {0}",
            ["main.menu.show"] = "Show Widget",
            ["main.menu.refresh"] = "Refresh Data",
            ["main.menu.changeZone"] = "Change Zone...",
            ["main.menu.settings"] = "Settings...",
            ["main.menu.startWithWindows"] = "Start with Windows",
            ["main.menu.exit"] = "Exit",
            ["main.menu.hide"] = "Hide Widget",
            ["main.balloon.title"] = "Widget is still running",
            ["main.balloon.text"] = "Double-click the tray icon to open the prayer widget.",
            ["main.error.saveSettings"] = "Settings could not be saved. Please check folder permissions.",

            ["settings.title"] = "Settings",
            ["settings.subtitle"] = "Malaysian prayer times widget  •  Changes preview instantly",
            ["settings.section.zone"] = "ZONE",
            ["settings.section.language"] = "LANGUAGE",
            ["settings.section.display"] = "DISPLAY",
            ["settings.theme"] = "Theme",
            ["settings.timeFormat"] = "Time format",
            ["settings.size"] = "Size",
            ["settings.opacity"] = "Widget opacity",
            ["settings.alwaysOnTop"] = "Always on top",
            ["settings.startWithWindows"] = "Start with Windows",
            ["settings.showImsak"] = "Show Imsak",
            ["settings.showDhuha"] = "Show Dhuha",
            ["settings.taskbar"] = "Timer on taskbar",
            ["settings.taskbarHint"] = "Live countdown to the next prayer",
            ["settings.section.notifications"] = "NOTIFICATIONS",
            ["settings.notifications"] = "Prayer time notifications",
            ["settings.azan"] = "Play azan",
            ["settings.azanTimes"] = "Azan times",
            ["settings.volume"] = "Azan volume",
            ["settings.azanType"] = "Azan type",
            ["settings.error.audio"] = "The azan audio file could not be played.",
            ["settings.language.malay"] = "Bahasa Melayu",
            ["settings.language.english"] = "English",

            ["zone.title"] = "Select Zone",
            ["zone.subtitle"] = "Select a state and district by JAKIM code.",
            ["zone.search"] = "Search state, district, or code...",
            ["zone.none"] = "No zones found.",
            ["zone.found"] = "{0} zones found",

            ["toast.title"] = "{0} Prayer Time",
            ["toast.open"] = "Click to open widget",
            ["toast.test"] = "Test notification  •  Click to close"
        };

    static AppText()
    {
        foreach (var pair in Malay)
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                throw new InvalidOperationException($"Empty Malay text for '{pair.Key}'.");
            }

            if (!English.TryGetValue(pair.Key, out var english) || string.IsNullOrWhiteSpace(english))
            {
                throw new InvalidOperationException($"Missing English text for '{pair.Key}'.");
            }
        }

        foreach (var key in English.Keys)
        {
            if (!Malay.ContainsKey(key))
            {
                throw new InvalidOperationException($"Missing Malay text for '{key}'.");
            }
        }
    }

    public static AppLanguage Language { get; private set; } = AppLanguage.Malay;

    public static void SetLanguage(AppLanguage language) => Language = language;

    public static string Get(string key) =>
        (Language == AppLanguage.English ? English : Malay)
        .TryGetValue(key, out var value)
            ? value
            : key;

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);
}
