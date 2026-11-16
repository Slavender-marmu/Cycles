using System.Net;
using System.Text.RegularExpressions;
using BicycleRacing.Database;
using BicycleRacing.Database.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BicycleRacing.Tests;

public class EnterAccountTests
{
    [Fact]
    public async Task PageContainsBothFormsPostingToTheCustomUrl()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        var response = await client.GetAsync("/enter-account");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, Regex.Matches(html, "action=\"/enter-account\"").Count);
        Assert.Contains("name=\"Signup.Email\"", html);
        Assert.Contains("name=\"Login.Email\"", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    public async Task LegacyPagesRedirectToEnterAccountAndPreserveReturnUrl(string page)
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        var response = await client.GetAsync($"/Identity/Account/{page}?returnUrl=%2FHome%2FPrivacy");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/enter-account?returnUrl=%2FHome%2FPrivacy", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ProtectedIdentityPageChallengesAtCustomUrl()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        var response = await client.GetAsync("/Identity/Account/Manage");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/enter-account", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task PostRequiresAntiforgeryToken()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        var response = await client.PostAsync("/enter-account", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["form"] = "login" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignupCreatesHashedAccountAndSendsConfirmationWithoutValidatingLoginForm()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        var response = await SubmitAsync(client, SignupFields());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/RegisterConfirmation", response.Headers.Location?.OriginalString);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await users.FindByEmailAsync("rider@example.com");
        Assert.NotNull(user);
        Assert.False(user.EmailConfirmed);
        Assert.NotEqual("Ride123!", user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, "Ride123!"));
        Assert.Equal("rider@example.com", app.Email.Recipient);
        Assert.Contains("/Identity/Account/ConfirmEmail", app.Email.Body);
    }

    [Fact]
    public async Task InvalidSignupOnlyShowsSignupErrorsAndDoesNotCreateUser()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        var fields = SignupFields();
        fields["Signup.ConfirmPassword"] = "different";
        var response = await SubmitAsync(client, fields);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("&#x27;Confirm password&#x27; and &#x27;Password&#x27; do not match.", html);
        Assert.DoesNotMatch("<span[^>]*field-validation-error[^>]*data-valmsg-for=\"Login\\.", html);
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task DuplicateSignupAndIdentityPasswordRulesAreDisplayed()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        await CreateUserAsync(app, confirmed: true);
        var duplicate = await SubmitAsync(client, SignupFields());
        Assert.Contains("is already taken", await duplicate.Content.ReadAsStringAsync());

        var fields = SignupFields();
        fields["Signup.Email"] = "another@example.com";
        fields["Signup.Password"] = fields["Signup.ConfirmPassword"] = "abcdef";
        var weakPassword = await SubmitAsync(client, fields);
        Assert.Contains("Passwords must have", await weakPassword.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/Home/Privacy", "/Home/Privacy")]
    [InlineData("https://example.com/elsewhere", "/")]
    [InlineData("//example.com/elsewhere", "/")]
    public async Task LoginSetsPersistentCookieAndOnlyRedirectsLocally(string returnUrl, string expectedLocation)
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        await CreateUserAsync(app, confirmed: true);
        var fields = LoginFields();
        fields["ReturnUrl"] = returnUrl;
        fields["Login.RememberMe"] = "true";
        var response = await SubmitAsync(client, fields);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith(".AspNetCore.Identity.Application=") && cookie.Contains("expires="));
        var manage = await client.GetAsync("/Identity/Account/Manage");
        Assert.Equal(HttpStatusCode.OK, manage.StatusCode);
    }

    [Fact]
    public async Task UnconfirmedAccountCannotLogIn()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        await CreateUserAsync(app, confirmed: false);
        var response = await SubmitAsync(client, LoginFields());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Unable to log in.", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("Set-Cookie", response.Headers.Select(header => header.Key));
    }

    [Fact]
    public async Task FailedPasswordsLockOutAccountAndDoNotValidateSignupForm()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        await CreateUserAsync(app, confirmed: true);
        var fields = LoginFields();
        fields["Login.Password"] = "wrong-password";
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await SubmitAsync(client, fields);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotMatch("<span[^>]*field-validation-error[^>]*data-valmsg-for=\"Signup\\.", html);
            if (attempt == 4)
            {
                Assert.Contains("This account is temporarily locked.", html);
            }
        }
    }

    [Fact]
    public async Task TwoFactorLoginContinuesThroughIdentity()
    {
        using var app = new AccountApplication();
        using var client = app.CreateAccountClient();
        await CreateUserAsync(app, confirmed: true);
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByEmailAsync("rider@example.com");
            Assert.True((await users.SetTwoFactorEnabledAsync(user!, true)).Succeeded);
        }

        var response = await SubmitAsync(client, LoginFields());
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/LoginWith2fa", response.Headers.Location?.OriginalString);
    }

    private static Dictionary<string, string> SignupFields() => new()
    {
        ["form"] = "signup",
        ["Signup.Email"] = "rider@example.com",
        ["Signup.Password"] = "Ride123!",
        ["Signup.ConfirmPassword"] = "Ride123!"
    };

    private static Dictionary<string, string> LoginFields() => new()
    {
        ["form"] = "login",
        ["Login.Email"] = "rider@example.com",
        ["Login.Password"] = "Ride123!"
    };

    private static async Task<HttpResponseMessage> SubmitAsync(HttpClient client, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync("/enter-account");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        return await client.PostAsync("/enter-account", new FormUrlEncodedContent(fields));
    }

    private static async Task<User> CreateUserAsync(AccountApplication app, bool confirmed)
    {
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User { UserName = "rider@example.com", Email = "rider@example.com", EmailConfirmed = confirmed };
        var result = await users.CreateAsync(user, "Ride123!");
        Assert.True(result.Succeeded);
        return user;
    }

    private sealed class AccountApplication : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public CapturingEmailSender Email { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Email);
            });
        }

        public HttpClient CreateAccountClient()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
            return client;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                connection.Dispose();
            }
        }
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public string Recipient { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Recipient = email;
            Body = htmlMessage;
            return Task.CompletedTask;
        }
    }
}
