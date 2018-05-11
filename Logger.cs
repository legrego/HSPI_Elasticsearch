using HomeSeerAPI;
using System;
using System.Globalization;

namespace HSPI_Elasticsearch
{
	public class Logger
	{
		private string Name { get; set; }
		public IHSApplication HS { get; set; }
		public bool EnableDebug { get; set; }

		public Logger(string name, IHSApplication HS = null, bool enableDebug = true)
		{
			this.Name = name;
			this.HS = HS;
			this.EnableDebug = enableDebug;
		}

		public void LogDebug(string message)
		{
			if(this.EnableDebug)
			{
				string entry = String.Format(CultureInfo.InvariantCulture, "Debug:{0}", message);
				if(HS == null)
				{
					Console.WriteLine(entry);
				}
				else
				{
					HS.WriteLog(this.Name, entry);
				}
			}
		}

		public void LogError(string message)
		{
			string entry = String.Format(CultureInfo.InvariantCulture, "Error:{0}", message);

			Console.Error.WriteLine(entry);
			if(HS != null)
			{
				HS.WriteLogEx(this.Name, entry, "#FF0000");
			}
		}

		public void LogInfo(string message)
		{
			Console.WriteLine(message);
			if(HS != null)
			{
				HS.WriteLog(this.Name, message);
			}
		}

		public void LogWarning(string message)
		{
			string entry = String.Format(CultureInfo.InvariantCulture, "Warning:{0}", message);
			Console.Error.WriteLine(entry);
			if(HS != null)
			{
				HS.WriteLogEx(this.Name, entry, "#D58000");
			}
		}
	}
}
