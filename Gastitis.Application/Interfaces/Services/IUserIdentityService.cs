using System;
using System.Collections.Generic;
using System.Text;

namespace Gastitis.Application.Interfaces.Services
{
	public interface IUserIdentityService
	{
		public Task<int> GetOrCreateUserIdAsync(string issuer, string subject, CancellationToken cancellationToken = default);
	}
}
