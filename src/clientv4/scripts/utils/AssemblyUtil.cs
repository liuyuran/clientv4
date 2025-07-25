using System.Reflection;

namespace game.scripts.utils;

public static class AssemblyUtil {
    public static string GetMetadata(this Assembly assembly, string key) {
        var attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
        foreach (var attribute in attributes) {
            var metadata = (AssemblyMetadataAttribute)attribute;
            if (metadata.Key == key) return metadata.Value;
        }
        return string.Empty;
    }
}