

namespace Gastitis.Application.Interfaces.Services
{
	public interface ICurrentUserService
	{
		Task<int> GetUserIdAsync(CancellationToken cancellationToken = default);
	}
}
