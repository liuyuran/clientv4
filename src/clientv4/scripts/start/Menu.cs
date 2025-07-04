using game.scripts.server;
using Godot;

namespace game.scripts.start;

public partial class Menu : Control {
    [Export] private PackedScene _gameScene;
    
    public override void _Ready() {
        CloseOtherPanel();
    }

    public override void _Process(double delta) {
        JumpToGameSceneAndStartLocalServer();
    }
    
    private void CloseOtherPanel() {
        CloseModPanel();
        CloseAboutPanel();
    }

    private void ExitGame() {
        GetTree().Quit();
    }
    
    private void OpenSingle() {
        CloseOtherPanel();
        OpenSinglePlayMenu();
    }
    
    private void OpenMulti() {
        CloseOtherPanel();
        OpenMultiPlayMenu();
    }
    
    private void OpenSettings() {
        CloseOtherPanel();
        OpenSettingPanel();
    }
    
    private void OpenModSettings() {
        CloseOtherPanel();
        OpenModPanel();
    }
    
    private void OpenAbout() {
        CloseOtherPanel();
        OpenAboutPanel();
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