﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using Elasticsearch.Net;
using HomeSeerAPI;
using Hspi.Documents;
using Nest;

namespace Hspi
{
	class ConnectionTestResults
	{
		public IClusterHealthResponse ClusterHealth { get; set; }
		public bool ConnectionSuccessful { get; set; }
	}

    class ElasticsearchManager: IDisposable
    {
        IHSApplication HS;

		Timer publishTimer;
		Timer rolloverTimer;
        readonly ConcurrentBag<BaseDocument> cache;
        readonly ElasticsearchPlugin pluginInstance;

        private PluginConfig Config {  get { return pluginInstance.Config; } }


        public void Stop()
        {
			// Shutdown thing
			

        }

        public ElasticsearchManager(IHSApplication pHsHost, ElasticsearchPlugin pluginInstance)
        {
            HS = pHsHost;
            this.pluginInstance = pluginInstance;
            cache = new ConcurrentBag<BaseDocument>();
        }

        public void Initialize()
        {
			if(Config.Enabled && this.IsConfigValid())
			{
				ElasticClient client = GetESClient(Config);
				this.CreateIndexTemplates(client);
				this.CreateInitialIndex(client);
				this.StartRolloverTimer();
				this.StartPublishTimer();
			}
        }

		public static ConnectionTestResults PerformConnectivityTest(PluginConfig config, Logger logger = null)
		{
			ConnectionTestResults results = new ConnectionTestResults();
			try
			{
				ElasticClient client = GetESClient(config);
				results.ClusterHealth = client.ClusterHealth((s) => s.Level(Level.Cluster));
				results.ConnectionSuccessful = results.ClusterHealth.IsValid;
			}
			catch(Exception e)
			{
				if(logger != null)
				{
					logger.LogInfo(string.Format("Connection test failed: {0}", e.Message));
				}
				results.ConnectionSuccessful = false;
			}

			return results;
		}

		public void WriteDocument(BaseDocument document)
		{
			if(Config.Enabled)
			{
				this.cache.Add(document);
			}
		}

		protected void onConfigChange(object sender, EventArgs e)
		{
			bool isTimerEnabled = this.publishTimer != null;
			bool isConfigValid = this.IsConfigValid();

			bool shouldDisableTimer = isTimerEnabled && (!isConfigValid || !Config.Enabled);
			bool shouldEnableTimer = !isTimerEnabled && Config.Enabled && isConfigValid;

			if(shouldDisableTimer)
			{
				this.StopPublishTimer();
				this.StopRolloverTimer();
			}
			if(shouldEnableTimer)
			{
				pluginInstance.LogInfo("Enabling ES Publish Timer due to config change");
				this.Initialize();
			}

			if(!Config.Enabled)
            {
                // safely empty cache when disabling plugin
                while (!this.cache.IsEmpty) this.cache.TryTake(out BaseDocument discard);
            }
        }

		protected bool IsConfigValid()
		{
			string url = Config.ElasticsearchUrl;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri);
        }

        protected void CreateIndexTemplates(ElasticClient client)
		{
            pluginInstance.LogInfo("Creating/Updating index template 'homeseer-index-template'");

			try
			{
				client.PutIndexTemplate("homeseer-index-template", (c) => c
					.Mappings(ms => ms.Map<BaseDocument>(m => m.AutoMap()))
					.IndexPatterns(new string[] { "homeseer-events-*" })
					.Settings(s => s
						.NumberOfShards(1)
						.NumberOfReplicas(0)
					)
					.Aliases(a => a.Alias("all-homeseer-events"))
				);
			}
			catch(Exception e)
			{
                pluginInstance.LogError(string.Format("Error creating index template: {0}", e.Message));
				throw;
			}
			
		}

		protected void CreateInitialIndex(ElasticClient client)
		{
            pluginInstance.LogInfo("Setting up initial index and alias");

			IExistsResponse aliasExists = client.AliasExists("active-homeseer-index");
			if(aliasExists.Exists)
			{
                pluginInstance.LogInfo("Alias 'active-homeseer-index' already exists!");
				return;
			}

            pluginInstance.LogInfo("Creating alias and initial index");

			try
			{
				client.CreateIndex("homeseer-events-00001", index => index.Aliases(alias => alias.Alias("active-homeseer-index")));
			}
			catch(Exception e)
			{
                pluginInstance.LogError(string.Format("Error creating index or alias: {0}", e.Message));
				throw;
			}
		}

