using System;
using System.Linq;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.language;
using ModLoader.logger;
using SecondLanguage;
using Translation = Godot.Translation;

namespace game.scripts.manager;

public class LanguageManager: ITranslateService {
    private readonly ILogger _logger = LogManager.GetLogger<LanguageManager>();
    public static LanguageManager instance { get; private set; } = new();

    private LanguageManager() {
        TranslationServer.SetLocale("zh_CN");
        I18N.service = this;
    }

    public void ReloadLanguageFiles() {
        TranslationServer.Clear();
        var allResourcePacks = ResourcePackManager.instance.GetAllResourcePacks();
        foreach (var pack in allResourcePacks) {
            var languagePath = System.IO.Path.Combine(pack.path, "language");
            if (!DirAccess.DirExistsAbsolute(languagePath)) continue;
            // 如果存在相同文件名的两个翻译文件，优先使用mo文件
            var moFiles = DirAccess.GetFilesAt(languagePath)
                .Where(file => file.EndsWith(".mo"))
                .ToList();
            var moKeys = moFiles
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToHashSet();
            var poFiles = DirAccess.GetFilesAt(languagePath)
                .Where(file => {
                    if (!file.EndsWith(".po")) return false;
                    if (moKeys.Contains(System.IO.Path.GetFileNameWithoutExtension(file))) {
                        return false;
                    }

                    return true;
                })
                .ToList();
            // 先加载mo文件
            foreach (var file in moFiles) {
                var locale = System.IO.Path.GetFileNameWithoutExtension(file);
                AddTranslation(locale, System.IO.Path.Combine(languagePath, file));
            }

            // 再加载po文件
            foreach (var file in poFiles) {
                var locale = System.IO.Path.GetFileNameWithoutExtension(file);
                AddTranslation(locale, System.IO.Path.Combine(languagePath, file));
            }
        }
    }

    private void AddTranslation(string locale, string path) {
        _logger.LogDebug("Adding translation for locale {Locale} from path {Path}", locale, path);
        var extension = System.IO.Path.GetExtension(path);
        switch (extension) {
            case ".mo": {
                var parser = new GettextMOTranslation();
                var bytes = FileAccess.GetFileAsBytes(path);
                parser.Load(bytes);
                var keys = parser.GetGettextKeys();
                var translation = new Translation {
                    Locale = locale
                };
                foreach (var key in keys) {
                    var translateKey = key.Key;
                    var value = key.Value;
                    if (string.IsNullOrEmpty(translateKey.ID)) continue;
                    translation.AddPluralMessage(translateKey.ID, value, translateKey.IDPlural);
                }

                TranslationServer.AddTranslation(translation);
                break;
            }
            case "po": {
                var parser = new GettextPOTranslation();
                var bytes = FileAccess.GetFileAsBytes(path);
                parser.Load(bytes);
                var keys = parser.GetGettextKeys();
                var translation = new Translation {
                    Locale = locale
                };
                foreach (var key in keys) {
                    var translateKey = key.Key;
                    var value = key.Value;
                    if (string.IsNullOrEmpty(translateKey.ID)) continue;
                    translation.AddPluralMessage(translateKey.ID, value, translateKey.IDPlural);
                }

                TranslationServer.AddTranslation(translation);
                break;
            }
        }
    }

    public string format(string key, string module, params object[] args) {
        if (string.IsNullOrEmpty(key)) {
            _logger.LogWarning("Translation key is null or empty.");
            return string.Empty;
        }

        var translation = TranslationServer.Translate(key, module);
        if (translation == key) {
            _logger.LogWarning("Translation for key '{Key}' not found.", key);
            return key;
        }

        if (args.Length > 0) {
            try {
                return string.Format(translation, args);
            } catch (FormatException e) {
                _logger.LogError(e, "Error formatting translation for key '{Key}' with args {Args}", key, args);
                return translation;
            }
        }

        return translation;
    }
}