namespace Return.Sim.Content;

/// <summary>
/// The content ids (and incident params) the simulation's code names directly. ContentDb checks them
/// at load, so renaming or dropping one fails there, naming the file and id, instead of hours into a
/// soak. A test keeps this list in step with the code.
/// </summary>
public static class RequiredContent
{
    public static readonly IReadOnlyList<string> Items = new[] { "berries", "timber" };

    /// <summary>Night thieves are human.</summary>
    public static readonly IReadOnlyList<string> Species = new[] { "human" };

    public static readonly IReadOnlyList<string> Work = new[]
    {
        "care", "eat", "forage", "gather_timber", "give_blood", "haul", "keep_watch", "repair_roof", "socialize",
    };

    public static readonly IReadOnlyList<string> Thoughts = new[]
    {
        "ate", "burned_by_sun", "cared_for", "chatted", "deep_talk", "exhausted", "friend_died", "gave_blood", "hungry",
        "insulted", "joined_camp", "lonely", "meal_stolen", "promise_broken", "promise_kept", "received_blood",
        "refused_someone", "rescued", "robbed", "roof_sound", "saw_death", "slept_in_shelter", "slept_outside", "soaked",
        "starving", "thief_driven_off", "thirsting", "was_refused", "windfall", "wounded",
        // The four peoples (ADR-0022).
        "at_purpose", "uneasy_near_dead", "among_trees", "tree_felled", "own_tree_felled",
    };

    /// <summary>Every incident M0 has behaviour for, with the params that behaviour reads.</summary>
    public static readonly IReadOnlyList<(string Id, string[] Params)> Incidents = new (string, string[])[]
    {
        ("wanderer_arrives", Array.Empty<string>()),
        ("rain", new[] { "startPermillePerHour", "minHours", "maxHours", "soakRestPerHour" }),
        ("food_spoils", new[] { "dailyPermille", "minBerries", "lossMin", "lossMax" }),
        ("work_accident", new[] { "damageMin", "damageMax" }),
        ("night_thief", new[] { "nightPermillePerHour", "earliestDay", "health", "stealMax", "fleeHealthBelow" }),
        ("windfall", new[] { "dailyPermille", "amount" }),
    };

    /// <summary>Every need from a species' nature that the simulation has behaviour for, with the params that behaviour reads (ADR-0022).</summary>
    public static readonly IReadOnlyList<(string Id, string[] Params)> Needs = new (string, string[])[]
    {
        ("purpose", new[] { "restorePerHour", "boundWorkBias" }),
        ("forest", new[]
        {
            "treeRange", "restorePerTree", "ownTreeWeight", "maxRestorePerHour", "tendMaxTicks", "bondSkipNearest", "bondCandidates",
            "respectOpinion", "woundMin", "woundMax", "ownTreeLoss", "grudgeIntensity", "griefGrudgeIntensity",
        }),
    };
}
