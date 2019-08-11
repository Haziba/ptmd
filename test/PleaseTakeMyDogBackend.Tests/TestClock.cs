using System;
using Microsoft.Extensions.Internal;

namespace PleaseTakeMyDogBackend.Tests
{
	internal sealed class TestClock : ISystemClock
	{
		public TestClock() : this(DateTimeOffset.UtcNow)
		{
		}

		public TestClock(DateTimeOffset testTime) => this.UtcNow = testTime;

		public DateTimeOffset UtcNow { get; }
	}
}