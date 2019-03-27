using System;
using System.Globalization;

using System.Threading;
using HomeSeerAPI;
using Hspi.Documents;
using Hspi.Pages;
namespace Hspi
{
    internal class ElasticsearchPlugin : HspiBase
    {
        private ElasticsearchManager esManager;

		public bool Running { get; set; } = true;
        public PluginConfig Config { get; private set; }

        public ElasticsearchPlugin() : base(Constants.PLUGIN_STRING_NAME) { 
        
        }


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (Config != null)
            {
                Config.ConfigChanged -= PluginConfig_ConfigChanged;
                Config.Dispose();
            }

            base.Dispose(disposing);
        }
       

        public override string GetPagePlugin(string page, string user, int userRights, string queryString)
        {
            if (page == ConfigPage.Name)
            {
                using(var configPage = new ConfigPage(HS, this))
                {
                    return configPage.GetWebPage();
                }
            }

            return string.Empty;
        }

        public override void HSEvent(Enums.HSEvent eventType, object[] parameters)
        {
            bool canContinue = true;  // this.SettingsManager.Settings.IsEventTypeEnabled((int)eventType);
			if(!canContinue) return;

			BaseDocument document = null;
            try
            {
                switch (eventType)
                {
                    case Enums.HSEvent.CONFIG_CHANGE:
                        {
							document = new ConfigChangeEvent(parameters);
                        }
                        break;
                    case Enums.HSEvent.LOG:
						document = new LogEvent(parameters);
                        break;
                    case Enums.HSEvent.STRING_CHANGE:
						document = new StringChangeEvent(parameters);
                        break;
                    case Enums.HSEvent.VALUE_CHANGE:
                        {
							document = new ValueChangeEvent(parameters);
                        }
                        break;
					case Enums.HSEvent.GENERIC:
						{
							document = new GenericEvent(parameters);
						}
						break;
					case Enums.HSEvent.SETUP_CHANGE:
						{
							document = new SetupChangeEvent();
						}
						break;
                    default:
                        LogInfo(string.Format("No handler yet for HSEvent type {0}", eventType.ToString()));
                        Console.WriteLine(" - HSEvent {0}: {1}", eventType.ToString(), String.Join(" | ", parameters));
                        break;
                }

				if(document != null)
				{
					this.esManager.WriteDocument(document);
				}
            }
            catch (Exception e)
            {
                LogError(string.Format("Error while handling HS Event: {0}", e.Message));
            }
        }

        public override string InitIO(string port)
        {
           

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            Config = new PluginConfig(HS);

            LogInfo("Initializing Elasticsearch plugin...");

            esManager = new ElasticsearchManager(HS, this);
            esManager.Initialize();
            Callback.RegisterEventCB(Enums.HSEvent.CONFIG_CHANGE, Name, "");
            Callback.RegisterEventCB(Enums.HSEvent.LOG, Name, "");
            Callback.RegisterEventCB(Enums.HSEvent.SETUP_CHANGE, Name, "");
            Callback.RegisterEventCB(Enums.HSEvent.STRING_CHANGE, Name, "");
            Callback.RegisterEventCB(Enums.HSEvent.GENERIC, Name, "");

            string link = ConfigPage.Name;
            HS.RegisterPage(link, Name, string.Empty);

            HomeSeerAPI.WebPageDesc wpd = new HomeSeerAPI.WebPageDesc
            {
                plugInName = Name,
                link = link,
                linktext = "Configuration",
                page_title = $"{Name} Configuration"
            };
            Callback.RegisterConfigLink(wpd);
            Callback.RegisterLink(wpd);

            LogInfo("Initialization Complete!");
            return "";
        }

        public override string PostBackProc(string page, string data, string user, int userRights)
        {
            if (page == ConfigPage.Name)
            {
                using (var configPage = new ConfigPage(HS, this))
                {
                    return configPage.PostBackProc(data, user, userRights);
                }
            }
            return string.Empty;

        }

        public override bool RaisesGenericCallbacks()
        {
            return true;
        }


        public override void ShutdownIO()
        {
            esManager.Stop();
            Running = false;
        }

        private void PluginConfig_ConfigChanged(object sender, EventArgs e)
        {

        }

    }
}
