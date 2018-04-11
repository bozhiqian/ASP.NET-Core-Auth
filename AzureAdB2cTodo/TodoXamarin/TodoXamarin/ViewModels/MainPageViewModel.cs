using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using TodoXamarin.Events;

namespace TodoXamarin.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private string _apiText;
        private string _id;
        private string _name;
        private string _signInSignOutButtonText;

        public MainPageViewModel(INavigationService navigationService, IEventAggregator eventAggregator) : base(navigationService, eventAggregator)
        {
            Title = "MSAL Xamarin Forms Sample";

            CallApiCommand = new DelegateCommand(CallApi, () => IsSignedIn);
            SignInSignOutCommand = new DelegateCommand(SignInSignOut);
            EditProfileCommand = new DelegateCommand(EditProfile, () => IsSignedIn);
        }

        #region Properties
        
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string ApiText
        {
            get => _apiText;
            set => SetProperty(ref _apiText, value);
        }

        public string SignInSignOutButtonText
        {
            get => _signInSignOutButtonText;
            set => SetProperty(ref _signInSignOutButtonText, value);
        }

        public bool IsSignedIn { get; set; }
        #endregion

        public DelegateCommand CallApiCommand { get; }
        public DelegateCommand SignInSignOutCommand { get; }
        public DelegateCommand EditProfileCommand { get; }

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            UpdateSignInState(false);

            // Check to see if we have a User
            // in the cache already.
            try
            {
                var user = GetUserByPolicy(App.PCA.Users, App.PolicySignUpSignIn);
                var ar = await App.PCA.AcquireTokenSilentAsync(App.Scopes, user, App.Authority, false);
                UpdateUserInfo(ar);
                UpdateSignInState(true);
            }
            catch (Exception ex)
            {
                // Uncomment for debugging purposes
                DisplayAlert($"Exception:", ex.ToString(), "Dismiss");

                // Doesn't matter, we go in interactive mode
                UpdateSignInState(false);
            }
        }

        private IUser User => GetUserByPolicy(App.PCA.Users, App.PolicySignUpSignIn);
        private async void EditProfile()
        {
            try
            {
                // KNOWN ISSUE:
                // User will get prompted 
                // to pick an IdP again.
                var ar = await App.PCA.AcquireTokenAsync(App.Scopes, User, UIBehavior.SelectAccount, string.Empty, null, App.AuthorityEditProfile, App.UiParent);
                UpdateUserInfo(ar);
            }
            catch (Exception ex)
            {
                // Alert if any exception excludig user cancelling sign-in dialog
                if ((ex as MsalException)?.ErrorCode != "authentication_canceled")
                    DisplayAlert($"Exception:", ex.ToString(), "Dismiss");
            }
        }

        private async void SignInSignOut()
        {
            try
            {
                if (SignInSignOutButtonText == "Sign in")
                {
                    var ar = await App.PCA.AcquireTokenAsync(App.Scopes, User, App.UiParent);
                    UpdateUserInfo(ar);
                    UpdateSignInState(true);
                }
                else
                {
                    foreach (var user in App.PCA.Users) App.PCA.Remove(user);
                    UpdateSignInState(false);
                }
            }
            catch (Exception ex)
            {
                // Checking the exception message 
                // should ONLY be done for B2C
                // reset and not any other error.
                if (ex.Message.Contains("AADB2C90118"))
                    OnPasswordReset();
                // Alert if any exception excludig user cancelling sign-in dialog
                else if ((ex as MsalException)?.ErrorCode != "authentication_canceled")
                    DisplayAlert($"Exception:", ex.ToString(), "Dismiss");
            }
        }

        private async void CallApi()
        {
            try
            {
                ApiText = $"Calling API {App.ApiEndpoint}";
                var ar = await App.PCA.AcquireTokenSilentAsync(App.Scopes, User, App.Authority, false);

                // https://stackoverflow.com/questions/47800830/azure-ad-b2c-acquiretokensilentasync-returns-empty-access-token
                // https://dzimchuk.net/setting-up-your-asp-net-core-2-0-apps-and-services-for-azure-ad-b2c/
                var token = ar.AccessToken; 

                // Get data from API
                var response = await RequestAsync(HttpMethod.Get, App.ApiEndpoint, token);
                
                var responseString = await response.Content.ReadAsStringAsync();
                ApiText = response.IsSuccessStatusCode ? $"Response from API {App.ApiEndpoint} | {responseString}" : $"Error calling API {App.ApiEndpoint} | {responseString}";
            }
            catch (MsalUiRequiredException ex)
            {
                DisplayAlert($"Session has expired, please sign out and back in.", ex.ToString(), "Dismiss");
            }
            catch (Exception ex)
            {
                DisplayAlert($"Exception:", ex.ToString(), "Dismiss");
            }
        }

        private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string apiUrl, string accessToken)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(method, apiUrl);

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.SendAsync(request);

            return response;
        }

        private async void OnPasswordReset()
        {
            try
            {
                var ar = await App.PCA.AcquireTokenAsync(App.Scopes, (IUser) null, UIBehavior.SelectAccount,
                    string.Empty, null, App.AuthorityPasswordReset, App.UiParent);
                UpdateUserInfo(ar);
            }
            catch (Exception ex)
            {
                // Alert if any exception excludig user cancelling sign-in dialog
                if ((ex as MsalException)?.ErrorCode != "authentication_canceled")
                    DisplayAlert($"Exception:", ex.ToString(), "Dismiss");
            }
        }
        private void UpdateUserInfo(AuthenticationResult ar)
        {
            var user = ParseIdToken(ar.IdToken);
            Name = user["name"]?.ToString();
            Id = user["oid"]?.ToString();
        }

        private void UpdateSignInState(bool isSignedIn)
        {
            IsSignedIn = isSignedIn;
            SignInSignOutButtonText = isSignedIn ? "Sign out" : "Sign in";
            EditProfileCommand.RaiseCanExecuteChanged();
            CallApiCommand.RaiseCanExecuteChanged();

            ApiText = "";
        }

        private JObject ParseIdToken(string idToken)
        {
            // Get the piece with actual user info
            idToken = idToken.Split('.')[1];
            idToken = Base64UrlDecode(idToken);
            return JObject.Parse(idToken);
        }

        private IUser GetUserByPolicy(IEnumerable<IUser> users, string policy)
        {
            foreach (var user in users)
            {
                var userIdentifier = Base64UrlDecode(user.Identifier.Split('.')[0]);
                if (userIdentifier.EndsWith(policy.ToLower())) return user;
            }

            return null;
        }

        private string Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var byteArray = Convert.FromBase64String(s);
            var decoded = Encoding.UTF8.GetString(byteArray, 0, byteArray.Count());
            return decoded;
        }

        private void DisplayAlert(string title, string message, string cancel)
        {
            EventAggregator.GetEvent<AlertEvent>().Publish(new AlertEventArgs(message) { Title = title, Cancel = cancel });
        }
    }
}