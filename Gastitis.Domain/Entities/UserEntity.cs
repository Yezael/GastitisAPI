

namespace Gastitis.Domain.Entities
{
	public class UserEntity
	{
		public int Id { get; private set; }
		public string Issuer { get; private set; }
		public string Subject { get; private set; }

		public UserEntity(string issuer, string subject)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
			ArgumentException.ThrowIfNullOrWhiteSpace(subject);

			Issuer = issuer;
			Subject = subject;
		}
	}
}
