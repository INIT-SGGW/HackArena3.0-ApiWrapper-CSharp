namespace src.HackArena3.Auth;

/// <summary>
/// Wyjątek rzucany w przypadku błędów związanych z procesem uwierzytelniania,
/// np. nieznalezienie pliku binarnego lub błąd wykonania.
/// </summary>
public class AuthException(string message, Exception? innerException = null)
    : Exception(message, innerException);