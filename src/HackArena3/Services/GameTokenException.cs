namespace src.HackArena3.Services;

/// <summary>
/// Wyjątek rzucany w przypadku błędów podczas pozyskiwania tokenu gry.
/// </summary>
public class GameTokenException(string message, Exception? innerException = null)
    : Exception(message, innerException);
