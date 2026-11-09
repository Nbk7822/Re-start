using Godot;

namespace Return.Game;

/// <summary>
/// The Godot half of <see cref="Ui"/>: one calm theme shared by the bottom bar and the on-demand
/// panels. Colours are presentation only (M0 art is coloured rectangles and text).
/// </summary>
public static partial class Ui
{
    public static readonly Color Ink = new(0.93f, 0.90f, 0.85f);
    public static readonly Color Dim = new(0.63f, 0.60f, 0.55f);
    public static readonly Color Accent = new(0.85f, 0.72f, 0.43f);
    public static readonly Color PanelBg = new(0.13f, 0.125f, 0.11f, 0.95f);
    public static readonly Color Edge = new(0.32f, 0.30f, 0.26f);
    public static readonly Color DarkInk = new(0.12f, 0.11f, 0.09f);

    public const int FontSize = 14;
    public const int SmallFontSize = 12;
    public const int TitleFontSize = 18;

    private static Theme? _theme;

    public static Theme Theme => _theme ??= BuildTheme();

    public static StyleBoxFlat Box(Color bg, Color border, int borderWidth, int radius, float padX, float padY)
    {
        var box = new StyleBoxFlat { BgColor = bg, BorderColor = border };
        box.SetBorderWidthAll(borderWidth);
        box.SetCornerRadiusAll(radius);
        box.ContentMarginLeft = padX;
        box.ContentMarginRight = padX;
        box.ContentMarginTop = padY;
        box.ContentMarginBottom = padY;
        return box;
    }

    /// <summary>A label in the theme's colours; optionally another size or colour.</summary>
    public static Label MakeLabel(string text, int fontSize = FontSize, Color? color = null)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        if (fontSize != FontSize) label.AddThemeFontSizeOverride("font_size", fontSize);
        if (color is Color c) label.AddThemeColorOverride("font_color", c);
        return label;
    }

    /// <summary>A button that never takes keyboard focus, so Space and the arrow keys stay with the game.</summary>
    public static Button MakeButton(string text, string tooltip = "")
    {
        return new Button { Text = text, TooltipText = tooltip, FocusMode = Control.FocusModeEnum.None };
    }

    private static Theme BuildTheme()
    {
        var t = new Theme { DefaultFontSize = FontSize };
        t.SetStylebox("panel", "PanelContainer", Box(PanelBg, Edge, 1, 4, 12, 10));
        t.SetStylebox("panel", "TooltipPanel", Box(PanelBg, Edge, 1, 3, 8, 5));
        t.SetColor("font_color", "TooltipLabel", Ink);
        t.SetColor("font_color", "Label", Ink);

        var buttonBg = new Color(0.22f, 0.21f, 0.185f);
        t.SetStylebox("normal", "Button", Box(buttonBg, Edge, 1, 3, 10, 3));
        t.SetStylebox("hover", "Button", Box(buttonBg.Lightened(0.12f), Edge.Lightened(0.15f), 1, 3, 10, 3));
        t.SetStylebox("pressed", "Button", Box(Accent, Accent.Darkened(0.35f), 1, 3, 10, 3));
        t.SetStylebox("hover_pressed", "Button", Box(Accent.Lightened(0.1f), Accent.Darkened(0.35f), 1, 3, 10, 3));
        t.SetStylebox("disabled", "Button", Box(new Color(0.16f, 0.155f, 0.14f), new Color(0.22f, 0.21f, 0.19f), 1, 3, 10, 3));
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetColor("font_color", "Button", Ink);
        t.SetColor("font_hover_color", "Button", Colors.White);
        t.SetColor("font_pressed_color", "Button", DarkInk);
        t.SetColor("font_hover_pressed_color", "Button", DarkInk);
        t.SetColor("font_focus_color", "Button", Ink);
        t.SetColor("font_disabled_color", "Button", new Color(0.44f, 0.42f, 0.38f));

        t.SetStylebox("background", "ProgressBar", Box(new Color(0.24f, 0.23f, 0.20f), new Color(0.24f, 0.23f, 0.20f), 0, 2, 0, 0));
        t.SetStylebox("fill", "ProgressBar", Box(Accent, Accent, 0, 2, 0, 0));

        var line = new StyleBoxLine { Color = Edge, Thickness = 1, Vertical = true };
        t.SetStylebox("separator", "VSeparator", line);
        t.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = Edge, Thickness = 1 });
        return t;
    }
}
