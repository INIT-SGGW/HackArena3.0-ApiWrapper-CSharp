namespace src.HackArena3.Auth;
internal class AuthException(string message, Exception? innerException = null)
    : Exception(message, innerException);