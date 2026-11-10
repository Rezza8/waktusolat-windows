using System.Drawing;
using System.Windows.Forms;

namespace SolatWaktu.Widget.UI;

internal static class DialogPlacement
{
    /// <summary>
    /// Centers a dialog on the screen that contains its owner. If the dialog is
    /// taller or wider than the working area, it is reduced and can scroll.
    /// </summary>
    public static void CenterOnOwnerScreen(Form dialog)
    {
        var screen = dialog.Owner is { } owner
            ? Screen.FromControl(owner)
            : Screen.FromControl(dialog);
        var area = screen.WorkingArea;

        var maxWidth = Math.Max(360, area.Width - 40);
        var maxHeight = Math.Max(320, area.Height - 40);

        if (dialog.Width > maxWidth || dialog.Height > maxHeight)
        {
            dialog.AutoScroll = true;
            dialog.Size = new Size(
                Math.Min(dialog.Width, maxWidth),
                Math.Min(dialog.Height, maxHeight));
        }

        dialog.Location = new Point(
            area.Left + Math.Max(0, (area.Width - dialog.Width) / 2),
            area.Top + Math.Max(0, (area.Height - dialog.Height) / 2));
    }
}
