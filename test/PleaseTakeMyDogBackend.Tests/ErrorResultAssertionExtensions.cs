using System.Net;
using FluentAssertions;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace PleaseTakeMyDogBackend.Tests
{
	static class ErrorResultAssertionExtensions
	{
		public static void BeErrorResultWithMessages(this ObjectAssertions x, HttpStatusCode statusCode,
			params string[] errors)
		{
			var jsonResult = x.BeOfType<JsonResult>().Subject;

			jsonResult.Value.Should().BeEquivalentTo(new
			{
				messages = errors
			});

			jsonResult.StatusCode.Should().Be((int)statusCode);
		}
	}
}
