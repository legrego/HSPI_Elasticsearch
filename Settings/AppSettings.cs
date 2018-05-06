using System.Collections.Generic;

namespace HSPI_Elasticsearch.Settings
{
	public class AppSettings
	{
		public List<EventTypeSetting> EventTypeSettings { get; set; }

		public AppSettings()
		{
			this.EventTypeSettings = new List<EventTypeSetting>();
		}

		public bool IsEventTypeEnabled(int eventTypeId)
		{
			return this.EventTypeSettings.Exists(s => s.EventType.EventTypeId == eventTypeId && s.Enabled);
		}
	}

	public class EventTypeSetting
	{
		public EventType EventType { get; set; }
		public bool Enabled { get; set; }
	}
}
