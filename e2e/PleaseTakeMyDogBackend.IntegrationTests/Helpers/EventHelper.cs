using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.DynamoDBEvents;

namespace ContentApi.IntegrationTests.Helpers
{
	public class EventHelper
	{
		public DynamoDBEvent GenerateDynamoDbEvent(IEnumerable<(string, StreamRecord)> records)
		{
			var dynamoDbEvent = new DynamoDBEvent()
			{
				Records = new List<DynamoDBEvent.DynamodbStreamRecord>()
			};
			foreach (var (type, record) in records)
			{
				var fullRecord = this.BoilerplateDynamoDbEvent(type);
				fullRecord.Dynamodb = record;
				dynamoDbEvent.Records.Add(fullRecord);
			}
			
			return dynamoDbEvent;
		}

		private DynamoDBEvent.DynamodbStreamRecord BoilerplateDynamoDbEvent(string type)
		{
			return new DynamoDBEvent.DynamodbStreamRecord()
			{
				EventID = "c4ca4238a0b923820dcc509a6f75849b",
				EventName = type,
				EventVersion = "1.1",
				EventSource = "aws:dynamodb",
				AwsRegion = "us-east-1",
				EventSourceArn = "arn:aws:dynamodb:us-east-1:123456789012:table/ExampleTableWithStream/stream/2015-06-27T00:48:05.899",
				Dynamodb = null
			};
		}
	}
}
