using System.Drawing;
using System.Windows.Forms;

namespace SolatWaktu.Widget.UI;

internal sealed class DarkColorTable : ProfessionalColorTable
{
    private static readonly Color Background = Color.FromArgb(31, 31, 46);
    private static readonly Color Border = Color.FromArgb(58, 58, 82);
    private static readonly Color Selected = Color.FromArgb(64, 64, 94);

    public override Color ToolStripDropDownBackground => Background;
    public override Color ImageMarginGradientBegin => Background;
    public override Color ImageMarginGradientMiddle => Background;
    public override Color ImageMarginGradientEnd => Background;
    public override Color MenuBorder => Border;
    public override Color MenuItemBorder => Selected;
    public override Color MenuItemSelected => Selected;
    public override Color MenuItemSelectedGradientBegin => Selected;
    public override Color MenuItemSelectedGradientEnd => Selected;
    public override Color MenuItemPressedGradientBegin => Selected;
    public override Color MenuItemPressedGradientMiddle => Selected;
    public override Color MenuItemPressedGradientEnd => Selected;
    public override Color SeparatorDark => Border;
    public override Color SeparatorLight => Border;
}

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item?.Selected == true
            ? Color.White
            : Color.FromArgb(230, 236, 240, 255);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Color.FromArgb(210, 220, 235, 255);
        base.OnRenderArrow(e);
    }
}

internal static class DarkMenuFactory
{
    public static ContextMenuStrip Create()
    {
        return new ContextMenuStrip
        {
            BackColor = Color.FromArgb(31, 31, 46),
            ForeColor = Color.FromArgb(236, 240, 255),
            Renderer = new DarkMenuRenderer(),
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(2)
        };
    }

    public static ToolStripMenuItem Item(string text, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            ForeColor = Color.FromArgb(236, 240, 255),
            Padding = new Padding(6, 3, 6, 3)
        };
        item.Click += onClick;
        return item;
    }
}
