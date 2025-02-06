using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace GZCTF.Extensions.Startup;

static class IdentityExtension
{
    public static void ConfigureIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

        builder.Services.AddAuthentication(o =>
        {
            o.DefaultScheme = IdentityConstants.ApplicationScheme;
            o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddIdentityCookies(options =>
        {
            options.ApplicationCookie?.Configure(auth =>
            {
                auth.Cookie.Name = "GZCTF_Token";
                auth.SlidingExpiration = true;
                auth.ExpireTimeSpan = TimeSpan.FromDays(7);
            });
        });

        builder.Services.AddIdentityCore<UserInfo>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedEmail = true;

                // Allow all characters in username
                options.User.AllowedUserNameCharacters = string.Empty;
            })
            .AddSignInManager<SignInManager<UserInfo>>()
            .AddUserManager<UserManager<UserInfo>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<TranslatedIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
            o.TokenLifespan = TimeSpan.FromHours(3)
        );
    }
}
