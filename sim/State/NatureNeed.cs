namespace Return.Sim;

/// <summary>
/// A need that comes from a person's nature, beyond food, rest and company (ADR-0022): a dryad's
/// forest, a djinn's bargain, a fae's courtesy, an undead's purpose. Value is 0..1000 (1000 = fully
/// met), like the other needs. The definition (content/needs.json) sets the thresholds, thoughts,
/// events and harm; Stage and LastEvent work like HungerStage and LastFoodEvent.
/// </summary>
public sealed class NatureNeed
{
    public string Id { get; set; } = "";
    public int Value { get; set; }

    /// <summary>0 = met, 1 = low, 2 = critical.</summary>
    public int Stage { get; set; }

    /// <summary>The most recent event that explains this need's state, cited by the next threshold event; 0 = none.</summary>
    public long LastEvent { get; set; }

    /// <summary>When the need last became critical (for departures after so many hours).</summary>
    public long CriticalSince { get; set; }

    /// <summary>What the need is bound to, when it is bound to a thing: the tree a dryad is bound to (a Truth).</summary>
    public int BondId { get; set; }

    /// <summary>The work the need is bound to: the work an undead came back to do, the service a djinn offers (Truths).</summary>
    public string BondWork { get; set; } = "";

    /// <summary>The item the need asks for: the price a djinn asks for its service (a Truth).</summary>
    public string BondItem { get; set; } = "";

    /// <summary>When this need last drove a once-per-crisis act (a djinn's trouble); 0 = not this crisis.</summary>
    public long ActedTick { get; set; }
}
