using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeSeerAPI;

namespace Hspi
{
	public static class EventTypes
	{
		public static EventType LOG = new EventType((int) Enums.HSEvent.LOG, "Logging Events", "A message written to the event log");
		public static EventType CONFIG_CHANGE = new EventType((int) Enums.HSEvent.CONFIG_CHANGE, "Configuration Changes", "Device or event has changed");
		public static EventType SETUP_CHANGE = new EventType((int) Enums.HSEvent.SETUP_CHANGE, "Setup Changes", "System setup has changed");
		public static EventType STRING_CHANGE = new EventType((int) Enums.HSEvent.STRING_CHANGE, "Device String Changes", "Device's string value has changed");
		public static EventType VALUE_CHANGE = new EventType((int) Enums.HSEvent.VALUE_CHANGE, "Device Value Changes", "Device's value has changed");
		public static EventType GENERIC = new EventType((int) Enums.HSEvent.GENERIC, "Generic HomeSeer Events", "Generic event raised by other plug-ins and scripts");

		public static EventType[] ALL_EVENT_TYPES = new EventType[] {
			LOG,
			CONFIG_CHANGE,
			SETUP_CHANGE,
			STRING_CHANGE,
			VALUE_CHANGE,
			GENERIC
		};
	}

	public class EventType
	{
		public int EventTypeId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public EventType(int id, string name, string desc)
		{
			this.EventTypeId = id;
			this.Name = name;
			this.Description = desc;
		}
	}
}
