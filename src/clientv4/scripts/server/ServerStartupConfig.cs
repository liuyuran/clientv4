namespace game.scripts.server;

/// <summary>
/// start-game config instance, must set it before start game
/// </summary>
public class ServerStartupConfig {
    public static ServerStartupConfig instance { get; } = new();
    
    public bool isLocalServer { get; set; } = true;
    public string serverIp { get; set; } = "127.0.0.1";
    public int serverPort { get; set; } = 7000;
    public string nickname { get; set; } = "local player";
    public bool openBroadcast { get; set; } = false;
    public string serverName { get; set; } = "local server";
    public string serverDesc { get; set; } = "local server description";
}