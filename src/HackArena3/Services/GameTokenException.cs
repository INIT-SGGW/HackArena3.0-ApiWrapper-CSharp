namespace src.HackArena3.Services;

internal class GameTokenException(string message, Exception? innerException = null)
    : Exception(message, innerException);
