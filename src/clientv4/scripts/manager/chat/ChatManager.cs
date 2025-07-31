using System;
using System.Collections.Generic;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using ModLoader.archive;

namespace game.scripts.manager.chat;

public class ChatManager: IReset, IArchive, IDisposable {
    public delegate void MessageAddedHandler(MessageInfo message);
    private const string ArchiveFilename = "chat/chat-history.log";
    private readonly Queue<MessageInfo> _messages = new();
    public event MessageAddedHandler OnMessageAdded;
    public static ChatManager instance { get; private set; } = new();

    public void AddMessage(MessageInfo message) {
        _messages.Enqueue(message);
        OnMessageAdded?.Invoke(message);
    }
    
    public struct MessageInfo {
        public ulong Timestamp;
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

    public void Archive(Dictionary<string, byte[]> fileList) {
        List<string> waitWriteLines = [];
        while (_messages.Count > 0) {
            var line = _messages.Dequeue();
            waitWriteLines.Add($"[{line.Timestamp}] {line.Message}");
        }
        if (waitWriteLines.Count > 0) {
            var data = string.Join("\n", waitWriteLines);
            fileList[ArchiveFilename] = System.Text.Encoding.UTF8.GetBytes(data);
        }
    }
    
    public void Recover(Func<string, byte[]> getDataFunc) {
        var data = getDataFunc(ArchiveFilename);
        if (data == null) return;
        
        var lines = System.Text.Encoding.UTF8.GetString(data).Split('\n');
        foreach (var line in lines) {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split([' '], 3);
            if (parts.Length < 3) continue;
            if (!ulong.TryParse(parts[0].Trim('[', ']'), out var timestamp)) continue;
            var message = string.Join(' ', parts, 1, parts.Length - 1).Trim();
            AddMessage(new MessageInfo { Timestamp = timestamp, Message = message });
        }
    }
}