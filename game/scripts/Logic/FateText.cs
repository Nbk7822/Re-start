using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Systems;

namespace Return.Game.Logic;

/// <summary>
/// The view's words for who you are and Return by Death (ADR-0030): the opening card, your inspector lines, the cards
/// of a return and of the end of your lifetime, the Lost futures tab and the Orders row of your gift. Built from the
/// text-state API and loc keys only; Godot-free so the tests compile it.
/// </summary>
public static class FateText
{
    /// <summary>A line of the Lost futures tab and how it is drawn.</summary>
    public enum LineKind
    {
        Heading,
        Line,
        Growth,
        Death,
        Note,
    }

    public readonly record struct LostLine(string Text, LineKind Kind);

    /// <summary>The opening card: you died on Earth; how you woke here, as your people; that something came with you.</summary>
    public static IReadOnlyList<string> Opening(World w)
    {
        var lines = new List<string> { Ui.T("ui.fate.died_on_earth") };
        RebirthPeopleDef? people = w.Content.Rebirth.People(w.Fate.PeopleId);
        if (people != null) lines.Add(Ui.T(people.OpeningKey));
        if (w.Fate.GiftId.Length > 0) lines.Add(Ui.T("ui.fate.gift_unfound"));
        return lines;
    }

    /// <summary>Your inspector's lines above the usual ones: people, birth and age; your clock; your gift; lives lost; what you remember.</summary>
    public sealed record PlayerLines(string Subtitle, string Life, string Gift, string GiftTooltip, string LivesLost, IReadOnlyList<string> Remembers);

    public static PlayerLines Player(World w)
    {
        PlayerState p = StateApi.Player(w);
        string gift = "", tooltip = "";
        if (p.Gift is { } g)
        {
            gift = g.Awake ? Ui.F("life.gift_line", ("gift", g.Name), ("stage", g.StageText)) : Ui.T("life.gift_unknown");
            tooltip = g.Awake ? g.Summary + "\n" + g.Grows + "\n" + g.Seen : "";
        }
        var remembers = new List<string>();
        foreach (MemoryState m in p.Memories) remembers.Add(m.Text);
        return new PlayerLines(p.AgeLine, p.LifeLine, gift, tooltip, Ui.N(p.LivesLost), remembers);
    }

    /// <summary>The first card of a return: how you died, read in the world gone back to (the line is frozen in the lost future).</summary>
    public static string DeathCard(World w, int future)
    {
        List<LostFuture> futures = w.Fate.LostFutures;
        if (future < 0 || future >= futures.Count || futures[future].DeathLine < 0) return "";
        LostFuture f = futures[future];
        return EventText.Describe(w, f.Lines[f.DeathLine]);
    }

    /// <summary>The last card of a return: when you came back to, and that you remember everything.</summary>
    public static IReadOnlyList<string> CameBack(World w) => new[]
    {
        Ui.F("ui.fate.came_back", ("when", EventText.When(w, w.Tick))),
        Ui.T("ui.fate.remember"),
    };

    /// <summary>The card when your lifetime ends: how, and at what age; there is no return from this.</summary>
    public static string EndCard(World w) => StateApi.Player(w).EndText;

    /// <summary>The Lost futures tab: each timeline you lost, newest first, headed by its span and your death, then its lines.</summary>
    public static IReadOnlyList<LostLine> LostFutures(World w)
    {
        var lines = new List<LostLine>();
        foreach (LostFutureState f in StateApi.LostFutures(w))
        {
            if (lines.Count > 0) lines.Add(new LostLine("", LineKind.Note));
            lines.Add(new LostLine(Ui.F("ui.chronicle.lost_heading", ("from", f.From), ("to", f.To)), LineKind.Heading));
            if (f.Death.Length > 0) lines.Add(new LostLine(f.Death, LineKind.Death));
            string key = f.Stories == 1 ? "ui.chronicle.lost_stories_one" : f.Stories == 0 ? "ui.chronicle.lost_stories_zero" : "ui.chronicle.lost_stories";
            lines.Add(new LostLine(Ui.F(key, ("n", Ui.N(f.Stories))), LineKind.Note));
            foreach (LostLineState line in f.Lines)
            {
                if (line.Death) continue; // it heads the future
                string text = line.Growth ? Ui.F("ui.chronicle.lost_growth", ("text", line.Text)) : line.Text;
                lines.Add(new LostLine(Ui.F("ui.chronicle.entry", ("when", line.When), ("text", text)), line.Growth ? LineKind.Growth : LineKind.Line));
            }
        }
        if (lines.Count == 0) lines.Add(new LostLine(Ui.T("ui.chronicle.lost_empty"), LineKind.Note));
        return lines;
    }

    /// <summary>Your gift's standing order, if you hold a gift with one and have found it: its id, title, and each choice with its words and hint.</summary>
    public sealed record GiftOrder(string OrderId, string Title, IReadOnlyList<(string Choice, string Text, string Hint)> Choices, string Current);

    public static GiftOrder? Order(World w, OrderDrafts drafts)
    {
        if (!w.Fate.GiftAwakened || Gifts.Held(w)?.Order is not { } order) return null;
        var choices = new List<(string, string, string)>();
        foreach (string c in order.Choices) choices.Add((c, Ui.T(order.ChoiceKey(c)), Ui.T(order.HintKey(c))));
        string current = drafts.StandingOrder(order.Id) ?? PendingOrder(w, order.Id) ?? Gifts.Order(w, order);
        return new GiftOrder(order.Id, Ui.T(order.TitleKey), choices, current);
    }

    private static string? PendingOrder(World w, string orderId)
    {
        string? choice = null;
        foreach (Command c in w.PendingCommands)
            if (c is SetStandingOrder s && s.OrderId == orderId) choice = s.ChoiceId;
        return choice;
    }
}
