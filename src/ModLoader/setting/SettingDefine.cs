namespace ModLoader.setting;

public delegate string GetString();

public delegate string? SettingCallback();

public struct SettingDefine {
    public required string Key;
    public required GetString Category;
    public required object Config;
    public required string Value;
    public required GetString DefaultValue;
    public required GetString Name;
    public required GetString Description;
    public required Action<string> OnChange;
    public required List<ExtraButton> ExtraButtons;
    public required int Order;
}

public struct ExtraButton {
    public required GetString Name;
    public required SettingCallback OnClick;
}