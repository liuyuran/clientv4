using ModLoader.chat;

namespace ModLoader.handler;

public interface ICommandManager {
    public void Registry<T>() where T : ICommand, new();
    public void ExecuteCommand(ulong sender, string commandName, params string[] args);
}