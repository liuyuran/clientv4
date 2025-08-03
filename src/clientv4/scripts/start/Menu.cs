using System.Diagnostics.CodeAnalysis;
using game.scripts.config;
using game.scripts.manager;
using game.scripts.manager.archive;
using game.scripts.manager.mod;
using game.scripts.manager.reset;
using game.scripts.manager.settings;
using game.scripts.server;
using game.scripts.utils;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.logger;

namespace game.scripts.start;

[SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
public partial class Menu : Control {
    private readonly ILogger _logger = LogManager.GetLogger<Menu>();
    private PackedScene _gameScene;
    private TextureRect _backgroundRect;
    private VBoxContainer _gameBtnContainer;
    private RichTextLabel _gameTitleLabel;
    private Panel _modalPanel;
    private const int SplitBetweenTitleAndButtons = 20;
    private double _backgroundWhRatio = -1;
    private ulong _lastBackActive;
    private const ulong MinimumBackActiveTime = 300;
    
    public override void _Ready() {
        _gameScene = ResourceLoader.Load<PackedScene>("res://scenes/game.tscn");
        PlatformUtil.goDotMode = true;
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
        ResourcePackManager.instance.ScanResourcePacks();
        LanguageManager.instance.ReloadLanguageFiles();
        // reset to load language setting and mod settings correctly
        SettingsManager.instance.Reset();
        ModManager.instance.Reset();
        LanguageManager.LanguageChanged += UpdateUITranslate;
        UpdateUITranslate();
    }

    public override void _ExitTree() {
        GetTree().Root.SizeChanged -= OnRootSizeChanged;
        LanguageManager.LanguageChanged -= UpdateUITranslate;
    }
    
    private void UpdateUITranslate() {
        UpdateMainUITranslate();
        UpdateAboutUITranslate();
        UpdateSettingsUITranslate();
        UpdateSinglePlayUITranslate();
        UpdateMultiPlayUITranslate();
    }

    private void UpdateMainUITranslate() {
        var singlePlayerButton = this.FindNodeByName<Button>("SinglePlayer");
        var multiPlayerButton = this.FindNodeByName<Button>("MultiPlayer");
        var settingsButton = this.FindNodeByName<Button>("Settings");
        var modSettingsButton = this.FindNodeByName<Button>("ModSettings");
        var aboutButton = this.FindNodeByName<Button>("About");
        var exitButton = this.FindNodeByName<Button>("Exit");
        var gameTitle = this.FindNodeByName<RichTextLabel>("GameTitle");
        var versionLabel = this.FindNodeByName<RichTextLabel>("Version");
        singlePlayerButton.Text = I18N.Tr("core.gui", "main-menu.single-player");
        multiPlayerButton.Text = I18N.Tr("core.gui", "main-menu.multi-player");
        settingsButton.Text = I18N.Tr("core.gui", "main-menu.settings");
        modSettingsButton.Text = I18N.Tr("core.gui", "main-menu.mod-settings");
        aboutButton.Text = I18N.Tr("core.gui", "main-menu.about");
        exitButton.Text = I18N.Tr("core.gui", "main-menu.exit");
        gameTitle.Text = I18N.Tr("core.gui", "main-menu.game-title");
        versionLabel.Text = I18N.Tr("core.gui", "main-menu.version", GetVersion());
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

    public override void _Process(double delta) {
        if (!InputManager.instance.IsKeyPressed(InputKey.UICancel)) return;
        if (PlatformUtil.GetTimestamp() - _lastBackActive < MinimumBackActiveTime) return;
        _lastBackActive = PlatformUtil.GetTimestamp();
        if (_singlePlayMenu != null && _createPanel is { Visible: true }) {
            CloseSingleCreateWorld();
            return;
        }
        if (_singlePlayMenu != null) {
            CloseSinglePlayMenu();
            return;
        }
        if (_multiPlayMenu != null) {
            CloseMultiPlayMenu();
            return;
        }
        if (_settingPanel != null) {
            CloseSettingPanel();
            return;
        }
        if (_modPanel != null) {
            CloseModPanel();
            return;
        }

        if (_aboutPanel == null) return;
        CloseAboutPanel();
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