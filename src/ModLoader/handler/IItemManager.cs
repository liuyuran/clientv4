using ModLoader.item;

namespace ModLoader.handler;

public interface IItemManager {
    public void Register<T>() where T : Item, new();
}