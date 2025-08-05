using ModLoader.chat;

namespace ModLoader.handler;

public interface IChatManager {
    public void BroadcastMessage(MessageInfo message);
}