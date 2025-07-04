using System;
using System.Linq;
using game.scripts.utils;
using Godot;
using Karambolo.PO;
using Microsoft.Extensions.Logging;
using ModLoader;
using ModLoader.language;
using ModLoader.logger;
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
            var poFiles = DirAccess.GetFilesAt(languagePath)
                .Where(file => file.EndsWith(".po"))
                .ToHashSet();
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
            case ".po": {
                var parser = new POParser(new POParserSettings());
                var bytes = FileUtil.RemoveBom(FileAccess.GetFileAsBytes(path));
                var result = parser.Parse(bytes);
                var keys = result.Catalog.Keys;
                var translation = new Translation {
                    Locale = locale
                };
                foreach (var key in keys) {
                    var translateKey = key.Id;
                    var contextKey = key.PluralId;
                    var value = result.Catalog[key].ToArray();
                    translation.AddPluralMessage(translateKey, value, contextKey);
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