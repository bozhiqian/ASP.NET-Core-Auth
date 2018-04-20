using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer.Services;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers.UserRegistration
{
    public class UserRegistrationController:Controller
    {
        public UserRegistrationController(IUserRepository userRepository, IIdentityServerInteractionService interactionService) {
            UserRepository = userRepository;
            InteractionService = interactionService;
        }

        public IUserRepository UserRepository { get; }
        public IIdentityServerInteractionService InteractionService { get; }

        public IActionResult RegisterUser(RegistrationInputModel registrationInputModel)
        {
            return View(new RegisterUserViewModel {
                Provider = registrationInputModel.Provider,
                ProviderUserId = registrationInputModel.ProviderUserId,
                ReturnUrl = registrationInputModel.ReturnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // create user + claims
                var userToCreate = new Entities.User
                {
                    Password = model.Password,
                    Username = model.Username,
                    IsActive = true,
                    Claims = new List<Entities.UserClaim>()
                                       {
                                           new Entities.UserClaim("country", model.Country),
                                           new Entities.UserClaim("address", model.Address),
                                           new Entities.UserClaim("given_name", model.Firstname),
                                           new Entities.UserClaim("family_name", model.Lastname),
                                           new Entities.UserClaim("emailaddress", model.Email),
                                           new Entities.UserClaim("subscriptionlevel", "FreeUser"),
                                           new Entities.UserClaim("mobile", model.Mobile),
                                       }
                };

                if(model.IsExternalProvider)
                {
                    // Add external user identity to the new user's UserLogins. 
                    userToCreate.Logins.Add(new Entities.UserLogin
                    {
                        LoginProvider = model.Provider,
                        ProviderKey = model.ProviderUserId
                    });
                }
                // add it through the repository
                UserRepository.AddUser(userToCreate);

                if (!UserRepository.Save())
                {
                    throw new Exception($"Creating a user failed.");
                }

                if (!model.IsExternalProvider)
                {
                    // log the user in
                    await HttpContext.SignInAsync(userToCreate.SubjectId, userToCreate.Username);
                }
                // continue with the flow     
                if (InteractionService.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl); // ReturnUrl: /account/ExternalLoginCallback?returnUrl=%2Fgrants
                }

                return Redirect("~/");
            }

            // ModelState invalid, return the view with the passed-in model
            // so changes can be made
            return View(model);
        }
    }
}
