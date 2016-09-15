using System.Diagnostics;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.GridFS;

namespace RealEstate.App_Start
{
    using MongoDB.Driver;
    using Properties;
    using Rentals;

  

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

            ImagesBucket = new GridFSBucket(Database);
        }

        public GridFSBucket ImagesBucket { get; private set; }

        public IMongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}








