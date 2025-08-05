namespace ModLoader.chat;

public interface ICommand {
    public string name { get; }
    public string desc { get; }
    public void Execute(ulong sender, string[] args);
}