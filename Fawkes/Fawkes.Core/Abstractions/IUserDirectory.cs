namespace Fawkes.Core.Abstractions
{
    /// <summary>
    /// Looks up registered user accounts. Implemented by the API on top of ASP.NET Identity.
    /// </summary>
    public interface IUserDirectory
    {
        Task<bool> UserExistsAsync(string userName);
    }
}
