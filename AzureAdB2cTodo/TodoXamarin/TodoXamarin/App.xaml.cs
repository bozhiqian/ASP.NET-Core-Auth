using Microsoft.Identity.Client;
using Prism;
using Prism.Ioc;
using TodoXamarin.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Prism.Unity;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace TodoXamarin
{
    public partial class App : PrismApplication
    {
        public static PublicClientApplication PCA = null;

        // Azure AD B2C Coordinates
        public static string Tenant = "bqadb2c.onmicrosoft.com"; 
        public static string ClientId = "a0e368f7-8773-48d9-b20a-5d7414a13907"; 
        public static string PolicySignUpSignIn = "B2C_1_sign_up_in"; 
        public static string PolicyEditProfile = "B2C_1_edit_profile"; 
        public static string PolicyResetPassword = "B2C_1_reset_password";

        // https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-access-tokens
        public static string[] Scopes = { "https://bqadb2c.onmicrosoft.com/api/todo/demo.read openid offline_access" }; 

        // This has to be deployed to your own azure app service. Note: https://localhost:44317/api/todo can not accessed by Xamarin App. 
        public static string ApiEndpoint = "https://bqadb2c.azurewebsites.net/api/todo";

        public static string AuthorityBase = $"https://login.microsoftonline.com/tfp/{Tenant}/";
        public static string Authority = $"{AuthorityBase}{PolicySignUpSignIn}";
        public static string AuthorityEditProfile = $"{AuthorityBase}{PolicyEditProfile}";
        public static string AuthorityPasswordReset = $"{AuthorityBase}{PolicyResetPassword}";

        public static UIParent UiParent = null;

        /* 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */
        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        protected override async void OnInitialized()
        {
            InitializeComponent();

            // default redirectURI; each platform specific project will have to override it with its own
            // In azure registered this xamarin app, the "Custom Redirect URI" needs to be set to msal{ClientId}://auth, e.g. msala0e368f7-8773-48d9-b20a-5d7414a13907://auth
            PCA = new PublicClientApplication(ClientId, Authority) { RedirectUri = $"msal{ClientId}://auth" };

            await NavigationService.NavigateAsync("NavigationPage/MainPage");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<MainPage>();
        }
    }
}
