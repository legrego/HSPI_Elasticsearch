﻿using System;
using System.Collections.Generic;
using System.Globalization;

using System.Threading;
using HomeSeerAPI;
using HSPI_Elasticsearch.Documents;

[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
namespace HSPI_Elasticsearch
{

    public class HSPI : IPlugInAPI, IDisposable
    {
        private ElasticsearchManager mCore;
        private Dictionary<int, DateTime> mLastDSUpdate = new Dictionary<int, DateTime>();
        PageBuilder mPageBuilder;

        private string mFriendlyName = Constants.PLUGIN_STRING_NAME;
        public bool Running = true;
        public IHSApplication hsHost;
        public IAppCallbackAPI hsHostCB;

        // HS3 Plugin properties
        public string Name { get; private set; }

        public bool HSCOMPort { get; private set; }

        public bool ActionAdvancedMode { set; get; }

        public bool HasTriggers { get; private set; }

        public int TriggerCount { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
				// dispose managed resources
				mPageBuilder.Dispose();
                mPageBuilder = null;
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // HS3 Plugin methods
        public int AccessLevel()
        {
            return 1;  // 2 == Needs license, 1 .. free ?
        }

        public string ActionBuildUI(string sUnique, HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return "";
        }

        public bool ActionConfigured(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return false;
        }

        public int ActionCount()
        {
            return 0;
        }

        public string ActionFormatUI(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return "";
            //return null;
        }

        public HomeSeerAPI.IPlugInAPI.strMultiReturn ActionProcessPostUI(System.Collections.Specialized.NameValueCollection PostData, HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfoIN)
        {
            HomeSeerAPI.IPlugInAPI.strMultiReturn r = new IPlugInAPI.strMultiReturn();
            r.sResult = "NOT IMPLEMENTED";
            return r;
        }

        public bool ActionReferencesDevice(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo, int dvRef)
        {
            return false;
        }

        public int Capabilities()
        {
            return (int)HomeSeerAPI.Enums.eCapabilities.CA_IO;
        }

        public string ConfigDevice(int pDevRefId, string user, int userRights, bool pNewDevice)
        {
            return "";
        }

        public HomeSeerAPI.Enums.ConfigDevicePostReturn ConfigDevicePost(int devref, string data, string user, int userRights)
        {
            return Enums.ConfigDevicePostReturn.DoneAndCancelAndStay;
        }

        public string GenPage(string link)
        {
            return "old stuff";
        }

        public string get_ActionName(int ActionNumber)
        {
            return "";
        }

        public bool get_Condition(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return false;
        }

        public bool get_HasConditions(int TriggerNumber)
        {
            return false;
        }

        public int get_SubTriggerCount(int TriggerNumber)
        {
            return 0;
        }

        public string get_SubTriggerName(int TriggerNumber, int SubTriggerNumber)
        {
            return "";
        }

        public bool get_TriggerConfigured(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return false;
        }

        public string get_TriggerName(int TriggerNumber)
        {
            return "";
        }

        public string GetPagePlugin(string page, string user, int userRights, string queryString)
        {

            return mPageBuilder.GetPage(page, queryString);
        }

        public bool HandleAction(HomeSeerAPI.IPlugInAPI.strTrigActInfo ActInfo)
        {
            return false;
        }

        public void HSEvent(HomeSeerAPI.Enums.HSEvent EventType, object[] parms)
        {
			BaseDocument document = null;
            try
            {
                string Source = "Unknown";
                switch (EventType)
                {
                    case Enums.HSEvent.CONFIG_CHANGE:
                        {
							document = new ConfigChangeEvent(parms);
                            Console.WriteLine("Got CONFIG_CHANGE event");
                            Console.WriteLine(" - HSEvent {0}: {1}/{2}/{3}/{4}/{5}", EventType.ToString(), parms[5].ToString(), parms[0].ToString(), parms[1].ToString(), parms[2].ToString(), parms[3].ToString());
                        }
                        break;
                    case Enums.HSEvent.LOG:
						document = new LogEvent(parms);
                        break;
                    case Enums.HSEvent.STRING_CHANGE:
                        {
                            Source = parms[3].ToString();
                            string dev_addr = parms[1].ToString();
							document = new StringChangeEvent(parms);
                        }
                        break;
                    case Enums.HSEvent.VALUE_CHANGE:
                        {
							document = new ValueChangeEvent(parms);
                        }
                        break;
					case Enums.HSEvent.GENERIC:
						{
							document = new GenericEvent(parms);
						}
						break;
					case Enums.HSEvent.SETUP_CHANGE:
						{
							document = new SetupChangeEvent(parms);
						}
						break;
                    default:
                        Console.WriteLine("No handler yet for HSEvent type {0}", EventType.ToString());
                        Console.WriteLine(" - HSEvent {0}: {1}", EventType.ToString(), String.Join(" | ", parms));
                        break;
                }

				if(document != null)
				{
					this.mCore.WriteDocument(document);
				}
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception something foo: {0}", e.Message);
            }
        }

        public string InitIO(string port)
        {
            Console.WriteLine("InitIO called!");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            mCore = new ElasticsearchManager(hsHost, hsHostCB, this);
            mCore.Initialize();
            hsHostCB.RegisterEventCB(Enums.HSEvent.CONFIG_CHANGE, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.LOG, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.SETUP_CHANGE, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.STRING_CHANGE, Name, "");
            hsHostCB.RegisterEventCB(Enums.HSEvent.GENERIC, Name, "");

            this.mPageBuilder = new PageBuilder(hsHost, hsHostCB, this, mCore);

            hsHost.RegisterPage(Constants.PLUGIN_STRING_ID, Name, "");

            WebPageDesc wpd = new WebPageDesc();
            wpd.link = Constants.PLUGIN_STRING_ID;
            wpd.linktext = "Configuration";
            wpd.page_title = "Configuration";
            wpd.plugInName = Name;
            hsHostCB.RegisterLink(wpd);
            hsHostCB.RegisterConfigLink(wpd);

            Console.WriteLine("INIT IO complete");
            return "";
        }

        public string InstanceFriendlyName()
        {
            return mFriendlyName;
        }

        public HomeSeerAPI.IPlugInAPI.strInterfaceStatus InterfaceStatus()
        {
            HomeSeerAPI.IPlugInAPI.strInterfaceStatus r = new IPlugInAPI.strInterfaceStatus();
            r.intStatus = IPlugInAPI.enumInterfaceStatus.OK;
            r.sStatus = "Interface OK";
            return r;
        }

        public string PagePut(string data)
        {
            return "";
        }

        public object PluginFunction(string procName, object[] parms)
        {
            return "";
        }

        public object PluginPropertyGet(string procName, object[] parms)
        {
            return "";
        }

        public void PluginPropertySet(string procName, object value)
        {
        }

        public HomeSeerAPI.IPlugInAPI.PollResultInfo PollDevice(int dvref)
        {
            HomeSeerAPI.IPlugInAPI.PollResultInfo r = new IPlugInAPI.PollResultInfo();

            return r;
        }

        public string PostBackProc(string pPage, string pData, string pUser, int pUserRights)
        {
            return mPageBuilder.PostBack(pPage, pData, pUser, pUserRights);

        }

        public bool RaisesGenericCallbacks()
        {
            return true;
        }

        public HomeSeerAPI.SearchReturn[] Search(string SearchString, bool RegEx)
        {
            SearchReturn[] result = new SearchReturn[0];
            return result;
        }

        public void set_Condition(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo, bool Value)
        {
        }

        public void SetIOMulti(System.Collections.Generic.List<CAPI.CAPIControl> colSend)
        {
            Console.WriteLine("Set IO Multi : {0}", colSend[0]);
        }

        public void ShutdownIO()
        {
            mCore.Stop();
            Running = false;
        }

        public void SpeakIn(int device, string txt, bool w, string host)
        {
        }

        public bool SupportsAddDevice()
        {
            return false;
        }

        public bool SupportsConfigDevice()
        {
            return false;
        }

        public bool SupportsConfigDeviceAll()
        {
            return false;
        }

        public bool SupportsMultipleInstances()
        {
            return false;
        }

        public bool SupportsMultipleInstancesSingleEXE()
        {
            return false;
        }

        public string TriggerBuildUI(string sUnique, HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return "";
        }

        public string TriggerFormatUI(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return "";
        }

        public HomeSeerAPI.IPlugInAPI.strMultiReturn TriggerProcessPostUI(System.Collections.Specialized.NameValueCollection PostData, HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfoIN)
        {
            HomeSeerAPI.IPlugInAPI.strMultiReturn r = new IPlugInAPI.strMultiReturn();
            return r;
        }

        public bool TriggerReferencesDevice(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo, int dvRef)
        {
            return false;
        }

        public bool TriggerTrue(HomeSeerAPI.IPlugInAPI.strTrigActInfo TrigInfo)
        {
            return false;
        }

        public HSPI()
            : this("")
        {
        }

        public HSPI(String pInstance)
        {
            Name = Constants.PLUGIN_STRING_NAME;
            HSCOMPort = false;
            HasTriggers = false;
            TriggerCount = 0;

        }

    }
}
