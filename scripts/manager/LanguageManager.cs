using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using SecondLanguage;
using Translation = Godot.Translation;

namespace game.scripts.manager;

public class LanguageManager {
    public static LanguageManager instance { get; private set; } = new();
    
    private LanguageManager() {
        ReloadLanguageFiles();
    }

    private void ReloadLanguageFiles() {
        var allResourcePacks = ResourcePackManager.instance.GetAllResourcePacks();
        foreach (var pack in allResourcePacks) {
            var languagePath = System.IO.Path.Combine(pack.path, "language");
            if (!DirAccess.DirExistsAbsolute(languagePath)) continue;
            foreach (var file in DirAccess.GetFilesAt(languagePath)) {
                if (file.EndsWith(".po") || file.EndsWith(".mo")) {
                    var locale = System.IO.Path.GetFileNameWithoutExtension(file);
                    AddTranslation(locale, System.IO.Path.Combine(languagePath, file));
                }
            }
        }
    }
    
    private void AddTranslation(string locale, string path) {
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