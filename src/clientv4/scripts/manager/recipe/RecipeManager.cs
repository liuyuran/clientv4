using System;
using game.scripts.manager.reset;
using ModLoader.handler;

namespace game.scripts.manager.recipe;

public class RecipeManager: IReset, IRecipeManager, IDisposable {
    public static RecipeManager instance { get; private set; } = new();

    public void Reset() {
        instance = new RecipeManager();
        Dispose();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}