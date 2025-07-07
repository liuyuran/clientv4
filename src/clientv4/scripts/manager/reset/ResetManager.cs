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
            var resetInstance = (IReset)Activator.CreateInstance(type);
            resetInstance?.Reset();
        }
        GameNodeReference.UI = null;
        GameStatus.SetStatus(GameStatus.Status.StartMenu);
    }
}