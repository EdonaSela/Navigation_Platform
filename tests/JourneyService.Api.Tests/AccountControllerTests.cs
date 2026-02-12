using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

public class AccountControllerTests
{
    [Fact]
    public void Login_Returns_ChallengeResult_With_OpenIdConnect()
    {
        var config = new Mock<IConfiguration>();
        var controller = new AccountController(config.Object);

        var result = controller.Login("/home");

        result.Should().BeOfType<ChallengeResult>();
        var challenge = (ChallengeResult)result;
        challenge.AuthenticationSchemes.Should().Contain(OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Fact]
    public void SignedOut_Redirects_To_Configured_Url()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Authentication:Microsoft:PostLogoutRedirectUri"]).Returns("http://example.com/login");

        var controller = new AccountController(config.Object);

        var result = controller.SignedOut();

        result.Should().BeOfType<RedirectResult>();
        var redirect = (RedirectResult)result;
        redirect.Url.Should().Be("http://example.com/login");
    }

    [Fact]
    public void GetCurrentUserId_Returns_User_Data()
    {
        var config = new Mock<IConfiguration>();
        var controller = new AccountController(config.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim("roles", "Admin")
        }, "test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var result = controller.GetCurrentUserId();
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
        
        var valueType = okResult.Value!.GetType();
        var idProp = valueType.GetProperty("Id");
        var emailProp = valueType.GetProperty("Email");
        
        idProp!.GetValue(okResult.Value).Should().Be("user-123");
        emailProp!.GetValue(okResult.Value).Should().Be("test@example.com");
    }

    [Fact]
    public async Task Logout_Signs_Out_Cookie_And_OpenIdConnect()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Authentication:Microsoft:SignedOutCallbackPath"]).Returns("/signed-out");

        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();

        var controller = new AccountController(config.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };

        await controller.Logout();

        authentication.Verify(a => a.SignOutAsync(
            It.IsAny<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once);

        authentication.Verify(a => a.SignOutAsync(
            It.IsAny<HttpContext>(),
            OpenIdConnectDefaults.AuthenticationScheme,
            It.Is<AuthenticationProperties?>(p => p != null && p.RedirectUri == "/signed-out")), Times.Once);
    }
}
