using System.Diagnostics.CodeAnalysis;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using game.scripts.server;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.start;

[SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
public partial class Menu : Control {
    private readonly ILogger _logger = LogManager.GetLogger<Menu>();
    [Export] private PackedScene _gameScene;
    private TextureRect _backgroundRect;
    private VBoxContainer _gameBtnContainer;
    private RichTextLabel _gameTitleLabel;
    private Panel _modalPanel;
    private const int SplitBetweenTitleAndButtons = 20;
    private double _backgroundWhRatio = -1;
    
    public override void _Ready() {
        _backgroundRect = this.FindNodeByName<TextureRect>("Background");
        _gameBtnContainer = this.FindNodeByName<VBoxContainer>("GameButtons");
        _gameTitleLabel = this.FindNodeByName<RichTextLabel>("GameTitle");
        _modalPanel = this.FindNodeByName<Panel>("PanelBox");
        CloseOtherPanel();
        InjectVersionInfo();
        GameNodeReference.CurrentScene = GetTree().CurrentScene;
        GameNodeReference.GamingScenePacked = _gameScene;
        GetTree().Root.SizeChanged += OnRootSizeChanged;
        CallDeferred(MethodName.OnRootSizeChanged);
        ResetManager.Reset();
    }

    public override void _ExitTree() {
        GetTree().Root.SizeChanged -= OnRootSizeChanged;
    }

    private void OnRootSizeChanged() {
        if (_backgroundWhRatio < 0) {
            // 初始时设置背景的宽高比
            _backgroundWhRatio = _backgroundRect.Texture?.GetSize().X / _backgroundRect.Texture?.GetSize().Y ?? 1;
        }
        // 设置背景的大小
        var rootSize = GetTree().Root.Size;
        // 优先覆盖整个窗口，并保持图片比例
        if (rootSize.X / rootSize.Y > _backgroundWhRatio) {
            // 窗口更宽
            _backgroundRect.CustomMinimumSize = new Vector2(rootSize.X, rootSize.X / _backgroundWhRatio);
        } else {
            // 窗口更高
            _backgroundRect.CustomMinimumSize = new Vector2(rootSize.Y * _backgroundWhRatio, rootSize.Y);
        }

        if (_backgroundRect.CustomMinimumSize < rootSize) {
            if (rootSize.X / rootSize.Y <= _backgroundWhRatio) {
                // 窗口更宽
                _backgroundRect.CustomMinimumSize = new Vector2(rootSize.X, rootSize.X / _backgroundWhRatio);
            } else {
                // 窗口更高
                _backgroundRect.CustomMinimumSize = new Vector2(rootSize.Y * _backgroundWhRatio, rootSize.Y);
            }
        }
        _backgroundRect.Size = _backgroundRect.CustomMinimumSize;
        _backgroundRect.Position = (GetTree().Root.Size - _backgroundRect.Size) / 2;
        _gameTitleLabel.Position = new Vector2(
            GetTree().Root.Size.X / 2 - _gameTitleLabel.Size.X / 2,
            GetTree().Root.Size.Y / 2 - (_gameTitleLabel.Size.Y + _gameBtnContainer.Size.Y + SplitBetweenTitleAndButtons) / 2
        );
        _gameBtnContainer.Position = new Vector2(
            GetTree().Root.Size.X / 2 - _gameBtnContainer.Size.X / 2, 
            _gameTitleLabel.Position.Y + _gameTitleLabel.Size.Y + SplitBetweenTitleAndButtons
        );
    }

    private bool tmp = false;

    public override void _Process(double delta) {
        /*OpenSingle();
        ResetManager.Reset();
        ArchiveManager.instance.Load(_selectedArchiveName);
        JumpToGameSceneAndStartLocalServer();*/
        // OpenSettings();
        if (tmp) return;
        OpenSettings();
        tmp = true;
    }

    private void CloseOtherPanel() {
        CloseSinglePlayMenu();
        CloseMultiPlayMenu();
        CloseSettingPanel();
        CloseModPanel();
        CloseAboutPanel();
        _modalPanel.Visible = false;
    }

    private void ExitGame() {
        GetTree().Quit();
    }
    
    private void OpenSingle() {
        CloseOtherPanel();
        _modalPanel.Visible = true;
        OpenSinglePlayMenu();
    }
    
    private void OpenMulti() {
        CloseOtherPanel();
        _modalPanel.Visible = true;
        OpenMultiPlayMenu();
    }
    
    private void OpenSettings() {
        CloseOtherPanel();
        _modalPanel.Visible = true;
        OpenSettingPanel();
    }
    
    private void OpenModSettings() {
        CloseOtherPanel();
        _modalPanel.Visible = true;
        OpenModPanel();
    }
    
    private void OpenAbout() {
        CloseOtherPanel();
        _modalPanel.Visible = true;
        OpenAboutPanel();
    }

    private void JumpToGameSceneAndStartLocalServer() {
        // 设置启动参数
        ServerStartupConfig.instance.isLocalServer = true;
        ServerStartupConfig.instance.serverIp = "";
        ServerStartupConfig.instance.serverPort = -1;
        // 加载游戏场景
        GetTree().ChangeSceneToPacked(GameNodeReference.GamingScenePacked);
    }
    
    private void InjectVersionInfo() {
        var versionLabel = this.FindNodeByName<RichTextLabel>("Version");
        versionLabel.Text = $"Version: {ProjectSettings.GetSetting("application/config/version")}";
    }
}