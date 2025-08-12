namespace ModLoader.scene;

public abstract class NodeController {
    public delegate void OnClick(string name);
    
    public OnClick? ClickEvent;
}