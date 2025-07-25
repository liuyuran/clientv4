using ModLoader.setting;

namespace ModLoader.handler;

public interface ISettingsManager {
    public void AddSetting(string module, SettingDefine setting);
}