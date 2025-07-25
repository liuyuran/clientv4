namespace ModLoader.setting;

public struct SettingDefine {
    public string Key;
    public string Category;
    public object Config;
    public string Value;
    public string DefaultValue;
    public string Name;
    public string Description;
    public Action<string> OnChange;
    public int Order;
}