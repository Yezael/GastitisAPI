namespace Gastitis.Application.Exceptions
{
	public class ApplicationValidationException : Exception
    {
        public readonly IReadOnlyDictionary<string, string[]> Errors;
		public ApplicationValidationException(string field, string message) : base(message) 
        { 
            Errors = new Dictionary<string, string[]>
			{
				{ field, new string[] { message } }
			};
		}

		public ApplicationValidationException(Dictionary<string, string[]> errors) : base("One or more validation errors occurred.") 
        { 
            Errors = errors;
		}

    }
}