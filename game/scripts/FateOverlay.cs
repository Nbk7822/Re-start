using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;

namespace Return.Game;

/// <summary>
/// The cards of who you are and Return by Death (the player design §7, ADR-0030), over the map and above the bar:
/// the opening card on the first tick ("You died on Earth…", Begin); a return in three frames (time stops and the map
/// darkens under how you died; a short colourless fade as the world folds back; the map returns, paused, under "You came
/// back. You remember everything." with Read what was lost and Go on), about four seconds, a click skipping to the
/// last; and the last card when your lifetime ends (Read the Chronicle, A new world). Nothing here changes the world:
/// it reads it, and asks SimHost to pause, resume, swap the shown world or start a new one.
/// </summary>
public partial class FateOverlay : Control
{
    private const double DeathSeconds = 1.8;
    private const double FoldSeconds = 1.2;
    private const float CardWidth = 470f;

    /// <summary>The card's top edge, as a share of the window's height.</summary>
    private const float CardTop = 0.12f;

    private static readonly Color DarkShade = new(0.02f, 0.02f, 0.03f, 0.62f);
    private static readonly Color PaleShade = new(0.62f, 0.62f, 0.62f, 0.86f);
    private static readonly Color LightShade = new(0.02f, 0.02f, 0.03f, 0.32f);

    private enum Phase { Hidden, Opening, Death, Fold, Back, Ended }

    private SimHost _host = null!;
    private ColorRect _shade = null!;
    private PanelContainer _card = null!;
    private VBoxContainer _lines = null!;
    private HBoxContainer _buttons = null!;
    private Label _hint = null!;
    private Phase _phase;
    private double _t;
    private bool _folded;
    private FateReturn? _return;

    /// <summary>Whether a card is up (the map does not take clicks then).</summary>
    public bool Showing => _phase != Phase.Hidden;

    public void Bind(SimHost host)
    {
        _host = host;
        Name = "FateOverlay";
        Theme = Ui.Theme;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;

        _shade = new ColorRect { Color = DarkShade, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // The card sits in the upper part of the window, so what the camera centres on (where you fell) stays in view.
        _card = new PanelContainer { Theme = Ui.Theme, MouseFilter = MouseFilterEnum.Stop };
        _card.AddThemeStyleboxOverride("panel", Ui.Box(Ui.PanelBg, Ui.Accent.Darkened(0.35f), 1, 4, 22, 18));
        AddChild(_card);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        _card.AddChild(box);
        _lines = new VBoxContainer();
        _lines.AddThemeConstantOverride("separation", 8);
        box.AddChild(_lines);
        _buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        _buttons.AddThemeConstantOverride("separation", 8);
        box.AddChild(_buttons);
        _hint = Ui.MakeLabel(Ui.T("ui.fate.skip_hint"), Ui.SmallFontSize, Ui.Dim);
        _hint.HorizontalAlignment = HorizontalAlignment.Right;
        box.AddChild(_hint);
    }

    /// <summary>The first tick: you died on Earth and woke here, as your people. Time waits for Begin.</summary>
    public void ShowOpening(World w)
    {
        Show(Phase.Opening, DarkShade, FateText.Opening(w), true);
        AddButton(Ui.T("ui.fate.begin"), () => { Close(); _host.Resume(); });
    }

    /// <summary>You died: play the return. The shown world is the lost one until the fold.</summary>
    public void ShowReturn(FateReturn r)
    {
        _return = r;
        _folded = false;
        _t = 0;
        Show(Phase.Death, DarkShade, new[] { FateText.DeathCard(r.Restored, r.Future) }, true);
        _hint.Visible = true;
    }

    /// <summary>Your lifetime ended: the last card.</summary>
    public void ShowEnded(World w)
    {
        Show(Phase.Ended, DarkShade, FateText.EndCard(w), true);
        AddButton(Ui.T("ui.fate.read_chronicle"), () => { Close(); _host.OpenChronicle(lost: false); });
        AddButton(Ui.T("ui.fate.new_world"), () => _host.NewWorld());
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            Size = GetViewportRect().Size; // it covers the whole window, under a CanvasLayer that is not a Control
            _card.Size = _card.GetCombinedMinimumSize();
            _card.Position = new Vector2(Mathf.Round((Size.X - _card.Size.X) / 2f), Mathf.Round(Size.Y * CardTop));
        }
        if (_phase is Phase.Opening or Phase.Back && !_host.Clock.Paused) Close(); // time moved on: the card is done
        if (_phase == Phase.Death)
        {
            _t += delta;
            if (_t >= DeathSeconds) BeginFold();
        }
        else if (_phase == Phase.Fold)
        {
            _t += delta;
            float k = (float)Math.Clamp(_t / FoldSeconds, 0, 1);
            // Into a colourless pale, and out of it onto the world gone back to.
            _shade.Color = k < 0.5f ? DarkShade.Lerp(PaleShade, k * 2f) : PaleShade.Lerp(LightShade, (k - 0.5f) * 2f);
            if (_t >= FoldSeconds) ShowBack();
        }
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true } && _phase is Phase.Death or Phase.Fold)
        {
            ShowBack();
            AcceptEvent();
        }
    }

    private void BeginFold()
    {
        _phase = Phase.Fold;
        _t = 0;
        _card.Visible = false;
        Fold();
    }

    private void Fold()
    {
        if (_folded) return;
        _folded = true;
        _host.CompleteReturn();
    }

    private void ShowBack()
    {
        Fold();
        Show(Phase.Back, LightShade, FateText.CameBack(_host.World), false);
        AddButton(Ui.T("ui.fate.read_lost"), () => { Close(); _host.OpenChronicle(lost: true); });
        AddButton(Ui.T("ui.fate.go_on"), () => { Close(); _host.Resume(); });
    }

    private void Show(Phase phase, Color shade, IReadOnlyList<string> lines, bool firstIsTitle)
    {
        _phase = phase;
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        _shade.Color = shade;
        _card.Visible = true;
        _hint.Visible = false;
        foreach (Node n in _lines.GetChildren()) n.QueueFree();
        foreach (Node n in _buttons.GetChildren()) n.QueueFree();
        for (int i = 0; i < lines.Count; i++)
        {
            Label label = Ui.MakeLabel(lines[i], i == 0 && firstIsTitle ? Ui.TitleFontSize : Ui.FontSize, i == 0 ? Ui.Ink : Ui.Ink.Darkened(0.08f));
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new Vector2(CardWidth, 0);
            _lines.AddChild(label);
        }
    }

    private void AddButton(string text, Action pressed)
    {
        Button b = Ui.MakeButton(text);
        b.Pressed += pressed;
        _buttons.AddChild(b);
    }

    private void Close()
    {
        _phase = Phase.Hidden;
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }
}
