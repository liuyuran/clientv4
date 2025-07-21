using System;
using Godot;

namespace game.unittest;

public interface IUnitTest {
    public void RunTest(Node node, Action<string> log);
    public void Cleanup(Node node, Action<string> log);
}