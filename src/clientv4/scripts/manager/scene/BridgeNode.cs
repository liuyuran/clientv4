using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using ModLoader.scene;

namespace game.scripts.manager.scene;

[SuppressMessage("ReSharper", "Godot.MissingParameterlessConstructor")]
public partial class BridgeNode(NodeController controller) : Control {
    public override void _Ready()
    {
        base._Ready();
        var queue = new Queue<Node>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            if (node is Button button)
            {
                button.Pressed += () => controller.ClickEvent?.Invoke(button.Name);
            }

            foreach (var child in node.GetChildren())
            {
                queue.Enqueue(child);
            }
        }
    }
}