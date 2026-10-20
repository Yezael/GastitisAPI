

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class TestTokenIssuer : IDisposable
	{
		public const string Issuer = "https://identity.test.issuer";
		public const string Audience = "gastitis-api";

		private readonly RSA _rsa = RSA.Create(2048);
		private readonly SigningCredentials _signingCredentials;

		public RsaSecurityKey PublicKey { get; }

		public TestTokenIssuer()
		{
			var keyId = Guid.NewGuid().ToString("N");

			var signingKey = new RsaSecurityKey(_rsa)
			{
				KeyId = keyId
			};
			_signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
			PublicKey = new RsaSecurityKey(_rsa.ExportParameters(false))
			{
				KeyId = keyId
			};
		}

		public string CreateAccessToken(
			string audience = Audience, 
			string issuer = Issuer, 
			DateTime? issuedAt = null,
			DateTime? notBefore = null,
			string scope = "gastitis.access")
		{
			var issuedAtUTC = issuedAt ?? DateTime.UtcNow;
			var selectedNotBefore = notBefore ?? issuedAtUTC;

			var descriptor = new SecurityTokenDescriptor
			{
				Issuer = issuer,
				Audience = audience,
				IssuedAt = issuedAtUTC,
				NotBefore = selectedNotBefore,
				Expires = selectedNotBefore.AddMinutes(5),
				Claims = new Dictionary<string, object>
				{
					["sub"] = "test-alice",
					["scope"] = scope
				},
				SigningCredentials = _signingCredentials
			};

			return new JsonWebTokenHandler().CreateToken(descriptor);
		}

		public void Dispose()
		{
			_rsa.Dispose();
		}

	}
}
