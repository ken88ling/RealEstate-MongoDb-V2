﻿namespace RealEstate.Rentals
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson;
	using MongoDB.Bson.Serialization.Attributes;

    [BsonIgnoreExtraElements]
    public class ZipCode
    {
        [BsonId]
        public string Id { get; set; }
        [BsonElement("city")]
        public string City { get; set; }
        [BsonElement("state")]
        public string State { get; set; }

    }
    public class Rental
	{
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }

		public string Description { get; set; }
		public int NumberOfRooms { get; set; }
		public List<string> Address = new List<string>();
        public string ZipCode { get; set; }


        [BsonRepresentation(BsonType.Double)]
		public decimal Price { get; set; }

		public string ImageId { get; set; }

		public List<PriceAdjustment> Adjustments = new List<PriceAdjustment>();

		public Rental()
		{
		}

		public Rental(PostRental postRental)
		{
			Description = postRental.Description;
			NumberOfRooms = postRental.NumberOfRooms;
			Price = postRental.Price;
			Address = (postRental.Address ?? string.Empty).Split('\n').ToList();
		    ZipCode = postRental.ZipCode;
		}


	    public void AdjustPrice(AdjustPrice adjustPrice)
		{
			var adjustment = new PriceAdjustment(adjustPrice, Price);
			Adjustments.Add(adjustment);
			Price = adjustPrice.NewPrice;
		}

		public bool HasImage()
		{
			return !String.IsNullOrWhiteSpace(ImageId);
		}
	}
}