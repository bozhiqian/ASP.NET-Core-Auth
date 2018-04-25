using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Twitter;
using Newtonsoft.Json.Linq;

namespace IdentityServerWithAspNetIdentity.Extensions
{
    public static class CreatingTicketExtensions
    {
        public static async Task CreatingTicket(this TwitterCreatingTicketContext context)
        {
            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
                            {
                                {"oauth_consumer_key", context.Options.ConsumerKey},
                                {"oauth_nonce", nonce},
                                {"oauth_signature_method", "HMAC-SHA1"},
                                {"oauth_timestamp", GenerateTimeStamp()},
                                {"oauth_token", context.AccessToken},
                                {"oauth_version", "1.0"}
                            };

            var parameterBuilder = new StringBuilder();
            foreach (var authorizationKey in authorizationParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&",
                    UrlEncoder.Default.Encode(authorizationKey.Key),
                    UrlEncoder.Default.Encode(authorizationKey.Value));
            }

            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var resource_url = "https://api.twitter.com/1.1/account/verify_credentials.json";
            var resource_query = "include_email=true";
            var canonicalizedRequestBuilder = new StringBuilder();
            canonicalizedRequestBuilder.Append(HttpMethod.Get.Method);
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Default.Encode(resource_url));
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Default.Encode(resource_query));
            canonicalizedRequestBuilder.Append("%26");
            canonicalizedRequestBuilder.Append(UrlEncoder.Default.Encode(parameterString));

            var signature = ComputeSignature(context.Options.ConsumerSecret, context.AccessTokenSecret, canonicalizedRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key,
                    UrlEncoder.Default.Encode(authorizationPart.Value));
            }

            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Get, resource_url + "?include_email=true");
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();

            var result = JObject.Parse(responseText);

            var email = result.Value<string>("email");
            var identity = (ClaimsIdentity)context.Principal.Identity;
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, email, ClaimValueTypes.String, "Twitter"));
            }
        }

        private static string GenerateTimeStamp()
        {
            var secondsSinceUnixEpocStart = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64(secondsSinceUnixEpocStart.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        private static string ComputeSignature(string consumerSecret, string tokenSecret, string signatureData)
        {
            using (var algorithm = new HMACSHA1())
            {
                algorithm.Key = Encoding.ASCII.GetBytes(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}&{1}",
                        UrlEncoder.Default.Encode(consumerSecret),
                        string.IsNullOrEmpty(tokenSecret) ? string.Empty : UrlEncoder.Default.Encode(tokenSecret)));
                var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(signatureData));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
