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
}







