using ModLoader.language;

namespace ModLoader;

public class I18N {
    public static ITranslateService? service { get; set; }

    public static string Tr(string module, string key, params object[] args) {
        if (service == null) {
            throw new InvalidOperationException("I18N service is not initialized.");
        }
        return service.Format(key, module, args);
    } 
}