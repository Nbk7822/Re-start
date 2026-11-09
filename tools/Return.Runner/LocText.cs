using System.Globalization;
using Return.Sim.Content;

namespace Return.Runner;

/// <summary>
/// Helpers for runner text. Words come only from the loc table (content/loc/en.log.json for the
/// runner's own strings); C# supplies values and layout. Lines always end in "\n", whatever the
/// writer's NewLine, so logs are byte-identical on every OS.
/// </summary>
internal static class LocText
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Format(LocTable loc, string key, params (string Name, string Value)[] args)
    {
        if (args.Length == 0) return loc.Get(key);
        var pairs = new KeyValuePair<string, string>[args.Length];
        for (int i = 0; i < args.Length; i++) pairs[i] = new KeyValuePair<string, string>(args[i].Name, args[i].Value);
        return loc.Format(key, pairs);
    }

    /// <summary>Joins items with the localised list separator.</summary>
    public static string List(LocTable loc, IEnumerable<string> items) => string.Join(loc.Get("log.list_sep"), items);

    public static string Num(long n) => n.ToString(Inv);

    public static string Num(ulong n) => n.ToString(Inv);

    public static void Line(TextWriter writer, string text = "")
    {
        writer.Write(text);
        writer.Write('\n');
    }

    /// <summary>A heading underlined with <paramref name="rule"/> characters.</summary>
    public static void Heading(TextWriter writer, string text, char rule)
    {
        Line(writer, text);
        Line(writer, new string(rule, text.Length));
    }
}
