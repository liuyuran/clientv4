using System;
using System.Linq;
using System.Reflection;
using game.scripts.utils;

namespace game.scripts.manager.reset;

public class ResetManager {
    public static void Reset() {
        var resetTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IReset).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
        foreach (var type in resetTypes) {
            // 获取类型上名为instance的静态字段，此字段为自身类型的单例
            using var fields = type.GetRuntimeFields().GetEnumerator();
            while (fields.MoveNext()) {
                var field = fields.Current;
                if (field == null) continue;
                if (field.IsStatic && field.Name.Contains("<instance>", StringComparison.OrdinalIgnoreCase)) {
                    // 如果有名为instance的静态字段，则直接获取该字段的值
                    var instance = field.GetValue(null);
                    if (instance is IReset resetInstance) {
                        resetInstance.Reset();
                    }

                    break;
                }
            }
        }
        GameNodeReference.UI = null;
        GameStatus.SetStatus(GameStatus.Status.StartMenu);
    }
}