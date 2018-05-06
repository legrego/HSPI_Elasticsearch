using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace HSPI_Elasticsearch.Documents
{


	[ElasticsearchType(Name="event")]
	abstract class BaseDocument
	{
		public static string DATE_TIME_FORMAT = "yyyyMMddTHHmmss.fffffffZ" +
			"";

		public string Id { get; set; }

		[Date(Format = "basic_date_time")]
		public string Time { get; set; }

		[Keyword]
		public string EventType { get; set; }

		[Keyword]
		public string Event { get; set; }

		[Keyword]
		public string Sender { get; set; }

		[Text]
		public string EventParameters { get; set; }

		[Keyword]
		public string EntityType { get; set; }

		[Keyword]
		public string ChangeType { get; set; }

		[Number]
		public int DeviceRef { get; set; }

		[Keyword]
		public string TypeId { get; set; }

		[Keyword]
		public string MessageClass { get; set; }

		[Text]
		public string Message { get; set; }

		[Keyword]
		public string Color { get; set; }

		[Keyword]
		public string Source { get; set; }

		[Number]
		public int ErrorCode { get; set; }

		[Keyword]
		public string Address { get; set; }

		[Number]
		public double Value { get; set; }

		[Number]
		public double OldValue { get; set; }

		[Text]
		public string DeviceString { get; set; }

		public BaseDocument(string eventType)
		{
			this.EventType = eventType;
			this.Time = DateTime.UtcNow.ToString(DATE_TIME_FORMAT);
		}

		public override string ToString()
		{
			return string.Format("Id={0}; Time={1}; EventType={2}; Message={3}; Address={4}; DeviceRef={5} Value={6}",
				this.Id, this.Time, this.EventType, this.Message, this.Address, this.DeviceRef, this.Value
				);
		}
	}

	class GenericEvent : BaseDocument
	{
		public GenericEvent(object[] eventParams) : base("GENERIC")
		{
			this.Event = eventParams[1] as string;
			this.Sender = eventParams[2] as string;
			if(eventParams.Length > 3)
			{
				this.EventParameters = String.Join(" | ", eventParams.Skip(3));				
			}			
		}
	}

	class SetupChangeEvent : BaseDocument
	{
		public SetupChangeEvent(object[] eventParams) : base("SETUP_CHANGE")
		{
		}
	}

	class ConfigChangeEvent : BaseDocument
	{
		public ConfigChangeEvent(object[] eventParams) : base("CONFIG_CHANGE")
		{
			int entityType = (int) eventParams[1];
			switch(entityType)
			{
				case 0:
					this.EntityType = "DEVICE";
					break;
				case 1:
					this.EntityType = "EVENT";
					break;
				case 2:
					this.EntityType = "EVENT_GROUP";
					break;
				default:
					this.EntityType = "UNKNOWN";
					break;
			}

			this.DeviceRef = (int) eventParams[3];

			int changeType = (int) eventParams[4];
			switch(changeType)
			{
				case 0:
					this.ChangeType = "UNKNOWN";
					break;
				case 1:
					this.ChangeType = "ADDED";
					break;
				case 2:
					this.ChangeType = "DELETED";
					break;
				case 3:
					this.ChangeType = "CHANGED";
					break;
				default:
					this.ChangeType = "UNKNOWN";
					break;
			}
		}
	}

	class LogEvent : BaseDocument
	{
		public LogEvent(object[] eventParams) : base("LOG")
		{
			this.TypeId = eventParams[0] as string;
			// parameter 1 is the Log Time, which we assume to be close enough to when the Log event was triggered.
			this.MessageClass = eventParams[2] as string;
			this.Message = eventParams[3] as string;
			this.Color = eventParams[4] as string;
			// parameter 5 is the Priority, which I've chosen to ignore for now
			this.Source = eventParams[6] as string;
			this.ErrorCode = (int) eventParams[7];
		}
	}

	class StringChangeEvent : BaseDocument
	{
		public StringChangeEvent(object[] eventParams) : base("STRING_CHANGE")
		{
			this.Address = eventParams[1] as string;
			this.DeviceString = eventParams[2] as string;
			this.DeviceRef = (int) eventParams[3];
		}
	}

	class ValueChangeEvent : BaseDocument
	{
		public ValueChangeEvent(object[] eventParams) : base("VALUE_CHANGE")
		{
			this.Address = eventParams[1] as string;
			this.Value = (double) eventParams[2];
			this.OldValue = (double) eventParams[3];
			this.DeviceRef = (int) eventParams[4];
		}
	}

}
