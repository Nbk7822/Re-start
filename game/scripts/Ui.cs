using System.Collections.Generic;
using System.Globalization;
using Return.Sim.Content;

namespace Return.Game;

/// <summary>
/// The view's localisation helper over <see cref="ContentDb.Loc"/>. Every player-facing string in
/// Return.Game is a loc key (the view's own keys start with "ui." and live in
/// content/loc/en.ui.json); the view never composes English in code. A missing key shows as
/// ⟦key⟧ on screen, like everywhere else in Return.
/// This half of <c>Ui</c> is Godot-free so the tests compile it; UiStyle.cs holds the Godot theme.
/// </summary>
public static partial class Ui
{
    private static LocTable _loc = new(new Dictionary<string, string>());

    /// <summary>Called once by SimHost after content loads.</summary>
    public static void Init(LocTable loc) => _loc = loc;

    /// <summary>The string for a key.</summary>
    public static string T(string key) => _loc.Get(key);

    /// <summary>The template for a key with its {name} placeholders filled.</summary>
    public static string F(string key, params (string Name, string Value)[] args)
    {
        var list = new KeyValuePair<string, string>[args.Length];
        for (int i = 0; i < args.Length; i++) list[i] = new KeyValuePair<string, string>(args[i].Name, args[i].Value);
        return _loc.Format(key, list);
    }

    /// <summary>A number as invariant digits.</summary>
    public static string N(long n) => n.ToString(CultureInfo.InvariantCulture);

    /// <summary>A number with an explicit sign for positives ("+12", "-4", "0").</summary>
    public static string Signed(long n) => n > 0 ? "+" + N(n) : N(n);
}
