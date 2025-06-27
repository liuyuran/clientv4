using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Microsoft.Extensions.Logging;
using ModLoader.logger;
using SecondLanguage;
using Translation = Godot.Translation;

namespace game.scripts.manager;

public class LanguageManager {
    private readonly ILogger _logger = LogManager.GetLogger<LanguageManager>();
    public static LanguageManager instance { get; private set; } = new();

    private LanguageManager() {
        ReloadLanguageFiles();
    }

    private void ReloadLanguageFiles() {
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
                parser.Load(path);
                var keys = parser.GetGettextKeys();
                var translation = new Translation {
                    Locale = locale
                };
                foreach (var key in keys) {
                    var translateKey = key.Key;
                    var value = key.Value;
                    translation.AddPluralMessage(translateKey.ID, value, translateKey.IDPlural);
                }

                TranslationServer.AddTranslation(translation);
                break;
            }
            case "po": {
                var parser = new GettextPOTranslation();
                parser.Load(path);
                var keys = parser.GetGettextKeys();
                var translation = new Translation {
                    Locale = locale
                };
                foreach (var key in keys) {
                    var translateKey = key.Key;
                    var value = key.Value;
                    translation.AddPluralMessage(translateKey.ID, value, translateKey.IDPlural);
                }

                TranslationServer.AddTranslation(translation);
                break;
            }
        }
    }
}