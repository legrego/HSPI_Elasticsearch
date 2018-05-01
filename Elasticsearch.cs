using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HomeSeerAPI;
using HSPI_Elasticsearch;
using HSPI_Elasticsearch.Documents;
using Nest;

namespace HSPI_Elasticsearch
{
    class ElasticsearchManager
    {
        IHSApplication HS;
        IAppCallbackAPI HSCB;
        HSPI hspiInst;
		Timer publishTimer;
		ConcurrentBag<BaseDocument> cache;
		public PluginConfig pluginConfig;
		
		
        public void Stop()
        {
            // Shutdown thing
            Console.WriteLine(" - DONE");

        }
        public ElasticsearchManager(IHSApplication pHsHost, IAppCallbackAPI pHsCB, HSPI pHspiInst)
        {
            hspiInst = pHspiInst;
            HS = pHsHost;
            HSCB = pHsCB;
			cache = new ConcurrentBag<BaseDocument>();
        }

        public void Initialize()
        {
			this.pluginConfig = new PluginConfig(HS);
			this.pluginConfig.ConfigChanged += onConfigChange;

			if(this.pluginConfig.Enabled && this.IsConfigValid())
			{
				ElasticClient client = GetESClient();
				this.CreateIndexTemplates(client);
				this.CreateInitialIndex(client);
				this.StartRolloverTimer();
				this.WriteToCluster();
			}
        }

		public void WriteDocument(BaseDocument document)
		{
			this.cache.Add(document);
		}

		protected void onConfigChange(object sender, EventArgs e)
		{

		}

		protected bool IsConfigValid()
		{
			string url = this.pluginConfig.ElasticsearchUrl;

			Uri uri;
			return Uri.TryCreate(url, UriKind.Absolute, out uri);
		}

		protected void CreateIndexTemplates(ElasticClient client)
		{
			Console.Write("Index Template ");
			IExistsResponse templateExists = client.IndexTemplateExists("homeseer-active-events");
			if(templateExists.Exists)
			{
				Console.WriteLine("already exists!");
				return;
			}

			Console.WriteLine("doesn't exist. Creating...");

			client.PutIndexTemplate("homeseer-active-events", (c) => c
				.Mappings(ms => ms.Map<BaseDocument>(m => m.AutoMap()))
				.IndexPatterns(new string[] { "homeseer-events-*" })
				.Settings(s => s
					.NumberOfShards(3)
					.NumberOfReplicas(0)
				)
				.Aliases(a => a.Alias("search-homeseer-events"))
			);
		}

		protected void CreateInitialIndex(ElasticClient client)
		{
			Console.Write("Initial index & alias ");
			IExistsResponse aliasExists = client.AliasExists(a => a.Name("active-homeseer-index"));
			if(aliasExists.Exists)
			{
				Console.WriteLine("already exists!");
				return;
			}

			Console.WriteLine("doesn't exist. Creating...");
				
			client.CreateIndex("homeseer-events-1");
			client.Alias(s => s.Add(a => a.Index("homeseer-events-1").Alias("active-homeseer-index")));
		}

		protected void StartRolloverTimer()
		{
			Timer rolloverTimer = null;

			int oneHour = 1000 * 60 * 60;
			int thirtySeconds = 30000;

			rolloverTimer = new Timer((g) => {
				try
				{
					Console.WriteLine("Calling Rollover API");
					ElasticClient client = this.GetESClient();
					client.RolloverIndex("active-homeseer-index", ri => ri
						.Conditions(c => c.MaxAge("7d").MaxDocs(5))
					);
				}
				catch(Exception e)
				{
					Console.WriteLine(string.Format("Error calling Rollover API: {0}", e.Message));
				}

				rolloverTimer.Change(thirtySeconds, Timeout.Infinite);

			}, null, 0, Timeout.Infinite);
			
		}

		protected void WriteToCluster()
		{
			Timer timer = null;

			int thirtySeconds = 1000 * 30;

			timer = new Timer((g) => {

				BaseDocument document;
				IBulkRequest request = new BulkRequest("active-homeseer-index");
				request.Operations = new List<IBulkOperation>();

				while(!this.cache.IsEmpty)
				{
					if(this.cache.TryTake(out document))
					{
						request.Operations.Add(new BulkIndexOperation<BaseDocument>(document));
					}
				}

				if(request.Operations.Count == 0)
				{
					Console.WriteLine("WriteToCluster: no documents to write");
					timer.Change(thirtySeconds, Timeout.Infinite);
					return;
				}

				try
				{
					Console.Write(string.Format("Writing {0} documents to Elasticsearch...", request.Operations.Count));
					ElasticClient client = this.GetESClient();
					IBulkResponse r = client.Bulk(request);
					if(r.IsValid)
					{
						Console.WriteLine("Success!");
					}
					else
					{
						Console.WriteLine("Failed!:");
						Console.Write(r.DebugInformation);
					}
				}
				catch(Exception e)
				{
					Console.WriteLine(string.Format("Error writing documents to Elasticsearch: {0}", e.Message));
				}
				timer.Change(thirtySeconds, Timeout.Infinite);
			}, null, 0, Timeout.Infinite);
		}

		protected ElasticClient GetESClient()
		{
			ConnectionSettings connection = new Nest.ConnectionSettings(new Uri(this.pluginConfig.ElasticsearchUrl));

			if(this.pluginConfig.Username != null && this.pluginConfig.Password != null)
			{
				connection.BasicAuthentication(this.pluginConfig.Username, this.pluginConfig.Password);
			}

			connection.DefaultIndex("active-homeseer-index");

			ElasticClient client = new Nest.ElasticClient(connection);
			return client;
		}
    }
}
