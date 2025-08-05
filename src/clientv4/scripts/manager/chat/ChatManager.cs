using System;
using System.Collections.Generic;
using game.scripts.gui.InGameUI;
using game.scripts.manager.player;
using game.scripts.manager.reset;
using game.scripts.utils;
using ModLoader.archive;
using ModLoader.chat;
using ModLoader.handler;

namespace game.scripts.manager.chat;

public class ChatManager: IReset, IChatManager, IArchive, IDisposable {
    public delegate void MessageAddedHandler(MessageInfo message);
    private const string ArchiveFilename = "chat/chat-history.log";
    private readonly Queue<MessageInfo> _messages = new();
    public event MessageAddedHandler OnMessageAdded;
    public static ChatManager instance { get; private set; } = new();

    public void ReceiveMessage(MessageInfo message) {
        _messages.Enqueue(message);
        OnMessageAdded?.Invoke(message);
    }
    
    public void BroadcastMessage(MessageInfo message) {
        if (GameNodeReference.UI == null || !GameNodeReference.UI.CanProcess()) {
            return; // UI not ready, skip broadcasting
        }
        GameNodeReference.UI.Rpc(InGamingUI.MethodName.BroadcastChatMessage, message.Timestamp, message.Message);
    }
    
    public void SendMessage(ulong playerId, MessageInfo message) {
        if (GameNodeReference.UI == null || !GameNodeReference.UI.CanProcess()) {
            return; // UI not ready, skip broadcasting
        }

        if (playerId == 0) {
            GameNodeReference.UI.BroadcastChatMessage(message.Timestamp, message.Message);
        } else {
            var player = PlayerManager.instance.GetPlayerById(playerId);
            GameNodeReference.UI.RpcId(player.peerId, InGamingUI.MethodName.ReceiveChatMessage, message.Timestamp, message.Message);            
        }
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
            ReceiveMessage(new MessageInfo { Timestamp = timestamp, Message = message });
        }
    }
}