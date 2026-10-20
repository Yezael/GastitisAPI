using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public const string SchemeName = "Test";
		public const string UserSubject = "test-alice";
		public const string ScopeHeaderName = "X-Test-Scope";
		public const string OmitScopeHeaderName = "X-Test-Omit-Scope";
		public const string OmitSubjectHeaderName = "X-Test-Omit-Subject";

		public TestAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options, 
			ILoggerFactory 
			logger, 
			UrlEncoder encoder)
			: base(options, logger, encoder)
		{
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authorizationHeader = Request.Headers.Authorization.ToString();

			if (!string.Equals(authorizationHeader, SchemeName, StringComparison.OrdinalIgnoreCase))
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			var scope = "gastitis.access";
			if (Request.Headers.TryGetValue(ScopeHeaderName, out var scopeHeaderValue))
			{
				scope = scopeHeaderValue.ToString();
			}

			var shouldOmitScope = ReadBooleanHeader(OmitScopeHeaderName);
			var shouldOmitSubject = ReadBooleanHeader(OmitSubjectHeaderName);

			var claims = new List<Claim>();
			if(!shouldOmitSubject)
			{
				claims.Add(new Claim("sub", UserSubject));
			}

			if(!shouldOmitScope && !string.IsNullOrWhiteSpace(scope))
			{
				claims.Add(new Claim("scope", scope));
			}

			var identity = new ClaimsIdentity(claims, SchemeName);
			var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, SchemeName);

			return Task.FromResult(AuthenticateResult.Success(ticket));
		}

		private bool ReadBooleanHeader(string headerName)
		{
			if (!Request.Headers.TryGetValue(headerName, out var headerValue))
			{
				return false;
			}

			if (!bool.TryParse(headerValue.ToString(), out var value))
			{
				throw new InvalidOperationException(
					$"Test header '{headerName}' must contain 'true' or 'false'.");
			}

			return value;
		}
	}
}
