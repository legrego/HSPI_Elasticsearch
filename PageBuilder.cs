using System;
using System.IO.Ports;
using System.Text;
using System.Web;
using System.Reflection;
using System.Collections.Specialized;
using HomeSeerAPI;
using Scheduler;
using Nest;
using System.Collections.Generic;
using HSPI_Elasticsearch.Settings;
using System.Linq;

namespace HSPI_Elasticsearch
{

    public class PageReturn
    {
        public String content { get; private set; }
        public bool full_page { get; private set; }

        public PageReturn(String pContent, bool pFullPage)
        {
            content = pContent;
            full_page = pFullPage;
        }
    }
    /// <remarks>
    /// Class that contains basic functions for building HTML pages and processing postbacks
    /// </remarks>
    public abstract class PageBuilderBase : Scheduler.PageBuilderAndMenu.clsPageBuilder
    {
        protected IHSApplication hsHost { get; private set; }
        protected IAppCallbackAPI hsHostCB { get; private set; }
		protected HSPI pluginInstance { get; private set; }
		protected Logger logger { get; private set; }

		protected PageBuilderBase(IHSApplication pHS, IAppCallbackAPI pHSCB, HSPI plugInInst)
            : base("dummy")
        {
            hsHost = pHS;
            hsHostCB = pHSCB;
            pluginInstance = plugInInst;
			this.logger = pluginInstance.logger;
            reset();
        }

        public String GetPage(String pPageName, String pParamValue)
        {
            reset();
			if(pPageName == null)
			{
				return null;
			}

            PageName = pPageName;
            String pPageNameClean = pPageName;
            if (pPageNameClean.Contains("/"))
            {
                pPageNameClean = pPageName.Split('/')[1];
            }

            NameValueCollection parts = HttpUtility.ParseQueryString(pParamValue);

			PageReturn page = this.HandleGetPage(pPageName, parts);

            if (page.full_page)
            {
                return page.content;
            }
            else
            {
                AddHeader(hsHost.GetPageHeader(pPageName, pPageNameClean, "", "", false, true));
                AddBody(page.content);
                this.RefreshIntervalMilliSeconds = 10;
                suppressDefaultFooter = true;
                AddFooter(hsHost.GetPageFooter());
                return BuildPage();
            }
        }

		protected abstract PageReturn HandleGetPage(String pPageName, NameValueCollection parts);

        public String PostBack(String pPageName, String pParamValue, String pUser, int pUserRights)
        {
            reset();
			if(pPageName == null)
			{
				return null;
			}
            PageName = pPageName;
            String pPageNameClean = pPageName;
            if (pPageNameClean.Contains("/"))
            {
                pPageNameClean = pPageName.Split('/')[1];
            }

            NameValueCollection parts = HttpUtility.ParseQueryString(pParamValue);

			PageReturn page = this.HandlePostBack(pPageName, parts);
			if(page.full_page)
			{
				return page.content;
			}
			else
			{
				return postBackProc(pPageName, pParamValue, pUser, pUserRights);
			}
        }

		protected abstract PageReturn HandlePostBack(String pPageName, NameValueCollection pargs);
    }
}


namespace HSPI_Elasticsearch
{
    class PageBuilder : PageBuilderBase
    {
        private ElasticsearchManager mCore;
        private IHSApplication HS;
		private const string IdPrefix = "id_";

		private const string EnabledId = "EnabledId";
		private const string ElasticsearchUrlId = "ElasticsearchUrlId";
		private const string UsernameId = "UsernameId";
		private const string PasswordId = "PasswordId";
		private const string DebugLoggingId = "DebugLoggingId";
		private const string SecurityTypeId = "SecurityTypeId";
		private const string SaveButtonName = "Save";
		private const string TestButtonName = "Test";
		private const string ErrorDivId = "message_id";
		private const string SuccessDivId = "success_message_id";

