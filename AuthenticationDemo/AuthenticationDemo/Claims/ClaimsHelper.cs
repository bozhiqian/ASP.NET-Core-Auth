using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthenticationDemo.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationDemo.Claims
{
    public class ClaimsHelper
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;

        public ClaimsHelper(UserManager<ApplicationUser> userManager, IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
        {
            _userManager = userManager;
            _claimsPrincipalFactory = claimsPrincipalFactory;
        }

        public async Task<IdentityResult> AddOrReplaceClaimsAsync(ApplicationUser user, List<KeyValuePair<string, string>> claimKeyValuePairs = null)
        {
            var principal = await _claimsPrincipalFactory.CreateAsync(user);
            var claims = principal.Identities.First().Claims.ToList();

            var result = await AddOrReplaceClaimsAsync(user, claims, claimKeyValuePairs);

            return result;
        }

        public async Task<IdentityResult> AddOrReplaceClaimsAsync(ApplicationUser user, List<Claim> claims, List<KeyValuePair<string, string>> claimKeyValuePairs = null)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            List<Claim> newClaims = new List<Claim>();

            // Add or replace default claims
            var cps = claims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)).ToList();
            if (claimKeyValuePairs != null)
            {
                // append custom claims.
                cps.AddRange(claimKeyValuePairs);
            }

            // Add or replace claims
            foreach (var claimKeyValuePair in cps)
            {
                if (!string.IsNullOrEmpty(claimKeyValuePair.Value))
                {
                    var claim = await AddOrReplaceClaimAsync(user, userClaims, claimKeyValuePair.Key, claimKeyValuePair.Value);
                    if (claim != null)
                    {
                        // Add new claim.
                        newClaims.Add(claim);
                    }
                }
            }

            if (newClaims.Any())
            {
                var identityResult = await _userManager.AddClaimsAsync(user, newClaims);
                return identityResult;
            }

            return IdentityResult.Success;
        }

        private async Task<Claim> AddOrReplaceClaimAsync(ApplicationUser user, IList<Claim> userClaims, string claimType, string claimValue)
        {
            if (!string.IsNullOrEmpty(claimValue))
            {
                Claim claim = new Claim(claimType, claimValue);

                if (userClaims == null)
                {
                    userClaims = await _userManager.GetClaimsAsync(user);
                }
                var existingClaim = userClaims.FirstOrDefault(c => c.Type == claimType);
                if (existingClaim != null)
                {
                    if (existingClaim.Value != claimValue)
                    {
                        // Replace the claim.
                        var result = await _userManager.ReplaceClaimAsync(user, existingClaim, claim);
                        return null;
                    }
                }
                else
                {
                    // Add new claim.
                    return claim;
                }
            }

            return null;
        }

        private async Task<Claim> AddOrReplaceClaimAsync(ApplicationUser user, string claimType, string claimValue)
        {
            var claim = await AddOrReplaceClaimAsync(user, null, claimType, claimValue);
            return claim;
        }
    }
}
