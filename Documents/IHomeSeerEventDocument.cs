
namespace Hspi.Documents
{
	interface IHomeSeerEventDocument
	{
		string Id { get; set; }
		
		string Time { get; set; }

		string EventType { get; set; }

		string Event { get; set; }

		string Sender { get; set; }

		string EventParameters { get; set; }

		string EntityType { get; set; }

		string ChangeType { get; set; }

		int DeviceRef { get; set; }

		string TypeId { get; set; }

		string MessageClass { get; set; }

		string Message { get; set; }

		string Color { get; set; }

		string Source { get; set; }

		int ErrorCode { get; set; }

		string Address { get; set; }

		double Value { get; set; }

		double OldValue { get; set; }

		string DeviceString { get; set; }
	}
}