		public PageBuilder(IHSApplication pHS, IAppCallbackAPI pHSCB, HSPI_Elasticsearch.HSPI pluginInstance, ElasticsearchManager pCore)
            : base(pHS, pHSCB, pluginInstance)
        {
            mCore = pCore;
            HS = pHS;
        }

		protected override PageReturn HandleGetPage(String pPageName, NameValueCollection pArgs)
		{
			return Page_HS3_Elasticsearch(pArgs);
		}

		private string BuildSettingTab()
		{
			PluginConfig pluginConfig = this.mCore.pluginConfig;

			StringBuilder stb = new StringBuilder();
			stb.Append(PageBuilderAndMenu.clsPageBuilder.FormStart("ftmSettings", "IdSettings", "Post"));

			stb.Append(@"<br>");
			stb.Append(@"<div>");
			stb.Append(@"<style> .headerCell {width: 25%;} </style>");
			stb.Append(@"<table class='full_width_table'>");
			stb.Append(@"<tr><td class='tableheader' colspan=2>Plugin Settings</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Enabled:</td><td class='tablecell' style='width: 100px'>{FormCheckBox(EnabledId, string.Empty, pluginConfig.Enabled)}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Debug Logging Enabled:</td><td colspan=2 class='tablecell'>{FormCheckBox(DebugLoggingId, string.Empty, pluginConfig.DebugLogging)}</td></tr>");

			stb.Append(@"<tr><td class='tableheader' colspan=2>Elasticsearch Settings</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Elasticsearch URL:</td><td class='tablecell' style='width: 100px'>{HtmlTextBox(ElasticsearchUrlId, pluginConfig.ElasticsearchUrl, 40)}</td></tr>");
			stb.Append(this.BuildSecurityOptions(pluginConfig));

			stb.Append(this.BuildEventTypesSelection());			
			
			stb.Append($"<tr><td colspan=2><div id='{ErrorDivId}' style='color:Red'></div></td></tr>");
			stb.Append($"<tr><td colspan=2><div id='{SuccessDivId}' style='color:dodgerblue'></div></td></tr>");
			stb.Append($"<tr><td colspan=2>{FormButton(TestButtonName, TestButtonName, "Test Configuration")} {FormButton(SaveButtonName, SaveButtonName, "Save Settings")}</td></tr>");
			stb.Append(@" </table>");
			stb.Append(@"</div>");
			stb.Append(PageBuilderAndMenu.clsPageBuilder.FormEnd());

			return stb.ToString();
		}

		protected override PageReturn HandlePostBack(string pPageName, NameValueCollection pargs)
		{
			// TODO: actually check page name here.
			if(pPageName != null)
			{
				return PostHandler_HS3_Elasticsearch(pargs);
			}
			return null;
		}

		public PageReturn PostHandler_HS3_Elasticsearch(NameValueCollection pArgs)
        {
			PluginConfig config = this.mCore.pluginConfig;
			string action = pArgs["id"];

			if(NameToIdWithPrefix(SaveButtonName) == action)
			{
				try
				{
					PopulatePluginConfig(config, pArgs);
					config.FireConfigChanged();

					UpdatePluginSettings(this.pluginInstance.settingsManager, pArgs);
					
					this.divToUpdate.Add(SuccessDivId, "Settings updated successfully");
				}
				catch (Exception e)
				{
					logger.LogError(string.Format("Error updating settings: {0}", e.Message));
					this.divToUpdate.Add(ErrorDivId, "Error updating settings");
				}
			}
			else if(NameToIdWithPrefix(TestButtonName) == action)
			{
				PluginConfig testConfig;
				using(testConfig = new PluginConfig(HS, true))
				{
					PopulatePluginConfig(testConfig, pArgs);

					ConnectionTestResults results = ElasticsearchManager.PerformConnectivityTest(testConfig);

					if(results.ConnectionSuccessful)
					{
						StringBuilder stb = new StringBuilder();
						stb.Append(@"<div>");
						stb.Append(@"<h3>Connection Test Successful!<h3>");
						stb.Append(BuildClusterHealthView(results.ClusterHealth));
						stb.Append(@"</div>");
						this.divToUpdate.Add(SuccessDivId, stb.ToString());
						this.divToUpdate.Add(ErrorDivId, "");
					}
					else
					{
						string message = "Connection test failed";
						if(results.ClusterHealth != null && results.ClusterHealth.OriginalException != null)
						{
							message += ": " + results.ClusterHealth.OriginalException.Message;
						}

						this.divToUpdate.Add(SuccessDivId, "");
						this.divToUpdate.Add(ErrorDivId, message);
					}
				}
			}

            return new PageReturn("", false);
        }

