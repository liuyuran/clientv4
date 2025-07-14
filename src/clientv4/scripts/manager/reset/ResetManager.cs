using System;
using System.Linq;
using game.scripts.utils;

namespace game.scripts.manager.reset;

public class ResetManager {
    public static void Reset() {
        var resetTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IReset).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
        foreach (var type in resetTypes) {
            // 获取类型上名为instance的静态字段，此字段为自身类型的单例
            var instanceField = type.GetField("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceField == null) continue;
            var instance = instanceField.GetValue(null);
            if (instance is not IReset resetInstance) continue;
            resetInstance.Reset();
        }
        GameNodeReference.UI = null;
        GameStatus.SetStatus(GameStatus.Status.StartMenu);
    }
}