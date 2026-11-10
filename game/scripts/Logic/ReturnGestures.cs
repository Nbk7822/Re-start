namespace Return.Game.Logic;

/// <summary>
/// What a return does to the player's hands (ADR-0030; the merge review), Godot-free so the tests compile it. A gesture in
/// flight when you die was begun in the world that is gone: it ends without being released, the tool staying in hand, and
/// the drafts made in that world are dropped. SimHost releases nothing while the return plays and drops the drafts again at
/// the fold, so nothing chosen in the lost world reaches the world gone back to when time moves.
/// </summary>
public static class ReturnGestures
{
    /// <summary>A return began: the marking tool's and the Build tool's gestures end unreleased, and the drafts go.</summary>
    public static void Begin(OrderDrafts drafts, ZoneTool zones, BuildTool build)
    {
        zones.CancelDrag();
        build.CancelDrag();
        drafts.Clear();
    }

    /// <summary>The fold: anything drafted while the return played was chosen in the lost world, and goes too.</summary>
    public static void Fold(OrderDrafts drafts) => drafts.Clear();
}
