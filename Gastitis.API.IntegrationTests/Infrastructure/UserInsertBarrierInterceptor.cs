using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Gastitis.API.IntegrationTests.Infrastructure
{
	public sealed class UserInsertBarrierInterceptor : DbCommandInterceptor
	{
		private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int ArrivalCount => Volatile.Read(ref _arrivalCount);

		private int _arrivalCount;

		public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
			DbCommand command,
			CommandEventData eventData,
			InterceptionResult<int> result,
			CancellationToken cancellationToken
			)
		{
			if (!command.CommandText.TrimStart().StartsWith("INSERT INTO \"Users\"", StringComparison.Ordinal))
			{
				return result;
			}

			if (Interlocked.Increment(ref _arrivalCount) == 2)
			{
				_bothArrived.TrySetResult();
			}

			await _bothArrived.Task.WaitAsync(
				TimeSpan.FromSeconds(30),
				cancellationToken);

			return result;
		}
	}
}
