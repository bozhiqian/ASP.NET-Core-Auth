using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServerWithAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServerWithAspNetIdentity.Extensions
{
    public static class ApplicationUserExtensions
    {
        public static async Task<int> AddUserClaimsAsync<TUser>(this UserManager<TUser> userManager, TUser user, List<Claim> claims) where TUser : class
        {
            if (claims.Any())
            {
                var existingUserClaims = await userManager.GetClaimsAsync(user);
                foreach (var existingUserClaim in existingUserClaims)
                {
                    var c = claims.FirstOrDefault(p => p.Type == existingUserClaim.Type);
                    if (c != null)
                    {
                        claims.Remove(c);
                    }
                }

                if (claims.Any())
                {
                    var identityResult = await userManager.AddClaimsAsync(user, claims);
                    if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);
                }
            }

            return claims.Count;
        }

        public static async Task<int> AddUserLoginsAsync<TUser>(this UserManager<TUser> userManager, TUser user, string provider, string providerUserId) where TUser : class
        {
            var existingUserLoginInfos = await userManager.GetLoginsAsync(user);
            foreach (var existingUserLoginInfo in existingUserLoginInfos)
            {
                if (existingUserLoginInfo.LoginProvider != provider)
                {
                    var identityResult = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerUserId, provider));
                    if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);
                    return 1;
                }
            }

            return 0;
        }
    }
}
