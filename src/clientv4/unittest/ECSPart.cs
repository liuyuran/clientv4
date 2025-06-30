using game.scripts.server.ECSBridge;

namespace game.unittest;

public partial class ShownUnitTest {
    private ECSSystemBridge _bridgeNode;
    
    private void RunTestForECSBridge() {
        _bridgeNode = new ECSSystemBridge();
        _boxGame.AddChild(_bridgeNode);
        AppendLog("Running ECS Bridge Test...");
    }
    
    private void CleanupECSBridgeTest() {
        if (_bridgeNode != null) {
            _bridgeNode.QueueFree();
            _bridgeNode = null;
            AppendLog("ECS Bridge Test Cleaned Up.");
        } else {
            AppendLog("No ECS Bridge Test to clean up.");
        }
    }
}