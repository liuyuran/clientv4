namespace game.scripts.utils;

public static class GameStatus {
    public enum Status {
        StartMenu, // in the start menu
        Loading, // in loading screen
        Typing, // game is in input to chat window state
        Playing // game is in the main play state
    }
    
    public static Status currentStatus { get; private set; } = Status.Loading;
    
    public static void SetStatus(Status status) {
        currentStatus = status;
    }
}