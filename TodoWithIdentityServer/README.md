# Deep dive in using IdentityServer4 to perform authentication for ASP.NET Core Api and Web app.

This Demo code was heavily referenced from the "[IdentityServer4Demo](https://github.com/fhrn71/IdentityServer4Demo)" at start up code, and then to be changed quite a lot implementation code plus adding few new features. 

There have a lot comments in code to explain in details. The solution contains 3 projects which are IdentityServer, TodoApi(web api) and TodoWebClient(mvc client). All users authentication requests from "TodoApi" and "TodoWebClient" are handled by IdentityServer, while authorization are managed by "TodoApi".


**Below are some of key features covered in this demo solution.**

1. Customizing [**IdentityServer4**](http://docs.identityserver.io/en/release/) UI with adding User Registration.
2. Securing ASP.NET Core Web client application with **[OpenID Connect](http://openid.net/connect/)** at Authentication by IdentityServer.
3. Securing ASP.NET Core Api with **[OAuth2](https://oauth.net/2/)** at Authentication by IdentityServer and Authorization by Policy.
4. **Hybrid Flow**, UserInfo Endpoint, Identity Token, Access Token.
5. Claims Transformation, Attribute-based Access Control, Role-based Access Control, Authorization Policy.
6. Refresh Tokens, Reference Tokens and Revocation, Revoking Tokens.
7. Integrating custom user database with IdentityServer.
8. Persisting Configuration and Operational data store into SQL Server.
8. Working with External Identity Providers such as Google, Microsoft, Twitter and Facebook.
9. Mapping user login for external provider to an existing user account.
10. **2-Factor** Authentication, sending verification code with Twilio.


**References:**

1. [Quickstart UI for an in-memory IdentityServer4 v2](https://github.com/IdentityServer/IdentityServer4.Quickstart.UI)

1. [Getting Started with IdentityServer 4](https://www.scottbrady91.com/Identity-Server/Getting-Started-with-IdentityServer-4)

1. [Authenticate with OAuth 2.0 in ASP.NET Core 2.0](https://www.jerriepelser.com/blog/authenticate-oauth-aspnet-core-2/)

1.  [IdentityServer4 Documenting](https://identityserver4.readthedocs.io/en/release/)
2.  [Why use OpenID Connect instead of plain OAuth2?](https://security.stackexchange.com/questions/37818/why-use-openid-connect-instead-of-plain-oauth2)

3.  [When To Use Which (OAuth2) Grants and (OIDC) Flows](https://medium.com/@robert.broeckelmann/when-to-use-which-oauth2-grants-and-oidc-flows-ec6a5c00d864)
