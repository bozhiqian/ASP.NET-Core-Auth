using IdentityServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Extensions
{
    public static class IdentityBuilderExtensions
    {
        public static IIdentityServerBuilder AddUserStore(this IIdentityServerBuilder builder)
        {
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.AddProfileService<UserProfileService>();
            return builder;
        }
    }
}
