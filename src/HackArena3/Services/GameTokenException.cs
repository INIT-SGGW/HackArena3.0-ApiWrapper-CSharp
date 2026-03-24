namespace src.HackArena3.Services;

/// <summary>
/// Wyjątek rzucany w przypadku błędów podczas pozyskiwania tokenu gry.
/// </summary>
internal class GameTokenException(string message, Exception? innerException = null)
    : Exception(message, innerException);
