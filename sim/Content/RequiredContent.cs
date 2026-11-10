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
        "bound_by_bargain", "bargain_refused", "bargain_honoured", "bargain_betrayed", "fire_loosed",
        "word_wound", "words_kept", "kept_my_word", "spoke_truth", "told_hard_truth", "heard_hard_truth",
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

    /// <summary>The culture and faith thoughts (ADR-0023), checked like <see cref="Thoughts"/>.</summary>
    public static readonly IReadOnlyList<string> CultureThoughts = new[]
    {
        "debt_honoured", "faux_pas", "festival", "festival_lean", "grudge_set_down", "guest_bread_received", "guest_right_broken",
        "heard_oath_broken", "kin_greeting", "leader_ate_first", "leader_ate_last", "omen_anger", "omen_blessing", "sacred_tree_felled",
    };

    /// <summary>
    /// Every kind of custom the code has behaviour for, with the params that behaviour reads
    /// (customs.json) and the event moments every custom of that kind needs text for
    /// (event.culture.&lt;id&gt;.&lt;moment&gt;). Moments that depend on params come from <see cref="CustomMoments"/>.
    /// </summary>
    public static readonly IReadOnlyList<(string Kind, string[] Params, string[] Moments)> CustomKinds = new (string, string[], string[])[]
    {
        ("greeting", new[] { "breachPermille" }, new[] { "breach" }),
        ("blood_stance", new[] { "consentBias", "offers" }, Array.Empty<string>()),
        ("guest_right", new[] { "offerPermille", "wrongedGrudgePct" }, new[] { "shared", "foraged", "wronged" }),
        ("leader_last", Array.Empty<string>(), new[] { "offended", "honoured" }),
        ("debt_of_honour", new[] { "minDebtAgeHours" }, new[] { "repaid" }),
        ("festival", new[] { "season", "hours", "feastPerHead", "forgive" }, new[] { "begins", "forgave" }),
        ("sacred_tree", new[] { "nearestTrees" }, new[] { "desecrated" }),
    };

    /// <summary>The deeds gods care about, as domains.json names them (the snake_case of each <c>DeedKind</c>).</summary>
    public static readonly IReadOnlyList<string> Deeds = new[]
    {
        "oath_kept", "oath_broken", "debt_repaid", "meal_taken", "meal_shared", "guest_honoured", "guest_wronged",
        "blood_given", "blood_refused", "tree_felled", "sacred_harmed",
    };

    /// <summary>The incidents (and a member's death) a god may be believed to speak through.</summary>
    public static readonly IReadOnlyList<string> Signs = new[] { "rain", "windfall", "food_spoils", "work_accident", "death" };

    /// <summary>A custom's event moments: its kind's, plus "refused" or "offered" for a blood stance that refuses or offers, and "begins_lean" for a festival with a feast.</summary>
    public static IReadOnlyList<string> CustomMoments(CustomDef c)
    {
        var moments = new List<string>();
        foreach ((string kind, string[] _, string[] ms) in CustomKinds)
            if (kind == c.Kind) moments.AddRange(ms);
        if (c.Kind == "blood_stance")
        {
            if (c.Params.TryGetValue("consentBias", out int bias) && bias < 0) moments.Add("refused");
            if (c.Params.TryGetValue("offers", out int offers) && offers == 1) moments.Add("offered");
        }
        if (c.Kind == "festival" && c.Params.TryGetValue("feastPerHead", out int feast) && feast > 0) moments.Add("begins_lean");
        return moments;
    }

    /// <summary>The zone kinds WorldGen lays out in every camp (ADR-0028).</summary>
    public static readonly IReadOnlyList<string> Zones = new[] { "stockpile", "meeting_place" };

    /// <summary>The thoughts zones and the shared compliance rule give (ADR-0028), checked like <see cref="Thoughts"/>.</summary>
    public static readonly IReadOnlyList<string> ZoneThoughts = new[] { "saw_rule_broken", "sat_apart" };

    /// <summary>Every line zones step 3 writes (ADR-0028: the dark and the home area), checked at load like <see cref="ScheduleText"/>.</summary>
    public static readonly IReadOnlyList<string> ZoneText = new[]
    {
        "event.incident.work_accident_dark", "event.incident.work_accident_dark_you_a",
        "event.incident.work_accident_tired_dark", "event.incident.work_accident_tired_dark_you_a",
    };

    /// <summary>Every zone behaviour the simulation implements, with the layer it belongs on (ADR-0028).</summary>
    public static readonly IReadOnlyList<(string Behaviour, string Layer)> ZoneBehaviours = new (string, string)[]
    {
        ("stores", "use"),
        ("gather", "use"),
    };

    /// <summary>The kinds of day the code falls back to: diurnal for a species that tires, sleepless for one that does not (ADR-0029).</summary>
    public static readonly IReadOnlyList<string> Rhythms = new[] { "diurnal", "sleepless" };

    /// <summary>The thoughts the Schedule gives (ADR-0029), checked like <see cref="Thoughts"/>.</summary>
    public static readonly IReadOnlyList<string> ScheduleThoughts = new[]
    {
        "overworked", "no_time_to_myself", "evening_by_the_fire", "woken", "watch_refused", "covered_for", "watch_taken_for_me",
    };

    /// <summary>
    /// Every line the Schedule writes, and the words it shows, checked at load (ADR-0029). The refusals and
    /// declines go their own way for each of Compliance's reasons, so each reason has its own line.
    /// </summary>
    public static readonly IReadOnlyList<string> ScheduleText = new[]
    {
        "block.sleep", "block.work", "block.free", "block.sleep_hint", "block.work_hint", "block.free_hint",
        "time.hour", "job.wake_for_watch", "debt.watch",
        "event.world.camp_day", "event.world.watch_hours", "event.world.watch_hours_zero",
        "event.world.person_day", "event.world.person_day_you_a", "event.world.person_day_reset", "event.world.person_day_reset_you_a",
        "event.social.declined_day_own_way", "event.social.declined_day_dislike", "event.social.declined_day_mood", "event.social.declined_day_ways",
        "event.social.kept_old_hours_own_way", "event.social.kept_old_hours_dislike", "event.social.kept_old_hours_mood", "event.social.kept_old_hours_ways",
        "event.work.took_watch", "event.work.went_to_wake", "event.work.covered_watch", "event.work.stayed_on_watch",
        "event.work.left_watch_unrelieved", "event.work.left_watch_hungry", "event.work.watch_unkept", "event.work.worked_late",
        "event.social.watch_refused_hurt", "event.social.watch_refused_tired", "event.social.watch_refused_hungry", "event.social.watch_refused_grudge",
        "event.social.watch_refused_own_way", "event.social.watch_refused_dislike", "event.social.watch_refused_mood", "event.social.watch_refused_ways",
        "event.social.owes_watch", "event.social.owes_watch_sworn", "event.social.blood_refused_woken",
        "event.needs.fell_asleep_on_watch", "event.needs.worn_out", "event.incident.work_accident_tired", "event.incident.stolen_unwatched",
        "place.watch_post_kept", "place.watch_post_empty",
        "lived.slept", "lived.worked", "lived.watched", "lived.free", "lived.needs",
        "schedule.range", "schedule.own.sleep", "schedule.own.work", "schedule.own.free", "schedule.own.and", "schedule.own.sep",
        "schedule.note.camp", "schedule.note.own", "schedule.note.never_sleeps", "schedule.note.sun", "schedule.note.declined", "schedule.latest",
        "schedule.hour.planned", "schedule.hour.lived", "schedule.hour.lived_differs", "schedule.hour.sun",
        "schedule.watch.kept", "schedule.watch.none", "schedule.watch.now", "schedule.watch.nobody_now", "schedule.watch.last_night",
        "schedule.watch.span", "schedule.watch.span_nobody", "schedule.watch.span_sep",
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
        ("contract", new[]
        {
            "boundRestorePerHour", "priceAmount", "reserveDays", "serviceBias", "offerCooldownHours", "minTermDays", "betrayalLoss",
            "grudgeIntensity", "refusedGrudgeIntensity", "troubleTimber", "fireGrudgeIntensity",
        }),
        ("courtesy", new[]
        {
            "brokenWordLoss", "brokenWordHurt", "keptWordGain", "chatGain", "deepTalkGain", "slightLoss", "truthEasesGrudge",
            "witnessGrudgeIntensity", "grudgeIntensity",
        }),
    };
}
