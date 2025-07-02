namespace game.scripts.utils;

public static class GameStatus {
    public enum Status {
        Starting,
        StartMenu,
        Loading,
        Playing
    }
    
    public static Status currentStatus { get; private set; } = Status.Loading;
    
    public static void SetStatus(Status status) {
        currentStatus = status;
    }
}