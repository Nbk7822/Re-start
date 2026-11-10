namespace Return.Sim.Systems.Culture;

/// <summary>
/// Culture and faith tuning, as named constants in the ADR-0013 style until the M0 values move into a
/// tuning block in world.json. Values from the culture-and-faith design, section 6.5 (ADR-0023).
/// </summary>
public static class CultureTuning
{
    /// <summary>Faith customs apply to followers at or above this devotion.</summary>
    public const int FaithCustomDevotion = 30;
    public const int LapsedBelow = 30;
    public const int DevoutFrom = 70;

    /// <summary>
    /// Chat warmth between two people of the same culture (G1), across cultures while either is new
    /// (G3), and between festival-goers (S1). A chat moves opinion by 1 × warmth / 100 in integers, so
    /// the design's +25 for kin changed no opinion at any chemistry; +50 lifts kin who grate a little
    /// (chemistry −1) to 1 and kin who click (+1) to 2 (lead's call, ADR-0023).
    /// </summary>
    public const int KinWarmth = 50;
    public const int StrangerWarmth = -25;
    public const int FestivalWarmth = 50;

    /// <summary>Someone with no culture at all (the otherworlder) is this many times likelier to breach a greeting (G4).</summary>
    public const int OutsiderBreachMultiplier = 3;

    /// <summary>A refusal on a custom's account is not personal: the refused take it at this share of a plain refusal.</summary>
    public const int CustomRefusalOpinionPct = 40;

    /// <summary>Oath weight (O1) is 100% plus this much per point of the creditor's Oathbound/Free axis.</summary>
    public const int OathWeightPerAxisPct = 25;
    /// <summary>Word of a broken promise spreads among a culture whose own Oathbound/Free axis is at least this (O2).</summary>
    public const int WordSpreadsFromAxis = 1;
    public const int HearsayGrudge = 15;

    public const int LeaderLastGrudge = 25;

    /// <summary>How long a deed is remembered for a sign to be read against, and how long a reader waits before reading another sign.</summary>
    public const int DeedMemoryHours = 48;
    public const int OmenCooldownHours = 120;
    public const int OmenOddsDivisor = 2000;
    public const int OmenOddsMaxPermille = 600;
    /// <summary>Omen odds scale by 100% plus this much per point of the reader's Arcane/Mundane axis.</summary>
    public const int ArcanePerAxisPct = 25;
    public const int OmenGrudge = 20;

    public const int SacrilegeGrudge = 40;

    /// <summary>Festival-goers sit one to a tile within this many tiles of the hearth (its neighbours first), so a festival is a ring, not a pile.</summary>
    public const int FireRings = 2;

    /// <summary>How much a deed weighs when a sign is read against it, in percent.</summary>
    public static int DeedWeightPct(DeedKind kind) => kind switch
    {
        DeedKind.DebtRepaid => 80,
        DeedKind.MealShared => 60,
        DeedKind.GuestWronged => 150,
        DeedKind.BloodGiven => 40,
        DeedKind.BloodRefused => 60,
        DeedKind.TreeFelled => 30,
        DeedKind.SacredHarmed => 200,
        _ => 100,
    };

    /// <summary>What a culture's stance toward a people does: chat warmth, insult odds (‰) and blood consent (A1).</summary>
    public static (int Warmth, int Insult, int Consent) Stance(string? stance) => stance switch
    {
        "revere" => (50, -20, 30),
        "wary" => (-25, 20, -10),
        "fear" => (-50, 40, -30),
        "scorn" => (-25, 60, -20),
        _ => (0, 0, 0),
    };
}
