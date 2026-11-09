using System.Globalization;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Calendar = Return.Sim.Core.Calendar;

namespace Return.Sim.Api;

/// <summary>Turns events into localised sentences. All English lives in content/loc.</summary>
public static class EventText
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>
    /// A character placeholder that does not open the sentence uses the name's "_mid" variant when the
    /// loc table has one (so the otherworlder reads "You asked…" but "…asked you"), keeping grammar in
    /// the loc files rather than in code.
    /// </summary>
    public static string Describe(World w, SimEvent e)
    {
        LocTable loc = w.Content.Loc;
        string template = loc.Get(e.Key);
        var args = new List<KeyValuePair<string, string>>(e.Args.Count);
        foreach (EventArg a in e.Args)
        {
            string value = Resolve(w, a.Value);
            if (a.Value.StartsWith("c:", StringComparison.Ordinal) && !template.StartsWith("{" + a.Name + "}", StringComparison.Ordinal)
                && int.TryParse(a.Value.AsSpan(2), NumberStyles.Integer, Inv, out int id) && w.Find(id) is { } c && loc.Has(c.NameKey + "_mid"))
                value = loc.Get(c.NameKey + "_mid");
            args.Add(new KeyValuePair<string, string>(a.Name, value));
        }
        return loc.Format(e.Key, args);
    }

    public static string Resolve(World w, string value)
    {
        if (value.Length < 2 || value[1] != ':') return value;
        string body = value.Substring(2);
        LocTable loc = w.Content.Loc;
        switch (value[0])
        {
            case 'c':
                return int.TryParse(body, NumberStyles.Integer, Inv, out int id) ? Name(w, id) : loc.Get("name.someone");
            case 'i':
                return loc.Get(w.Content.ItemById(body).NameKey);
            case 'k':
                return loc.Get(body);
            case 'n':
                return body;
            default:
                return value;
        }
    }

    public static string Name(World w, int id)
    {
        Character? c = w.Find(id);
        return w.Content.Loc.Get(c == null ? "name.someone" : c.NameKey);
    }

    public static string When(World w, long tick)
    {
        WorldDef def = w.Content.World;
        string time = Calendar.Hour(tick, def).ToString("00", Inv) + ":" + Calendar.Minute(tick, def).ToString("00", Inv);
        return w.Content.Loc.Format("time.when", new[]
        {
            new KeyValuePair<string, string>("day", (Calendar.Day(tick, def) + 1).ToString(Inv)),
            new KeyValuePair<string, string>("time", time),
        });
    }
}
