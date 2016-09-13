using System.Diagnostics;
using MongoDB.Driver.Core.Configuration;

namespace RealEstate.App_Start
{
    using MongoDB.Driver;
    using Properties;
    using Rentals;

    public class RealEstateContext
	{
		public MongoDatabase Database;

		public RealEstateContext()
		{
			var client = new MongoClient(Settings.Default.RealEstateConnectionString);
			var server = client.GetServer();
			Database = server.GetDatabase(Settings.Default.RealEstateDatabaseName);
		}

		public MongoCollection<Rental> Rentals
		{
			get
			{
				return Database.GetCollection<Rental>("rentals");
			}
		}
	}

    public class RealEstateContextNewApis
    {
        public IMongoDatabase Database;

        public RealEstateContextNewApis()
        {
            var connectionString = Settings.Default.RealEstateConnectionString;
            var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            //settings.ClusterConfigurator = builder => builder.Subscribe(new Log4NetMongoEvents());//make the log history
            settings.ClusterConfigurator = builder =>
            {
                builder.Subscribe(new Log4NetMongoEvents());
                //builder.TraceWith(new TraceSource());
            };
            var client = new MongoClient(settings);
            Database = client.GetDatabase(Settings.Default.RealEstateDatabaseName);
        }

        public IMongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}