		private static string BuildClusterHealthView(IClusterHealthResponse health)
		{
			StringBuilder stb = new StringBuilder();
			stb.Append(@"<div>");
			stb.Append(@"<style> .headerCell {width: 25%;} </style>");
			stb.Append(@"<table class='full_width_table'>");
			stb.Append(@"<tr><td class='tableheader' colspan=2>Cluster Health</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Cluster Name:</td><td class='tablecell' style='width: 100px'>{health.ClusterName}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Status:</td><td class='tablecell' style='width: 100px color: {health.Status};'>{health.Status}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Number of Nodes:</td><td class='tablecell' style='width: 100px'>{health.NumberOfNodes}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Number of Data Nodes:</td><td class='tablecell' style='width: 100px'>{health.NumberOfDataNodes}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Active Primary Shards:</td><td class='tablecell' style='width: 100px'>{health.ActivePrimaryShards}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Active Shards:</td><td class='tablecell' style='width: 100px'>{health.ActiveShards}</td></tr>");
			stb.Append($"<tr><td class='tablecell headerCell'>Number of Pending Tasks:</td><td class='tablecell' style='width: 100px'>{health.NumberOfPendingTasks}</td></tr>");
			stb.Append(@" </table>");
			stb.Append(@"</div>");

			return stb.ToString();
		}

		protected static void PopulatePluginConfig(PluginConfig config, NameValueCollection formData)
		{
			config.Enabled = formData.Get(EnabledId) == "checked";
			config.ElasticsearchUrl = formData.Get(ElasticsearchUrlId);
			config.Username = formData.Get(UsernameId);
			config.Password = formData.Get(PasswordId);
			config.DebugLogging = formData.Get(DebugLoggingId) == "checked";
			config.SecurityType = formData.Get(SecurityTypeId);
		}

		protected static void UpdatePluginSettings(HSPI_Elasticsearch.Settings.SettingsManager manager, NameValueCollection formData)
		{
			AppSettings settings = manager.Settings;

			foreach(string arg in formData)
			{
				if(arg.StartsWith("eventType_", StringComparison.Ordinal))
				{
					int eventTypeId = int.Parse(arg.Split('_')[1]);
					bool enabled = formData[arg] == "checked";
					settings
						.EventTypeSettings
						.First(s => s.EventType.EventTypeId == eventTypeId).Enabled = enabled;
				}
			}

			manager.UpdateSettings(settings);
		}
        
        public PageReturn Page_HS3_Elasticsearch(NameValueCollection pArgs)
        {
            var stb = new StringBuilder();

            string conf_node_id = pArgs.Get("configure_node");
            string conf_controller_id = pArgs.Get("controller_id");
            stb.Append(DivStart("pluginpage", ""));

            // Add message area for (ajax) errors
            stb.Append(DivStart("errormessage", "class='errormessage'"));
            stb.Append(DivEnd());

            stb.Append(DivEnd());

			stb.Append(BuildSettingTab());

            return new PageReturn(stb.ToString(), false);
        }

		protected static string HtmlTextBox(string name, string defaultText, int size = 25)
		{
			return $"<input type=\'text\' id=\'{NameToIdWithPrefix(name)}\' size=\'{size}\' name=\'{name}\' value=\'{defaultText}\'>";
		}

