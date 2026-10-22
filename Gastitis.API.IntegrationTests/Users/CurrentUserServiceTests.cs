using Gastitis.API.Services;
using Gastitis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Gastitis.API.IntegrationTests.Users
{
	public sealed class CurrentUserServiceTests
	{
		private string _issuer = "http://test-issuer";
		private string _subject = "current-user-test";
		private int _expectedUserId = 1;

		[Fact]
		public async Task GetUserIdAsync_WithoutSubjectClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(omitSub: true);
		}

		[Fact]
		public async Task GetUserIdAsync_WithoutIssuerClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(omitIssuer: true);
		}

		[Fact]
		public async Task GetUserIdAsync_WithDuplicatedSubjectClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(duplicateSub: true);
		}

		[Fact]
		public async Task GetUserIdAsync_WithDuplicatedIssuerClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(duplicateIssuer: true);
		}

		[Fact]
		public async Task GetUserIdAsync_WithEmptyIssuerClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(overriddenIssuer: "");
		}

		[Fact]
		public async Task GetUserIdAsync_WithWhiteSpaceIssuerClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(overriddenIssuer: " ");
		}

		[Fact]
		public async Task GetUserIdAsync_WithEmptySubjectClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(overriddenSub: "");
		}

		[Fact]
		public async Task GetUserIdAsync_WithWhiteSpaceSubjectClaim_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(overriddenSub: " ");
		}

		[Fact]
		public async Task GetUserIdAsync_WithoutAuthenticatedIdentity_ThrowsInvalidOperationException()
		{
			await BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(omitAuthentication: true);
		}

		[Fact]
		public async Task GetUserIdAsync_WithoutHttpContext_ThrowsInvalidOperationException()
		{
			var emptyContextAccessor = new HttpContextAccessor()
			{
				HttpContext = null
			};

			var recording = new RecordingUserIdentityService();
			var service = new CurrentUserService(emptyContextAccessor, recording);

			using var tokenSource = new CancellationTokenSource();
			await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserIdAsync(tokenSource.Token));

			Assert.Equal(0, recording.InvocationsCount);
		}

		[Fact]
		public async Task GetUserIdAsync_WithValidIdentity_DelegatesAndReturnsUserId()
		{
			var contextAccessor = BuildContextAccessor();

			var recording = new RecordingUserIdentityService();
			var userIdentityService = new CurrentUserService(contextAccessor, recording);

			using var cancellationSource = new CancellationTokenSource();
			var result = await userIdentityService.GetUserIdAsync(cancellationSource.Token);

			Assert.Equal(_expectedUserId, result);
			Assert.Equal(_issuer, recording.Issuer);
			Assert.Equal(_subject, recording.Subject);
			Assert.Equal(cancellationSource.Token, recording.CancellationToken);
			Assert.Equal(1, recording.InvocationsCount);
		}

		private async Task BuildContextAndCallGetUserIdAsync_ExpectThrowsInvalidOperation(
			bool omitAuthentication = false,
			bool omitIssuer = false,
			bool omitSub = false,
			string? overriddenIssuer = null,
			string? overriddenSub = null,
			bool duplicateIssuer = false,
			bool duplicateSub = false
			)
		{
			var accessor = BuildContextAccessor(
				omitAuthentication, 
				omitIssuer, 
				omitSub, 
				overriddenIssuer, 
				overriddenSub, 
				duplicateIssuer, 
				duplicateSub);

			var recording = new RecordingUserIdentityService();
			var service = new CurrentUserService(accessor, recording);

			using var tokenSource = new CancellationTokenSource();
			await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserIdAsync(tokenSource.Token));

			Assert.Equal(0, recording.InvocationsCount);
		}

		private HttpContextAccessor BuildContextAccessor(
			bool omitAuthentication = false,
			bool omitIssuer = false,
			bool omitSub = false,
			string? overriddenIssuer = null,
			string? overriddenSub = null,
			bool duplicateIssuer = false,
			bool duplicateSub = false
			)
		{
			var authenticationType = omitAuthentication? "" : "Test";

			List<Claim> claims = new List<Claim>();
			if (!omitIssuer)
			{
				claims.Add(new Claim("iss", overriddenIssuer?? _issuer));
				if (duplicateIssuer)
				{
					claims.Add(new Claim("iss", overriddenIssuer ?? _issuer));
				}
			}
			if (!omitSub)
			{
				claims.Add(new Claim("sub", overriddenSub?? _subject));
				if (duplicateSub)
				{
					claims.Add(new Claim("sub", overriddenSub ?? _subject));
				}
			}

			var identity = new ClaimsIdentity(claims, authenticationType);

			var claimsPrincipal = new ClaimsPrincipal(identity);

			var httpContex = new DefaultHttpContext()
			{
				User = claimsPrincipal
			};
			var contextAccessor = new HttpContextAccessor()
			{
				HttpContext = httpContex
			};

			return contextAccessor;
		}


		//Mock service to capture that CurrentUserService sends to the identity service
		private class RecordingUserIdentityService : IUserIdentityService
		{
			public string? Issuer { get; private set; }
			public string? Subject { get; private set; }
			public CancellationToken CancellationToken { get; private set; }
			public int InvocationsCount { get; private set; }

			public Task<int> GetOrCreateUserIdAsync(string issuer, string subject, CancellationToken cancellationToken = default)
			{
				Issuer = issuer;
				Subject = subject;
				CancellationToken = cancellationToken;
				InvocationsCount++;
				return Task.FromResult(1);
			}
		}
	}
}
