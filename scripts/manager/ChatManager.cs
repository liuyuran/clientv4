namespace game.scripts.manager;

public class ChatManager {
    public delegate void MessageAddedHandler(MessageInfo message);
    public event MessageAddedHandler OnMessageAdded;
    public static ChatManager instance { get; private set; } = new();

    public void AddMessage(MessageInfo message) {
        OnMessageAdded?.Invoke(message);
    }
    
    public struct MessageInfo {
        public string Message;
    }
}