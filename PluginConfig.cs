using HomeSeerAPI;
using System;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace Hspi
{
    struct EnabledEventType
    {
        public EventType eventType;
        public bool enabled;

        public EnabledEventType(EventType eventType, bool enabled)
        {
            this.eventType = eventType;
            this.enabled = enabled;
        }
    }

    class PluginConfig: IDisposable
    {
        public event EventHandler<EventArgs> ConfigChanged;

        private const string EnabledKey = "Enabled";
        private const string ElasticsearchUrlKey = "ElasticsearchUrl";
        private const string SecurityTypeKey = "SecurityType";
        private const string UsernameKey = "Username";
        private const string PasswordKey = "Password";
        private const string DebugLoggingKey = "DebugLogging";
        private const string EnabledEventsKey = "EnabledEvents";
        private readonly static string FileName = $"{Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location)}.ini";
        private const string DefaultSection = "Settings";

        private readonly IHSApplication HS;
        private bool enabled;
        private bool debugLogging;
        private string elasticsearchUrl;
        private string securityType;
        private string username;
        private string password;
        private string enabledEvents;
        private bool disposedValue = false;
        private readonly bool offline;
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfig"/> class.
        /// </summary>
        /// <param name="HS">The homeseer application.</param>
        public PluginConfig(IHSApplication HS, bool offline = false)
        {
            this.HS = HS;

            this.offline = offline;

            this.PopulateConfig();
        }

        private void PopulateConfig()
        {
            if (!this.offline)
            {
                elasticsearchUrl = GetValue<string>(ElasticsearchUrlKey, string.Empty);
                debugLogging = GetValue<bool>(DebugLoggingKey, false);
                enabled = GetValue<bool>(EnabledKey, true);
                securityType = GetValue<string>(SecurityTypeKey, "disabled");
                username = GetValue<string>(UsernameKey, string.Empty);
                password = GetValue<string>(PasswordKey, string.Empty);
                enabledEvents = GetValue<string>(EnabledEventsKey, string.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the Account SID for Twilio
        /// </summary>
        /// <value>
        /// The Account SID.
        /// </value>
        public bool Enabled
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return enabled;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(EnabledKey, value, ref enabled);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Auth Token.
        /// </summary>
        /// <value>
        /// The auth token.
        /// </value>
        public string ElasticsearchUrl
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return elasticsearchUrl;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(ElasticsearchUrlKey, value, ref elasticsearchUrl);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether debug logging is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [debug logging]; otherwise, <c>false</c>.
        /// </value>
        public bool DebugLogging
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return debugLogging;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(DebugLoggingKey, value, ref debugLogging);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        public string SecurityType
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return securityType;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(SecurityTypeKey, value, ref securityType);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        public string Username
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return username;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(UsernameKey, value, ref username);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        public string Password
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return password;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(PasswordKey, value, ref password);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        public string EnabledEvents
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return enabledEvents ?? string.Empty;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(EnabledEventsKey, string.Join(",", value), ref enabledEvents);
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        public List<EnabledEventType> Events
        {
            get
            {
                IEnumerable<int> enabledEventIds = EnabledEvents.Split(',').Select((arg) =>
                {
                    bool successful = int.TryParse(arg, out int result);
                    return successful ? result : -1;

                }); ;

                return EventTypes.ALL_EVENT_TYPES.Select((eventType) =>
                {
                    bool isEnabled = enabledEventIds.Any((id) => id == eventType.EventTypeId);
                    return new EnabledEventType(eventType, isEnabled);
                }).ToList();
            }
        }

        private T GetValue<T>(string key, T defaultValue)
        {
            if(this.offline) return defaultValue;
            return GetValue(key, defaultValue, DefaultSection);
        }

        private T GetValue<T>(string key, T defaultValue, string section)
        {
            if(this.offline) return defaultValue;
            string stringValue = HS.GetINISetting(section, key, null, FileName);

            if(stringValue != null)
            {
                try
                {
                    T result = (T) System.Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
                    return result;
                }
                catch(Exception)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private void SetValue<T>(string key, T value, ref T oldValue)
        {
            SetValue<T>(key, value, ref oldValue, DefaultSection);
        }

        private void SetValue<T>(string key, T value, ref T oldValue, string section)
        {
            if(!value.Equals(oldValue))
            {
                string stringValue = System.Convert.ToString(value, CultureInfo.InvariantCulture);
                if(!this.offline)
                {
                    HS.SaveINISetting(section, key, stringValue, FileName);
                }
                oldValue = value;
            }
        }

        /// <summary>
        /// Fires event that configuration changed.
        /// </summary>
        public void FireConfigChanged()
        {
            if(ConfigChanged != null)
            {
                var ConfigChangedCopy = ConfigChanged;
                ConfigChangedCopy(this, EventArgs.Empty);
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if(!disposedValue)
            {
                if(disposing)
                {
                    configLock.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support
    };
}