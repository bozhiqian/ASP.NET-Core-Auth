using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace IdentityServer.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        public AuthMessageSender(IOptions<AuthMessageSMSSenderOptions> optionsAccessor)
        {
            Options = optionsAccessor.Value;
        }

        public AuthMessageSMSSenderOptions Options { get; }  

        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }

        public Task SendSmsAsync(string mobile, string message)
        {
            try
            {
                Random generator = new Random();
                AuthMessageSMSSenderOptions.SendNumber = generator.Next(1, 10000).ToString("D4");

                TwilioClient.Init(Options.SID, Options.AuthToken);

                var to = new PhoneNumber("+61" + mobile);
                var result = MessageResource.Create(
                    to,
                    from: new PhoneNumber("+61439436581"), //  From number, must be an SMS-enabled Twilio number ( This will send sms from ur "To" numbers ).
                    body: $"{message} Here is the verification code {AuthMessageSMSSenderOptions.SendNumber} with Twilio SMS API !!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Registration Failure : {ex.Message} ");
            }

            return Task.FromResult(0);
        }
    }

    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }

    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
