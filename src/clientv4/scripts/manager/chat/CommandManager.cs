using System;
using System.Collections.Generic;
using game.scripts.manager.chat.command;
using game.scripts.manager.reset;
using game.scripts.utils;
using ModLoader;
using ModLoader.chat;
using ModLoader.handler;

namespace game.scripts.manager.chat;

public class CommandManager : IReset, ICommandManager, IDisposable {
    public static CommandManager instance { get; private set; } = new();
    private readonly Dictionary<string, ICommand> _commands = new();

    private CommandManager() {
        Registry<HelpCommand>();
    }
    
    public void Registry<T>() where T : ICommand, new() {
        var command = new T();
        var commandName = command.name;
        if (!_commands.TryAdd(commandName, command)) {
            throw new InvalidOperationException($"Command '{commandName}' is already registered.");
        }
    }
    
    public ICommand[] GetCommands() {
        var commands = new ICommand[_commands.Count];
        _commands.Values.CopyTo(commands, 0);
        return commands;
    }

    public (string commandName, string[] args)? TryParseCommand(string message) {
        // /{commandName} arg1 arg2 ...
        if (string.IsNullOrWhiteSpace(message)) return null;
        if (message[0] != '/') return null;
        var parts = message[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        var commandName = parts[0];
        var args = parts.Length > 1 ? parts[1..] : [];
        return (commandName, args);
    }
    
    public void ExecuteCommand(ulong sender, string commandName, params string[] args) {
        if (_commands.TryGetValue(commandName, out var command)) {
            command.Execute(sender, args);
        } else {
            ChatManager.instance.ReceiveMessage(new MessageInfo {
                Timestamp = PlatformUtil.GetTimestamp(),
                Message = I18N.Tr("core.command", "command-not-found", commandName),
            });
        }
    }

    public void Reset() {
        instance = new CommandManager();
        Dispose();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}