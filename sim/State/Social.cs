namespace Return.Sim;

/// <summary>Directed opinion of one character about another, −100..100.</summary>
public sealed class Opinion
{
    public int TargetId { get; set; }
    public int Value { get; set; }
}

/// <summary>A first-class grudge: who holds it, against whom, why, and how strongly.</summary>
public sealed class Grudge
{
    public int Id { get; set; }
    public int HolderId { get; set; }
    public int TargetId { get; set; }
    public int Intensity { get; set; }
    public long CauseEvent { get; set; }
    public long FormedTick { get; set; }
    public bool Active { get; set; } = true;
}

public enum DebtKind
{
    Care,
    Blood,
    Meal,
    Rescue,
    PromiseMeal,
    PromiseRest,
}

public enum DebtStatus
{
    Open,
    Repaid,
    Broken,
}

/// <summary>
/// A first-class debt. Promises are debts with a due tick (0 = no due date): the debtor owes the
/// creditor something by then, and breaking it forms a grudge.
/// </summary>
public sealed class Debt
{
    public int Id { get; set; }
    public int DebtorId { get; set; }
    public int CreditorId { get; set; }
    public DebtKind Kind { get; set; }
    public int Weight { get; set; }
    public long CauseEvent { get; set; }
    public long FormedTick { get; set; }
    public long DueTick { get; set; }
    public DebtStatus Status { get; set; }

    public bool IsPromise => Kind is DebtKind.PromiseMeal or DebtKind.PromiseRest;
}

/// <summary>A remembered moment. Mood and opinion effects come from the thought's definition.</summary>
public sealed class Thought
{
    public string DefId { get; set; } = "";
    public int AboutId { get; set; }
    public long ExpiresTick { get; set; }
    public long CauseEvent { get; set; }
}
