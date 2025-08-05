using System;
using System.Collections.Generic;
using game.scripts.manager.chat;
using game.scripts.utils;
using Godot;
using JetBrains.Annotations;
using ModLoader.chat;

namespace game.unittest.manager;

[UsedImplicitly]
public class ChatManagerUnitTest: IUnitTest {
    private List<MessageInfo> _message = [];
    
    public void RunTest(Node node, Action<string> log) {
        try {
            ChatManager.instance.OnMessageAdded += OnInstanceOnOnMessageAdded;
            ChatManager.instance.ReceiveMessage(new MessageInfo {
                Timestamp = PlatformUtil.GetTimestamp(),
                Message = "Test message"
            });
            if (_message.Count != 1) {
                log($"ChatManagerUnitTest failed: expected 1 message, got {_message.Count}");
                return;
            }
            if (_message[0].Message != "Test message") {
                log($"ChatManagerUnitTest failed: expected 'Test message', got '{_message[0].Message}'");
                return;
            }
            Dictionary<string, byte[]> fileBuffer = new();
            ChatManager.instance.Archive(fileBuffer);
            ChatManager.instance.Reset();
            _message = [];
            ChatManager.instance.OnMessageAdded += OnInstanceOnOnMessageAdded;
            ChatManager.instance.Recover(s => fileBuffer.GetValueOrDefault(s));
            if (_message.Count != 1) {
                log($"ChatManagerUnitTest failed: expected 1 message, got {_message.Count}");
                return;
            }
            if (_message[0].Message != "Test message") {
                log($"ChatManagerUnitTest failed: expected 'Test message', got '{_message[0].Message}'");
                return;
            }

            log("ChatManagerUnitTest pass");
        } catch (Exception e) {
            log($"ChatManagerUnitTest failed: {e.Message}");
        }
    }

    private void OnInstanceOnOnMessageAdded(MessageInfo msg) {
        _message.Add(msg);
    }

    public void Cleanup(Node node, Action<string> log) {
        // no need to clean up for ChatManager
    }
}