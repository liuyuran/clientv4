using game.scripts.server;
using Godot;

namespace game.scripts.start;

public partial class Menu : Node3D {
    [Export] private PackedScene _gameScene;
    
    public override void _Ready() {
        //
    }

    public override void _Process(double delta) {
        //
    }
    
    private void CloseOtherPanel() {
        CloseModPanel();
        CloseAboutPanel();
    }

    private void ExitGame() {
        GetTree().Quit();
    }
    
    private void OpenSettings() {
        CloseOtherPanel();
        //
    }
    
    private void OpenModSettings() {
        CloseOtherPanel();
        //
    }
    
    private void OpenAbout() {
        CloseOtherPanel();
        //
    }

    private void JumpToGameSceneAndStartLocalServer() {
        // 设置启动参数
        ServerStartupConfig.instance.isLocalServer = true;
        ServerStartupConfig.instance.serverIp = "";
        ServerStartupConfig.instance.serverPort = -1;
        // 加载游戏场景
        GetTree().ChangeSceneToPacked(_gameScene);
    }
}