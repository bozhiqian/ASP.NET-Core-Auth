using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthenticationDemo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AuthenticationDemo.Claims
{
    // https://github.com/aspnet/Identity/blob/87a956e49415581e3767d8de66a1f5a326f02194/src/Identity/SignInManager.cs#L188 
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, 
            IOptions<IdentityOptions> optionsAccessor) 
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        /*
         It is invoked by 
         UserClaimsPrincipalFactory.CreateAsync(TUser user) <==  SignInManager.CreateUserPrincipalAsync(TUser user) 
         <== SignInManager.CreateUserPrincipalAsync(TUser user) <== SignInManager.SignInAsync(TUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null)

        So it is called when user is signing each time.
         */
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // https://github.com/aspnet/Identity/blob/329eed9e8d14243d0b36385bb1adc9fc85df0e41/src/Core/UserClaimsPrincipalFactory.cs#L82
            var identity = await base.GenerateClaimsAsync(user);
            
            return ClaimsIdentityClaimsUpdate(user, identity);
        }

        //public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        //{
        //    var principal = await base.CreateAsync(user);
        //    var identity = (ClaimsIdentity) principal.Identity;
        //    ClaimsIdentityClaimsUpdate(user, identity);

        //    return principal;
        //}

        private ClaimsIdentity ClaimsIdentityClaimsUpdate(ApplicationUser user, ClaimsIdentity identity)
        {
            var existingClaims = identity.Claims.ToList();

            if (existingClaims.All(c => c.Type != "locale"))
            {
                identity.AddClaim(new Claim("locale", user.Locale));
            }

            // Remove duplicated claims from 'identity' in memory that are added each time at signing by base.GenerateClaimsAsync(user). 
            var claims = existingClaims.GroupBy(c => c.Type).Select(p => p.First()).ToList();
            foreach (var claim in existingClaims)
            {
                identity.RemoveClaim(claim);
            }
            identity.AddClaims(claims);

            return identity;
        }
    }
}

/*
 in the case of using SginInManager to SignInAsync(...), the ClaimPrincipal is generated from TUser before HttpContext.SignInAsync(...) to be called. And this is in case of using asp.net core identity.
 */
