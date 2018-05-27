using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Globalization;

namespace HSPI_Elasticsearch.Settings
{
	public class SettingsManager: IDisposable
	{
		private static string TABLE_EVENT_TYPES = string.Format("{0}_event_types", Constants.PLUGIN_STRING_NAME.ToLower(CultureInfo.InvariantCulture));

		private SQLiteConnection connection;

		private AppSettings settingsCache;

		private Logger logger;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public AppSettings Settings { get {
				if(this.settingsCache == null)
				{
					AppSettings settings = new AppSettings();

					SQLiteCommand eventTypesQuery;
					using(eventTypesQuery = new SQLiteCommand(string.Format("SELECT event_type_id, enabled FROM {0}", TABLE_EVENT_TYPES), this.connection))
					{
						SQLiteDataReader reader = eventTypesQuery.ExecuteReader();
						while(reader.Read())
						{
							EventTypeSetting ets = new EventTypeSetting();
							int eventTypeId = reader.GetInt32(0);
							ets.EventType = EventTypes.ALL_EVENT_TYPES.First((e) => e.EventTypeId == eventTypeId);
							ets.Enabled = reader.GetInt32(1) == 1;

							settings.EventTypeSettings.Add(ets);
						}

						this.settingsCache = settings;
					}
				}

				return this.settingsCache;
			}
		}

		public SettingsManager(Logger logger)
		{
			if(logger == null)
			{
				throw new ArgumentNullException("logger");
			}
			this.logger = logger;
			this.logger.LogInfo("Initializing Settings");
			if(!File.Exists(Constants.PLUGIN_DB_FILE_NAME))
			{
				logger.LogInfo("Creating database file");
				SQLiteConnection.CreateFile(Constants.PLUGIN_DB_FILE_NAME);
			}
			connection = new SQLiteConnection(string.Format("Data Source={0}; Version=3;", Constants.PLUGIN_DB_FILE_NAME));
			connection.Open();
			this.logger.LogInfo("Database opened");
			this.SetupDatabaseIfRequired();
			this.logger.LogInfo("Database setup and ready for use!");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public void UpdateSettings(AppSettings settings)
		{
			if(settings == null)
			{
				return;
			}

			settings.EventTypeSettings
				.Select(s => new SQLiteCommand(string.Format(
						"update {0} set enabled = {1} where event_type_id = {2}",
						TABLE_EVENT_TYPES,
						s.Enabled ? 1 : 0,
						s.EventType.EventTypeId
					),
					this.connection
				))
				.Select(c => c.ExecuteNonQuery())
				.ToArray();

			this.settingsCache = null;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		private void SetupDatabaseIfRequired()
		{
			if(!TableExists(TABLE_EVENT_TYPES))
			{
				SQLiteCommand createTableCommand = new SQLiteCommand(string.Format(
					"CREATE TABLE {0} (event_type_id INTEGER, enabled INTEGER)"
				, TABLE_EVENT_TYPES), this.connection);

				using(createTableCommand)
				{
					createTableCommand.ExecuteNonQuery();

					SQLiteCommand[] commands = EventTypes.ALL_EVENT_TYPES
						.Select((e) => string.Format("INSERT INTO {0} VALUES ('{1}', 1)", TABLE_EVENT_TYPES, e.EventTypeId))
						.Select((e) => new SQLiteCommand(e, this.connection))
						.ToArray();

					foreach(SQLiteCommand command in commands)
					{
						using(command)
						{
							command.ExecuteNonQuery();
						}
					}
				}
			}
		}

		#region SQLite Utils
		private bool TableExists(string tableName)
		{
			SQLiteCommand command = new SQLiteCommand(
				string.Format("SELECT name from sqlite_master WHERE type='table' AND name=$name"),
				this.connection
			);

			using(command)
			{
				command.Parameters.AddWithValue("$name", tableName);

				bool tableExists = false;
				SQLiteDataReader reader = command.ExecuteReader();
				while(reader.Read())
				{
					tableExists = true;
				}
				return tableExists;
			}
		}
		#endregion

		#region IDisposable
		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(this.connection != null)
				{
					this.connection.Dispose();
					this.connection = null;
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
