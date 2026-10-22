using Gastitis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Gastitis.API.IntegrationTests.Infrastructure
{
	//Controller useful to test directly using http the user providing from CurrentUserService
	[ApiController]
	[Authorize]
	[Route("test/current-user")]
	public sealed class CurrentUserProbeController : ControllerBase
	{
		private readonly ICurrentUserService _currentUserService;

		public CurrentUserProbeController(ICurrentUserService currentUserService)
		{
			_currentUserService = currentUserService;
		}

		[HttpGet]
		public async Task<IActionResult> GetCurrentUserIdAsync(CancellationToken cancellationToken)
		{
			var userId = await _currentUserService.GetUserIdAsync(cancellationToken);
			return Ok(userId);
		}
	}
}
