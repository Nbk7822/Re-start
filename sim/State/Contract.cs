namespace Return.Sim;

public enum ContractStatus
{
    Open,
    Kept,
    Broken,
    Void,
}

/// <summary>
/// A bargain, first-class like a debt (ADR-0022): the bound party (a djinn) serves at a kind of work,
/// and the camp pays a price each dawn until the term ends. The patron is whoever struck it for the
/// camp. Paid to the end, it is kept; a dawn with nothing to pay breaks it. If either party leaves
/// or dies it lapses without a word (Void).
/// </summary>
public sealed class Contract
{
    public int Id { get; set; }
    public int BoundId { get; set; }
    public int PatronId { get; set; }
    public string ServiceWork { get; set; } = "";
    public string PriceItem { get; set; } = "";
    public int PriceAmount { get; set; }
    public long FormedTick { get; set; }
    public long NextDueTick { get; set; }
    public long EndsTick { get; set; }
    public long CauseEvent { get; set; }
    public long FormedEvent { get; set; }
    public long ClosedEvent { get; set; }
    public ContractStatus Status { get; set; }
}
