using game.scripts.utils;
using ModLoader;
using ModLoader.chat;

namespace game.scripts.manager.chat.command;

public class HelpCommand : ICommand {
    public string name => "help";
    public string desc => I18N.Tr("core.command", "help.desc");

    public void Execute(ulong sender, string[] args) {
        var commands = CommandManager.instance.GetCommands();
        var message = "";
        if (commands.Length == 0) {
            message = I18N.Tr("core.command", "help.no-commands");
        } else {
            message = I18N.Tr("core.command", "help.list-header");
            foreach (var command in commands) {
                message += $"\n/{command.name} - {command.desc}";
            }
        }
        ChatManager.instance.SendMessage(sender, new MessageInfo {
            Timestamp = PlatformUtil.GetTimestamp(),
            Message = message,
        });
    }
}