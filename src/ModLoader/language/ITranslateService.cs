namespace ModLoader.language;

public interface ITranslateService {
    public string format(string key, string module, params object[] args);
}