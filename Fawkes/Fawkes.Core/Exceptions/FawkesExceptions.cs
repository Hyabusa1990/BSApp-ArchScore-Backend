namespace Fawkes.Core.Exceptions
{
    /// <summary>
    /// Base class for expected business errors. The API maps each subtype to an HTTP status code;
    /// <see cref="Code"/> is passed to clients as a machine-readable error code.
    /// </summary>
    public abstract class FawkesException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }

    /// <summary>
    /// The requested resource does not exist (HTTP 404).
    /// </summary>
    public class NotFoundException(string message, string code = "NOT_FOUND") : FawkesException(code, message);

    /// <summary>
    /// The user is known but not allowed to perform the operation (HTTP 403).
    /// </summary>
    public class AccessDeniedException(string message, string code = "ACCESS_DENIED") : FawkesException(code, message);

    /// <summary>
    /// The operation is not possible in the current state of the resource (HTTP 409).
    /// </summary>
    public class ConflictException(string message, string code = "CONFLICT") : FawkesException(code, message);

    /// <summary>
    /// The request itself is invalid, independent of the current state (HTTP 400).
    /// </summary>
    public class InvalidRequestException(string message, string code = "INVALID_REQUEST") : FawkesException(code, message);
}
