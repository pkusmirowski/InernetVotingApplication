using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.Services.Mail;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using System.Threading.RateLimiting;

namespace InternetVotingApplication
{
    /// <summary>
    /// Registers services and builds the HTTP pipeline. Kept as a separate class so that the
    /// composition of the application is readable in one place and testable in isolation.
    /// </summary>
    public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        public IConfiguration Configuration { get; } = configuration;

        public IWebHostEnvironment Environment { get; } = environment;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<SmtpOptions>()
                .Bind(Configuration.GetSection(SmtpOptions.SectionName))
                .ValidateDataAnnotations();
            services.AddOptions<MailOptions>().Bind(Configuration.GetSection(MailOptions.SectionName));
            services.AddOptions<SeedingOptions>().Bind(Configuration.GetSection(SeedingOptions.SectionName));
            services.AddOptions<SecurityOptions>().Bind(Configuration.GetSection(SecurityOptions.SectionName));
            services.AddOptions<SigningOptions>().Bind(Configuration.GetSection(SigningOptions.SectionName));
            services.AddOptions<ChainOptions>().Bind(Configuration.GetSection(ChainOptions.SectionName));

            var connectionString = Configuration.GetConnectionString("InternetVotingDBConnection");
            services.AddDbContext<InternetVotingContext>(options => options.UseSqlServer(connectionString));

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IBlockSigner>(sp => SigningKeyProvider.Create(
                sp.GetRequiredService<IOptions<SigningOptions>>(),
                sp.GetRequiredService<IHostEnvironment>(),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SigningKeyProvider))));

            services.AddScoped<IAuditLog, AuditLog>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IElectionService, ElectionService>();
            services.AddScoped<IResultsService, ResultsService>();
            services.AddScoped<IChainService, ChainService>();

            services.AddScoped<EmailQueue>();
            services.AddScoped<IEmailSender, QueuedEmailSender>();
            services.AddSingleton<ISmtpTransport, SmtpEmailSender>();
            services.AddHostedService<EmailDispatcher>();
            services.AddHostedService<ChainVerificationWorker>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = "InternetVoting.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin));

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Configuration.GetValue("RateLimiting:AuthPermitLimit", 20),
                            Window = Configuration.GetValue("RateLimiting:AuthWindow", TimeSpan.FromMinutes(1)),
                            QueueLimit = 0,
                        }));
            });

            services.AddHealthChecks()
                .AddDbContextCheck<InternetVotingContext>("database");

            services.AddControllersWithViews(options =>
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        }

        public void Configure(WebApplication app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSerilogRequestLogging();
            app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");
            app.UseHttpsRedirection();
            app.UseRequestLocalization("pl-PL");
            app.UseSecurityHeaders();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
