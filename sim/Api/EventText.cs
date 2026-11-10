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
    /// Grammar stays in the loc files. When the player fills placeholder X and the table has
    /// "&lt;key&gt;_you_X", that second-person template is used ("You were hurt", "Mira tended your wounds").
    /// A character placeholder that does not open the sentence uses the name's "_mid" variant when the
    /// table has one, so the player reads "You asked…" but "…asked you".
    /// </summary>
    public static string Describe(World w, SimEvent e) => Describe(w, e.Key, e.Args);

    /// <summary>A line of a lost future (ADR-0030), frozen as a key and arguments, read in the world as it is now.</summary>
    public static string Describe(World w, LostLine line) => Describe(w, line.Key, line.Args);

    /// <summary>An event's text from its key and arguments (a live event's, or a frozen line's).</summary>
    public static string Describe(World w, string eventKey, IReadOnlyList<EventArg> eventArgs)
    {
        LocTable loc = w.Content.Loc;
        string key = SecondPersonKey(w, eventKey, eventArgs) ?? eventKey;
        foreach (EventArg a in eventArgs)
        {
            if (a.Name != "n") continue;
            if (a.Value == "n:1" && loc.Has(key + "_one")) key += "_one";
            else if (a.Value == "n:0" && loc.Has(key + "_zero")) key += "_zero";
            break;
        }
        string template = loc.Get(key);
        var args = new List<KeyValuePair<string, string>>(eventArgs.Count);
        bool single = false;
        foreach (EventArg a in eventArgs)
            if (a.Name == "n" && a.Value == "n:1") single = true;
        foreach (EventArg a in eventArgs)
        {
            string value = Resolve(w, a.Value);
            if (single && a.Value.StartsWith("i:", StringComparison.Ordinal) && w.Content.TryItem(a.Value.Substring(2)) is { } item && loc.Has(item.NameKey + "_one"))
                value = loc.Get(item.NameKey + "_one");
            if (a.Value.StartsWith("c:", StringComparison.Ordinal) && !template.StartsWith("{" + a.Name + "}", StringComparison.Ordinal)
                && int.TryParse(a.Value.AsSpan(2), NumberStyles.Integer, Inv, out int id) && w.Find(id) is { } c && loc.Has(c.NameKey + "_mid"))
                value = loc.Get(c.NameKey + "_mid");
            args.Add(new KeyValuePair<string, string>(a.Name, value));
        }
        return loc.Format(key, args);
    }

    private static string? SecondPersonKey(World w, string key, IReadOnlyList<EventArg> args)
    {
        if (w.PlayerId == 0) return null;
        string player = "c:" + w.PlayerId.ToString(Inv);
        foreach (EventArg a in args)
        {
            if (a.Value != player) continue;
            string variant = key + "_you_" + a.Name;
            if (w.Content.Loc.Has(variant)) return variant;
        }
        return null;
    }

    /// <summary>Like <see cref="Resolve"/>, but a character name takes its mid-sentence form ("you") when the table has one.</summary>
    public static string ResolveMid(World w, string value)
    {
        if (value.StartsWith("c:", StringComparison.Ordinal) && int.TryParse(value.AsSpan(2), NumberStyles.Integer, Inv, out int id)
            && w.Find(id) is { } c && w.Content.Loc.Has(c.NameKey + "_mid"))
            return w.Content.Loc.Get(c.NameKey + "_mid");
        return Resolve(w, value);
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
            case 'e':
                return Quote(w, body) ?? value;
            default:
                return value;
        }
    }

    /// <summary>
    /// An <c>e:&lt;future&gt;:&lt;line&gt;</c> argument (ADR-0030): one line quoting another, here a memory quoting the
    /// lost future it came from, without composing English. Null if the run memory holds no such line.
    /// </summary>
    private static string? Quote(World w, string body)
    {
        int colon = body.IndexOf(':');
        if (colon <= 0 || !int.TryParse(body.AsSpan(0, colon), NumberStyles.Integer, Inv, out int future)
            || !int.TryParse(body.AsSpan(colon + 1), NumberStyles.Integer, Inv, out int line)) return null;
        List<LostFuture> futures = w.Fate.LostFutures;
        if (future < 0 || future >= futures.Count || line < 0 || line >= futures[future].Lines.Count) return null;
        LostLine quoted = futures[future].Lines[line];
        // A memory of a memory (a return to before an earlier one, where the remembered grudge was the wrong) quotes what
        // was first remembered, not the remembering. A line quotes only futures lost before its own, so this ends.
        if (quoted.Key.StartsWith("event.fate.remembered", StringComparison.Ordinal))
            foreach (EventArg a in quoted.Args)
                if (a.Value.StartsWith("e:", StringComparison.Ordinal) && Quote(w, a.Value.Substring(2)) is { } first) return first;
        return Describe(w, quoted);
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
