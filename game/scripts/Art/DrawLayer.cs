using System;
using Godot;

namespace Return.Game.Art;

/// <summary>
/// One drawing pass of the map (ground, world, overlay): a Node2D whose _Draw calls back into the
/// map view, so the orchestration stays in MapView and the pictures stay in the Art classes.
/// </summary>
public partial class DrawLayer : Node2D
{
    private Action<DrawLayer>? _painter;

    public static DrawLayer Make(string name, Action<DrawLayer> painter)
    {
        var layer = new DrawLayer { Name = name };
        layer._painter = painter;
        return layer;
    }

    public override void _Draw() => _painter?.Invoke(this);
}
