namespace GoHardAPI.Services
{
    /// <summary>
    /// The uploaded file failed a profile-photo validation rule (size, declared
    /// type, or content signature). Its <see cref="System.Exception.Message"/> is
    /// safe to return to the caller as a 400; it never contains file bytes,
    /// tokens, or paths.
    /// </summary>
    public sealed class PhotoValidationException : Exception
    {
        public PhotoValidationException(string message) : base(message) { }
    }
}
