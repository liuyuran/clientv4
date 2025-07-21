using System;
using game.scripts.server.ECSBridge;
using Godot;
using JetBrains.Annotations;

namespace game.unittest;

[UsedImplicitly]
public class ECSSystemBridgeUnitTest : IUnitTest {
    private ECSSystemBridge _bridgeNode;

    public void RunTest(Node node, Action<string> log) {
        _bridgeNode = new ECSSystemBridge();
        node.AddChild(_bridgeNode);
    }

    public void Cleanup(Node node, Action<string> log) {
        if (_bridgeNode != null) {
            _bridgeNode.QueueFree();
            _bridgeNode = null;
        }
    }
}