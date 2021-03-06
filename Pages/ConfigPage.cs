﻿using HomeSeerAPI;
using NullGuard;
using Scheduler;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Web;
using Nest;
using Hspi.Exceptions;
using System.Collections.Generic;

namespace Hspi.Pages
{

    /// <summary>
    /// Helper class to generate configuration page for plugin
    /// </summary>
    /// <seealso cref="Scheduler.PageBuilderAndMenu.clsPageBuilder" />
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal class ConfigPage : PageHelper
    {
        protected const string IdPrefix = "id_";

        private ElasticsearchPlugin pluginInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigPage" /> class.
        /// </summary>
        /// <param name="HS">The hs.</param>
        /// <param name="pluginInstance">The plugin instance.</param>
        public ConfigPage(IHSApplication HS, ElasticsearchPlugin pluginInstance) : base(HS, pluginInstance.Config, pageName)
        {
            this.pluginInstance = pluginInstance;
        }

        /// <summary>
        /// Gets the name of the web page.
        /// </summary>
        public static string Name => pageName;

        /// <summary>
        /// Get the web page string for the configuration page.
        /// </summary>
        /// <returns>
        /// System.String.
        /// </returns>
        public string GetWebPage()
        {
            try
            {
                reset();

                AddHeader(HS.GetPageHeader(Name, "Configuration", string.Empty, string.Empty, false, false));

                System.Text.StringBuilder stb = new System.Text.StringBuilder();
                stb.Append(PageBuilderAndMenu.clsPageBuilder.DivStart("pluginpage", ""));
                stb.Append(BuildWebPageBody());
                stb.Append(PageBuilderAndMenu.clsPageBuilder.DivEnd());
                AddBody(stb.ToString());

                AddFooter(HS.GetPageFooter());
                suppressDefaultFooter = true;

                return BuildPage();
            }
            catch (Exception e)
            {
                return e.GetFullMessage();
            }
        }

        /// <summary>
        /// The user has selected a control on the configuration web page.
        /// The post data is provided to determine the control that initiated the post and the state of the other controls.
        /// </summary>
        /// <param name="data">The post data.</param>C:\Users\Larry\Desktop\HSPI_WUWeather\src\Pages\ConfigPage.cs
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <returns>Any serialized data that needs to be passed back to the web page, generated by the clsPageBuilder class.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Twilio")]
        public string PostBackProc(string data, [AllowNull]string user, int userRights)
        {
            NameValueCollection parts = HttpUtility.ParseQueryString(data);

            string form = parts["id"];


            /////
            /// 
            /// 
            PluginConfig config = this.pluginConfig;
            string action = parts["id"];

            if (NameToIdWithPrefix(SaveButtonName) == action)
            {
                try
                {
                    PopulatePluginConfig(config, parts);
                    config.FireConfigChanged();

                    this.divToUpdate.Add(SuccessDivId, "Settings updated successfully");
                }
                catch (Exception e)
                {
                    pluginInstance.LogError(string.Format("Error updating settings: {0}", e.Message));
                    this.divToUpdate.Add(ErrorDivId, "Error updating settings");
                }
            }
            else if (NameToIdWithPrefix(TestButtonName) == action)
            {
                PluginConfig testConfig;
                using (testConfig = new PluginConfig(HS, true))
                {
                    PopulatePluginConfig(testConfig, parts);

                    ConnectionTestResults results = ElasticsearchManager.PerformConnectivityTest(testConfig);

                    if (results.ConnectionSuccessful)
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
                        if (results.ClusterHealth != null && results.ClusterHealth.OriginalException != null)
                        {
                            message += ": " + results.ClusterHealth.OriginalException.Message;
                        }

                        this.divToUpdate.Add(SuccessDivId, "");
                        this.divToUpdate.Add(ErrorDivId, message);
                    }
                }
            }

            return base.postBackProc(Name, data, user, userRights);

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

        protected static void UpdatePluginSettings(object manager, NameValueCollection formData)
        {

            foreach (string arg in formData)
            {
                if (arg.StartsWith("eventType_", StringComparison.Ordinal))
                {
                    int eventTypeId = int.Parse(arg.Split('_')[1]);
                    bool enabled = formData[arg] == "checked";
                    //settings
                        //.EventTypeSettings
                        //.First(s => s.EventType.EventTypeId == eventTypeId).Enabled = enabled;
                }
            }

            //manager.UpdateSettings(settings);
        }

        /// <summary>
        /// Builds the web page body for the configuration page.
        /// The page has separate forms so that only the data in the appropriate form is returned when a button is pressed.
        /// </summary>
        private string BuildWebPageBody()
        {
            int i = 0;
            StringBuilder stb = new StringBuilder();

            var tabs = new clsJQuery.jqTabs("tab1id", PageName);
            var tab1 = new clsJQuery.Tab
            {
                tabTitle = "Elasticsearch Settings",
                tabDIVID = String.Format(CultureInfo.InvariantCulture, "tabs{0}", i++),
                tabContent = BuildSettingTab()
            };
            tabs.tabs.Add(tab1);

            tabs.postOnTabClick = false;
            stb.Append(tabs.Build());

            return stb.ToString();
        }

        private string BuildSettingTab()
        {
            PluginConfig pluginConfig = this.pluginConfig;

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

            stb.Append(this.BuildEventTypesSelection(pluginConfig));

            stb.Append($"<tr><td colspan=2><div id='{ErrorDivId}' style='color:Red'></div></td></tr>");
            stb.Append($"<tr><td colspan=2><div id='{SuccessDivId}' style='color:dodgerblue'></div></td></tr>");
            stb.Append($"<tr><td colspan=2>{FormButton(TestButtonName, TestButtonName, "Test Configuration")} {FormButton(SaveButtonName, SaveButtonName, "Save Settings")}</td></tr>");
            stb.Append(@" </table>");
            stb.Append(@"</div>");
            stb.Append(PageBuilderAndMenu.clsPageBuilder.FormEnd());

            return stb.ToString();
        }

        protected string BuildSecurityOptions(PluginConfig pluginConfig)
        {
            StringBuilder stb = new StringBuilder();

            NameValueCollection securityOptions = new NameValueCollection
            {
                { "disabled", "Disabled" },
                { "basic", "X-Pack (Basic Security)" }
            };

            stb.Append($"<tr><td class='tablecell headerCell'>Security:</td><td class='tablecell' style='width: 100px'>{FormDropDown(SecurityTypeId, securityOptions, pluginConfig.SecurityType)}</td></tr>");

            stb.Append($"<tr class='securityOption'><td class='tablecell headerCell'>Username:</td><td class='tablecell' style='width: 100px'>{HtmlTextBox(UsernameId, pluginConfig.Username, 40)}</td></tr>");
            stb.Append($"<tr class='securityOption'><td class='tablecell headerCell'>Password:</td><td class='tablecell' style='width: 100px'>{HtmlTextBox(PasswordId, pluginConfig.Password, 40, "password")}</td></tr>");

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
            string dropdownScript = @"$(() => { $('#" + dropdownId + @"').change(updateSecurityOptionVisibility); updateSecurityOptionVisibility(); })";
            stb.Append($"<tr><td><script>{changeHandler} {dropdownScript}</script></td></tr>");

            return stb.ToString();
        }

        protected string BuildEventTypesSelection(PluginConfig config)
        {
            string[] enabledSettings = config.EnabledEvents.Split(',');
            StringBuilder stb = new StringBuilder();
            stb.Append(@"<div>");
            stb.Append(@"<style> .headerCell {width: 25%;} </style>");
            stb.Append(@"<table class='full_width_table'>");
            stb.Append(@"<tr><td class='tableheader'>Event Types</td></tr>");

            foreach(EnabledEventType e in config.Events)
            {
                stb.Append($"<tr><td class='tablecell'>{FormCheckBox("eventType_" + e.eventType.EventTypeId, e.eventType.Name, e.enabled)}</td></tr>");
            }

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

            List<int> enabledEventIds = new List<int>();
            foreach(string key in formData)
            {
                if (key.StartsWith("eventType_", StringComparison.InvariantCulture) && formData.Get(key) == "checked")
                {
                    string idStr = key.Substring(10);
                    if (int.TryParse(idStr, out int eventId))
                    {
                        enabledEventIds.Add(eventId);
                    }
                }
            }
            config.EnabledEvents = string.Join(",", enabledEventIds);
        }

        private const string EnabledId = "EnabledId";
        private const string ElasticsearchUrlId = "ElasticsearchUrlId";
        private const string UsernameId = "UsernameId";
        private const string PasswordId = "PasswordId";
        private const string DebugLoggingId = "DebugLoggingId";
        private const string SecurityTypeId = "SecurityTypeId";
        private const string EnabledEventsId = "EnabledEventsId";
        private const string SaveButtonName = "Save";
        private const string TestButtonName = "Test";
        private const string ErrorDivId = "message_id";
        private const string SuccessDivId = "success_message_id";
        private const string AccountSIDId = "AccountSIDId";
        private const string AuthTokenId = "AuthTokenId";
        private const string FromNumberId = "FromNumberId";
        private static readonly string pageName = $"Elasticsearch Configuration".Replace(' ', '_');
    }
}