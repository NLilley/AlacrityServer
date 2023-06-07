using AlacrityCore.Models.ReqRes.Authenatication;
using AlacrityCore.Services.Front;
using AlacrityCore.Utils;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers.Trade
{
    [ApiController]
    [Route("authentication")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IAuthenticationFrontService _authenticationService;
        public AuthenticationController(
            ILogger<AuthenticationController> logger,
            IAuthenticationFrontService authenticationService
        )
            => (_logger, _authenticationService) = (logger, authenticationService);

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var (error, clientId) = await _authenticationService.Login(request.UserName, request.Password);
            if (error != null || clientId == null)
            {
                _logger.LogInformation("Client failed to login with username: {username}, error: {error}", request.UserName, error);
                return StatusCode(403, "Username or password were incorrect");
            }

            var session = Request.HttpContext.Session;
            session.SetInt32(SessionUtil.IsAuthenticatedKey, 1);
            session.SetInt32(SessionUtil.ClientIdString, clientId.Value);

            return StatusCode(200);
        }

        [AllowAnonymous]
        [HttpGet("IsLoggedIn")]
        public async Task<bool> IsLoggedIn()
        {
            try
            {
                var clientId = this.GetClientId();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost("Logout")]
        public async Task Logout()
        {
            var session = Request.HttpContext.Session;
            session.Clear();
            session.SetInt32(SessionUtil.IsAuthenticatedKey, 0);
            await session.CommitAsync();

            // https://github.com/dotnet/aspnetcore/issues/5333
            // To CleanUp SignalR connections, we expect the client to reload after a logout.
            // A better approach would be to manually close the SignalR connection/unsubscribe
            // to mesaage groups from the server, but implementing this funcitonality is a bit ugly.
        }

        [HttpPut("ChangePassword")]
        public async Task<ChangePasswordResponse> ResetPassword([FromBody] ChangePasswordRequest request)
        {
            var clientId = this.GetClientId();

            if (!AuthenticationUtil.IsPasswordComplex(request.NewPassword))
                return new() { Succeeded = false, ErrorMessage = "New Password is not sufficiently complex" };

            if (request.ExistingPassword == null || request.NewPassword == null
                || await _authenticationService.IsPasswordCorrect(clientId, request.ExistingPassword))
                return new() { Succeeded = false, ErrorMessage = "Password Information Invalid" };

            var succeeded = await _authenticationService.ChangePassword(clientId, request.NewPassword);

            // Require clients to ReAuthenticate with the new password.
            if (succeeded)
                await Logout();

            return new() { Succeeded = succeeded, ErrorMessage = "Unable to change password at this time" };
        }
    }
}
