using System;

namespace PleaseTakeMyDogBackend.Tests
{
	public static class GuidExtensions
	{
		public static string ToUriSafeBase64(this Guid id) =>
			Convert.ToBase64String(id.ToByteArray())
				.Replace('+', '-')
				.Replace('/', '_');
	}
}
