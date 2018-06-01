using System;
using System.Globalization;
using System.Threading;

using HomeSeerAPI;
using HSCF.Communication.Scs.Communication;
using HSCF.Communication.Scs.Communication.EndPoints.Tcp;
using HSCF.Communication.ScsServices.Client;

namespace HSPI_Elasticsearch
{
    public class Manager : IDisposable
    {
        IScsServiceClient<IHSApplication> client;
        IScsServiceClient<IAppCallbackAPI> clientCB;
        IHSApplication hsHost;
        IAppCallbackAPI hsHostCB;

        HSPI pluginInst;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                pluginInst.Dispose();
                pluginInst = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void run()
        {
            string[] cmdArgs = Environment.GetCommandLineArgs();
            Console.WriteLine("Manager::run() - arguments are {0}", Environment.CommandLine);
            String paramServer = "192.168.10.20";
            foreach (string arg in cmdArgs)
            {
                Console.WriteLine(" - arg: {0}", arg);
                if (arg.Contains("="))
                {
                    String[] ArgS = arg.Split('=');
                    Console.WriteLine(" -- {0}=>{1}", ArgS[0], ArgS[1]);
                    switch (ArgS[0])
                    {
                        case "server":
                            paramServer = ArgS[1];
                            break;
                        default:
                            Console.WriteLine("Unhandled param: {0}", ArgS[0]);
                            break;

                    }
                }
            }
            pluginInst = new HSPI();

            //Environment.CommandLine.
            client = ScsServiceClientBuilder.CreateClient<IHSApplication>(new ScsTcpEndPoint(paramServer, 10400), pluginInst);
            clientCB = ScsServiceClientBuilder.CreateClient<IAppCallbackAPI>(new ScsTcpEndPoint(paramServer, 10400), pluginInst);

            try
            {
                client.Connect();
                clientCB.Connect();
                hsHost = client.ServiceProxy;
                double ApiVer = hsHost.APIVersion;
                Console.WriteLine("Host ApiVersion : {0}", ApiVer);
                hsHostCB = clientCB.ServiceProxy;
                ApiVer = hsHostCB.APIVersion;
                Console.WriteLine("Host CB ApiVersion : {0}", ApiVer);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot start instance because of : {0}", e.Message);
                return;
            }
            Console.WriteLine("Connection to HS succeeded!");
            try
            {
                pluginInst.hsHost = hsHost;
                pluginInst.hsHostCB = hsHostCB;
                hsHost.Connect(pluginInst.Name, "");
                Console.WriteLine("Connected, waiting to be initialized...");
                do
                {
                    Thread.Sleep(500);
                } while (client.CommunicationState == CommunicationStates.Connected && pluginInst.Running);

                Console.WriteLine("Connection lost, exiting");
                pluginInst.Running = false;

                client.Disconnect();
                clientCB.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to host connect: {0}", e.Message);
                return;
            }

            Console.WriteLine("Exiting!!!");
        }
    }
    class Program
    {
        static void Main()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			Manager m;
			using(m = new Manager())
			{
				m.run();
			}
        }
    }
}
