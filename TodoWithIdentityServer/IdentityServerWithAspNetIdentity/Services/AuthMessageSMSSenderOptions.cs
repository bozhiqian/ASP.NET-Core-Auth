namespace IdentityServerWithAspNetIdentity.Services
{
    public class AuthMessageSMSSenderOptions
    {
        public string SID { get; set; }
        public string AuthToken { get; set; }
        public static string SendNumber { get; set; }
    }
}
