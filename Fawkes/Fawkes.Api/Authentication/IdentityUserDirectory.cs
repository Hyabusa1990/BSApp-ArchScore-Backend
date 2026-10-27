using Fawkes.Core.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Fawkes.Api.Authentication
{
    public class IdentityUserDirectory(UserManager<IdentityUser> userManager) : IUserDirectory
    {
        public async Task<bool> UserExistsAsync(string userName)
        {
            return await userManager.FindByNameAsync(userName) != null;
        }
    }
}
