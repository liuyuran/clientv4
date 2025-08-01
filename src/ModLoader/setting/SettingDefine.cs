namespace ModLoader.setting;

public delegate string GetString();

public struct SettingDefine {
    public string Key;
    public GetString Category;
    public object Config;
    public string Value;
    public GetString DefaultValue;
    public GetString Name;
    public GetString Description;
    public Action<string> OnChange;
    public List<ExtraButton> ExtraButtons;
    public int Order;
}

public struct ExtraButton {
    public GetString Name;
    public Action OnClick;
}