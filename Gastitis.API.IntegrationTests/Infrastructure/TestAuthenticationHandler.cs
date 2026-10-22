using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public const string TestIssuer = "http://test-issuer";
		public const string SchemeName = "Test";
		public const string UserSubject = "test-alice";

		public const string ScopeHeaderName = "X-Test-Scope";
		public const string IssuerHeaderName = "X-Test-Issuer";
		public const string SubjectHeaderName = "X-Test-Subject";
		public const string DuplicateSubHeaderName = "X-Duplicate-Subject";
		public const string DuplicateIssuerHeaderName = "X-Duplicate-Issuer";
		public const string OmitScopeHeaderName = "X-Test-Omit-Scope";
		public const string OmitSubjectHeaderName = "X-Test-Omit-Subject";
		public const string OmitIssuerHeaderName = "X-Test-Omit-Issuer";
		public const string UseEmptySubjectHeaderName = "X-Test-Empty-Subject";
		public const string UseEmptyIssuerHeaderName = "X-Test-Empty-Issuer";

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

			var issuer = TestIssuer;
			if (Request.Headers.TryGetValue(IssuerHeaderName, out var issuerHeaderValue))
			{
				issuer = issuerHeaderValue.ToString();
			}
			if (ReadBooleanHeader(UseEmptyIssuerHeaderName))
			{
				issuer = string.Empty;
			}

			var subject = UserSubject;
			if (Request.Headers.TryGetValue(SubjectHeaderName, out var headerSubjectValue))
			{
				subject = headerSubjectValue.ToString();
			}
			if (ReadBooleanHeader(UseEmptySubjectHeaderName))
			{
				subject = string.Empty;
			}

			var shouldOmitScope = ReadBooleanHeader(OmitScopeHeaderName);
			var shouldOmitSubject = ReadBooleanHeader(OmitSubjectHeaderName);
			var shouldOmitIssuer = ReadBooleanHeader(OmitIssuerHeaderName);

			var claims = new List<Claim>();

			if (!shouldOmitScope && !string.IsNullOrWhiteSpace(scope))
			{
				claims.Add(new Claim("scope", scope));
			}

			if (!shouldOmitIssuer)
			{
				claims.Add(new Claim("iss", issuer));

				if (ReadBooleanHeader(DuplicateIssuerHeaderName))
				{
					claims.Add(new Claim("iss", issuer));
				}
			}

			if (!shouldOmitSubject)
			{
				claims.Add(new Claim("sub", subject));
				if (ReadBooleanHeader(DuplicateSubHeaderName))
				{
					claims.Add(new Claim("sub", subject));
				}
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
