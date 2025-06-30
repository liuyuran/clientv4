using System;

namespace game.scripts.exception;

/// <summary>
/// base exception class for the game.
/// </summary>
public class BaseException: Exception {
    public BaseException(string message) : base(message) {
    }

    public BaseException(string message, Exception innerException) : base(message, innerException) {
    }
}