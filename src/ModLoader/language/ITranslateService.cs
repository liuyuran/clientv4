namespace ModLoader.language;

public interface ITranslateService {
    public string Format(string key, string module, params object[] args);
}