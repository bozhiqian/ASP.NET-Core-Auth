using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthenticationDemo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationDemo.Claims
{
    public class ApplicationUserClaimsTransformation : IClaimsTransformation
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public ApplicationUserClaimsTransformation(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Each time HttpContext.AuthenticateAsync() or HttpContext.SignInAsync(...) is called the claims transformer is invoked. So this might be invoked multiple times. 
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identities.FirstOrDefault(x => x.IsAuthenticated);
            if (identity == null) return principal;

            var user = await _userManager.GetUserAsync(principal);
            if (user == null) return principal;

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

            if (user.PasswordHash != null)
            {
                claims.Add(new Claim(ClaimTypes.Hash, user.PasswordHash, ClaimValueTypes.String, "ProfileClaimsTransformation"));
            }
            
            var userRoles = await _userManager.GetRolesAsync(user);
            claims.AddRange(userRoles.Select(x => new Claim(ClaimTypes.Role, x, ClaimValueTypes.String, "ProfileClaimsTransformation")));

            identity.AddClaims(claims);

            var newIdentity = new ClaimsIdentity(claims, identity.AuthenticationType);
            // var newIdentity = new ClaimsIdentity(claims, identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType);
            return new ClaimsPrincipal(newIdentity);
        }
    }
}
/*
  'TransformAsync(ClaimsPrincipal principal)' is called after each time 'AuthenticateAsync()' is succeeded. And this is part of 'Microsoft.AspNetCore.Authentication.Core' so it is always available even when not using asp.net identity. 
  https://github.com/aspnet/HttpAbstractions/blob/e6919e550ff73806ecdf0643e74b44702e5f9caa/src/Microsoft.AspNetCore.Authentication.Core/AuthenticationService.cs#L72
 */
