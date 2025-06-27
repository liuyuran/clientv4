using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace UnitTest;

[TestSuite]
public class ExampleTest
{
    // Fast execution - no Godot runtime needed
    [TestCase]
    public void IsEqual()
    {
        AssertThat("This is a test message").IsEqual("This is a test message");
    }
        
    // Godot runtime required for Node operations
    [TestCase] 
    [RequireGodotRuntime]
    public void TestGodotNode()
    {
        AssertThat(new Node2D()).IsNotNull();
    }
}