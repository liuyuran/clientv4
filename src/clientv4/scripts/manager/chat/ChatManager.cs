using System;
using game.scripts.manager.reset;

namespace game.scripts.manager.chat;

public class ChatManager: IReset, IDisposable {
    public delegate void MessageAddedHandler(MessageInfo message);
    public event MessageAddedHandler OnMessageAdded;
    public static ChatManager instance { get; private set; } = new();

    public void AddMessage(MessageInfo message) {
        OnMessageAdded?.Invoke(message);
    }
    
    public struct MessageInfo {
        public string Message;
    }

    public void Reset() {
        instance = new ChatManager();
        Dispose();
    }
    
    public void Dispose() {
        OnMessageAdded = null;
        GC.SuppressFinalize(this);
    }
}