namespace src.HackArena3.Auth;

/// <summary>
/// Wyjątek rzucany w przypadku błędów związanych z procesem uwierzytelniania,
/// np. nieznalezienie pliku binarnego lub błąd wykonania.
/// </summary>
internal class AuthException(string message, Exception? innerException = null)
    : Exception(message, innerException);