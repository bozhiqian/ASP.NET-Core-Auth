using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace TodoWebClient.Token
{
    public static class TokenRevocationExtension
    {
        public static async Task RevokeAccessTokenAsync(this TokenRevocationClient revocationClient, HttpContext httpContext)
        {
            // get access token
            var accessToken = await httpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // revoke access token
                var revokeAccessTokenResponse = await revocationClient.RevokeAccessTokenAsync(accessToken);
                if (revokeAccessTokenResponse.IsError)
                {
                    throw new Exception("Error occurred during revocation of access token", revokeAccessTokenResponse.Exception);
                }
            }
        }

        public static async Task RevokeRefreshTokenAsync(this TokenRevocationClient revocationClient, HttpContext httpContext)
        {
            // get refresh token
            var refreshToken = await httpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                // revoke refresh token
                var revokeRefreshTokenResponse = await revocationClient.RevokeRefreshTokenAsync(refreshToken);
                if (revokeRefreshTokenResponse.IsError)
                {
                    throw new Exception("Error occurred during revocation of refresh token", revokeRefreshTokenResponse.Exception);
                }
            }
        }
    }
}