		protected static string HtmlPasswordBox(string name, string defaultText, int size = 25)
		{
			return $"<input type=\'password\' id=\'{NameToIdWithPrefix(name)}\' size=\'{size}\' name=\'{name}\' value=\'{defaultText}\'>";
		}

		protected string BuildSecurityOptions(PluginConfig pluginConfig)
		{
			StringBuilder stb = new StringBuilder();

			List<Pair> securityOptions = new List<Pair>();
			securityOptions.Add(new Pair() { Name = "Disabled", Value = "disabled" });
			securityOptions.Add(new Pair() { Name = "X-Pack (Basic Security)", Value = "basic" });

			stb.Append($"<tr><td class='tablecell headerCell'>Security:</td><td class='tablecell' style='width: 100px'>{HtmlDropDown(SecurityTypeId, pluginConfig.SecurityType, securityOptions)}</td></tr>");

			stb.Append($"<tr class='securityOption'><td class='tablecell headerCell'>Username:</td><td class='tablecell' style='width: 100px'>{HtmlTextBox(UsernameId, pluginConfig.Username, 40)}</td></tr>");
			stb.Append($"<tr class='securityOption'><td class='tablecell headerCell'>Password:</td><td class='tablecell' style='width: 100px'>{HtmlPasswordBox(PasswordId, pluginConfig.Password, 40)}</td></tr>");

			string dropdownId = NameToIdWithPrefix(SecurityTypeId);
			string changeHandler = @"function updateSecurityOptionVisibility(e) {
				const value = e ? e.target.value : $('#" + dropdownId + @"').val();
				switch(value) {
					case 'basic':
						$('.securityOption').show();
						break;
					default:
						$('.securityOption').hide();
				}
			}";
			string dropdownScript = @"$(() => { $('#"+dropdownId+@"').change(updateSecurityOptionVisibility); updateSecurityOptionVisibility(); })";
			stb.Append($"<tr><td><script>{changeHandler} {dropdownScript}</script></td></tr>");

			return stb.ToString();
		}

		protected string BuildEventTypesSelection()
		{
			AppSettings settings = this.pluginInstance.settingsManager.Settings;
			StringBuilder stb = new StringBuilder();
			stb.Append(@"<div>");
			stb.Append(@"<style> .headerCell {width: 25%;} </style>");
			stb.Append(@"<table class='full_width_table'>");
			stb.Append(@"<tr><td class='tableheader'>Event Types</td></tr>");
			settings
				.EventTypeSettings
				.Select((e) => stb.Append(
					$"<tr><td class='tablecell'>{FormCheckBox("eventType_" + e.EventType.EventTypeId, e.EventType.Name, e.Enabled)}</td></tr>"
				))
				.ToArray();
			stb.Append(@" </table>");
			stb.Append(@"</div>");

			return stb.ToString();
		}

		protected string HtmlDropDown(string name, string value, List<Pair> values)
		{
			var dropdown = new clsJQuery.jqDropList(name, PageName, false);
			dropdown.id = NameToIdWithPrefix(name);
			dropdown.items = values;
			dropdown.selectedItemIndex = values.FindIndex(p => p.Value == value);

			return dropdown.Build();
		}

		protected string FormCheckBox(string name, string label, bool @checked)
		{
			var checkbox = new clsJQuery.jqCheckBox(name, label, PageName, true, true) {
				id = NameToIdWithPrefix(name),
				@checked = @checked,
			};
			return checkbox.Build();
		}

		protected string FormButton(string name, string label, string toolTip, bool submit = true)
		{
			var button = new clsJQuery.jqButton(name, label, PageName, submit) {
				id = NameToIdWithPrefix(name),
				toolTip = toolTip,
			};
			button.toolTip = toolTip;
			button.enabled = true;

			return button.Build();
		}

		private static string NameToId(string name)
		{
			return name.Replace(' ', '_');
		}

		private static string NameToIdWithPrefix(string name)
		{
			return $"{ IdPrefix}{NameToId(name)}";
		}
	}
}