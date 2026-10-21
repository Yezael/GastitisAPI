using Gastitis.Application.Interfaces.Services;

namespace Gastitis.API.Services
{
	public sealed class CurrentUserService : ICurrentUserService
	{
		private readonly IHttpContextAccessor _contextAccessor;
		private readonly IUserIdentityService _userIdentityService;

		public CurrentUserService(
			IHttpContextAccessor contextAccessor, IUserIdentityService userIdentityService)
		{
			_contextAccessor = contextAccessor;
			_userIdentityService = userIdentityService;
		}

		public async Task<int> GetUserIdAsync(CancellationToken cancellationToken = default)
		{
			var currContext = _contextAccessor.HttpContext;
			if(currContext == null)
			{
				throw new InvalidOperationException("The current user cannot be resolved outside an HTTP request");
			}

			var isAuthenticated = currContext.User.Identity?.IsAuthenticated;
			if (!isAuthenticated.HasValue || !isAuthenticated.Value)
			{
				throw new InvalidOperationException("Current User is not authenticated");
			}

			var issuer = currContext.User.Claims.SingleOrDefault(c => c.Type == "iss")?.Value;
			if (string.IsNullOrWhiteSpace(issuer))
			{
				throw new InvalidOperationException("Issuer could not be resolved for the active user");
			}

			var subject = currContext.User.Claims.SingleOrDefault(c => c.Type == "sub")?.Value;
			if (string.IsNullOrWhiteSpace(subject))
			{
				throw new InvalidOperationException("subject could not be resolved for the active user");
			}

			var response = await _userIdentityService.GetOrCreateUserIdAsync(issuer, subject, cancellationToken);
			return response;
		}
	}
}