		protected void StartRolloverTimer()
		{
			int oneHour = 1000 * 60 * 60;

			rolloverTimer = new Timer((g) => {
				try
				{
                    pluginInstance.LogInfo("Calling Rollover API");
					ElasticClient client = GetESClient(Config);
					client.RolloverIndex("active-homeseer-index", ri => ri
						.Conditions(c => c.MaxAge("7d").MaxDocs(10000))
					);
				}
				catch(Exception e)
				{
                    pluginInstance.LogError(string.Format("Error calling Rollover API: {0}", e.Message));
				}

				rolloverTimer.Change(oneHour, Timeout.Infinite);

			}, null, 0, Timeout.Infinite);
			
		}

		protected void StopRolloverTimer()
		{
			if(this.rolloverTimer != null)
			{
				try
				{
					this.rolloverTimer.Dispose();
					this.rolloverTimer = null;
				}
				catch(Exception e)
				{
                    pluginInstance.LogError(string.Format("Error stopping rollover timer: {0}", e.Message));
				}
			}
		}

		protected void StartPublishTimer()
		{

			int thirtySeconds = 1000 * 30;

			this.publishTimer = new Timer((g) => {

				BaseDocument document;
                IBulkRequest request = new BulkRequest("active-homeseer-index")
                {
                    Operations = new List<IBulkOperation>()
                };

                while (!this.cache.IsEmpty)
				{
					if(this.cache.TryTake(out document))
					{
						request.Operations.Add(new BulkIndexOperation<BaseDocument>(document));
					}
				}

				if(request.Operations.Count == 0)
				{
                    pluginInstance.LogDebug("WriteToCluster: no documents to write");
					this.publishTimer.Change(thirtySeconds, Timeout.Infinite);
					return;
				}

				try
				{
                    pluginInstance.LogDebug(string.Format("Writing {0} documents to Elasticsearch...", request.Operations.Count));
				ElasticClient client = GetESClient(Config);
					IBulkResponse r = client.Bulk(request);
					if(r.IsValid)
					{
                        pluginInstance.LogDebug("Document publish Successful!");
					}
					else
					{
                        pluginInstance.LogError(string.Format("Document published failed: {0}", r.DebugInformation));
					}
				}
				catch(Exception e)
				{
                    pluginInstance.LogError(string.Format("Error writing documents to Elasticsearch: {0}", e.Message));
				}
				this.publishTimer.Change(thirtySeconds, Timeout.Infinite);
			}, null, 0, Timeout.Infinite);
		}

		protected void StopPublishTimer()
		{
			if(this.publishTimer != null)
			{
				try
				{
					this.publishTimer.Dispose();
					this.publishTimer = null;
				}
				catch(Exception e)
				{
                    pluginInstance.LogError(string.Format("Error stopping publish timer: {0}", e.Message));
				}
			}
		}

		protected static ElasticClient GetESClient(PluginConfig pluginConfig)
		{
			using(ConnectionSettings connection = new Nest.ConnectionSettings(new Uri(pluginConfig.ElasticsearchUrl)))
			{
				if(pluginConfig.SecurityType == "basic" && pluginConfig.Username != null && pluginConfig.Password != null)
				{
					connection.BasicAuthentication(pluginConfig.Username, pluginConfig.Password);
				}

				connection.DefaultIndex("active-homeseer-index");

				ElasticClient client = new Nest.ElasticClient(connection);
				return client;
			}
		}

		#region IDisposable
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "pluginConfig")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "rolloverTimer")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "publishTimer")]
		public void Dispose()
		{
			TryDispose(this.publishTimer);
			TryDispose(this.rolloverTimer);
		}

		private static void TryDispose(IDisposable disposable)
		{
			if(disposable != null)
			{
				try
				{
					disposable.Dispose();
					disposable = null;
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(string.Format("Error disposing of entity: {0}", e.Message));
				}
			}
		}
		#endregion
	}
}
