using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace RealEstate.Rentals
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using App_Start;
    using MongoDB.Bson;
    using MongoDB.Driver.Builders;
    using MongoDB.Driver.GridFS;
    using MongoDB.Driver.Linq;

    public class RentalsController : Controller
    {

        public readonly RealEstateContextNewApis ContextNew = new RealEstateContextNewApis();
        public readonly RealEstateContext Context = new RealEstateContext();

        public async Task<ActionResult> Index(RentalsFilter filters)
        {
            var rentals = await FilterRentals(filters)
                .Select(r => new RentalViewModel()
                {
                    Id = r.Id,
                    Address = r.Address,
                    Description = r.Description,
                    NumberOfRooms = r.NumberOfRooms,
                    Price = r.Price
                })
                .OrderBy(r => r.Price)
                .ThenByDescending(r => r.NumberOfRooms)
                .ToListAsync();

            var model = new RentalsList
            {
                Rentals = rentals,
                Filters = filters
            };
            return View(model);
        }

        private IMongoQueryable<Rental> FilterRentals(RentalsFilter filters)
        {
            IMongoQueryable<Rental> rentals = ContextNew.Rentals.AsQueryable();

            if (filters.MinimumRooms.HasValue)
            {
                rentals = rentals
                    .Where(r => r.NumberOfRooms >= filters.MinimumRooms);
            }

            if (filters.PriceLimit.HasValue)
            {
                rentals = rentals
                    .Where(r => r.Price <= filters.PriceLimit);
            }
            return rentals;
        }

        public ActionResult Post()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Post(PostRental postRental)
        {
            var rental = new Rental(postRental);
            // Context.Rentals.Insert(rental); old api
            await ContextNew.Rentals.InsertOneAsync(rental);
            return RedirectToAction("Index");
        }

        public ActionResult AdjustPrice(string id)
        {
            var rental = GetRental(id);
            return View(rental);
        }

        private Rental GetRental(string id)
        {
            //var rental = Context.Rentals.FindOneById(new ObjectId(id));// old api
            var rental = ContextNew.Rentals
                .Find(r => r.Id == id).FirstOrDefault();

            return rental;
        }


        //[HttpPost] //Replace document
        //public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
        //{
        //    var rental = GetRental(id);
        //    rental.AdjustPrice(adjustPrice);
        //    //Context.Rentals.Save(rental);
        //    await ContextNew.Rentals.ReplaceOneAsync(r => r.Id == id, rental);
        //    return RedirectToAction("Index");
        //}

        //[HttpPost] //update document
        //public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
        //{
        //    var rental = GetRental(id);
        //    var adjustment = new PriceAdjustment(adjustPrice, rental.Price);
        //    var modificationUpdate = Builders<Rental>.Update
        //        .Push(r => r.Adjustments, adjustment)
        //        .Set(r => r.Price, adjustPrice.NewPrice);
        //    //Context.Rentals.Update(Query.EQ("_id", new ObjectId(id)), modificationUpdate);
        //    await ContextNew.Rentals.UpdateOneAsync(r => r.Id == id, modificationUpdate);
        //    return RedirectToAction("Index");
        //}

        [HttpPost]
        public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
        {
            var rental = GetRental(id);
            rental.AdjustPrice(adjustPrice);
            //Context.Rentals.Save(rental);

            UpdateOptions options = new UpdateOptions
            {
                IsUpsert = true
            };

            await ContextNew.Rentals.ReplaceOneAsync(r => r.Id == id, rental, options);
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Delete(string id)
        {
            //Context.Rentals.Remove(Query.EQ("_id", new ObjectId(id))); old api
            await ContextNew.Rentals.DeleteOneAsync(r => r.Id == id);
            return RedirectToAction("Index");
        }

        public string PriceDistribution()
        {
            //use aggregate framework
            return new QueryPriceDistribution()
                //.RunAggregationFluent(ContextNew.Rentals) //new api v2
                .RunLinq(ContextNew.Rentals) // run Linq
                                             //.Run(Context.Rentals) // obsolete api
                .ToJson();
        }

        public ActionResult AttachImage(string id)
        {
            var rental = GetRental(id);
            return View(rental);
        }

        [HttpPost]
        public async Task<ActionResult> AttachImage(string id, HttpPostedFileBase file)
        {
            var rental = GetRental(id);
            if (rental.HasImage())
            {
                DeleteImage(rental);
            }
            await StoreImageAsync(file, id);
            return RedirectToAction("Index");
        }

        private void DeleteImage(Rental rental)
        {
            Context.Database.GridFS.DeleteById(new ObjectId(rental.ImageId));
            SetRentalImageId(rental.Id, null);
        }

        private async Task StoreImageAsync(HttpPostedFileBase file, string rentalId)
        {
            //var bucket = new GridFSBucket(ContextNew.Database);
            GridFSUploadOptions options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument("contentType",file.ContentType)
            };
            var imageId = await ContextNew.ImagesBucket
                .UploadFromStreamAsync(file.FileName, file.InputStream,options);
            SetRentalImageId(rentalId, imageId.ToString());

        }

        private void SetRentalImageId(string rentalId, string imageId)
        {
            var rentalByid = Query<Rental>.Where(r => r.Id == rentalId);
            var setRentalImageId = Update<Rental>.Set(r => r.ImageId, imageId);
            Context.Rentals.Update(rentalByid, setRentalImageId);
        }

        public ActionResult GetImage(string id)
        {
            var stream = ContextNew.ImagesBucket.OpenDownloadStream(new ObjectId(id));
            var contentType = stream.FileInfo.ContentType 
                ?? stream.FileInfo.Metadata["contentType"].AsString;
            return File(stream, contentType);
        }

        public ActionResult JoinPreLookup()
        {
            var rentals = ContextNew.Rentals.Find(new BsonDocument()).ToList();
            var rentalZips = rentals.Select(r => r.ZipCode).Distinct().ToArray();

            var zipsById = ContextNew.Database.GetCollection<ZipCode>("zips")
                .Find(z => rentalZips.Contains(z.Id))
                .ToList()
                .ToDictionary(d => d.Id);

            var report = rentals
                .Select(r => new
                {
                    rental = r,
                    ZipCode = r.ZipCode != null && zipsById.ContainsKey(r.ZipCode)
                        ? zipsById[r.ZipCode] : null
                });

            return Content(report.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict }), "application/json");
        }

        public ActionResult JoinWithLookup()
        {
            var report = ContextNew.Rentals
                .Aggregate()
                .Lookup<Rental, ZipCode, RentalWithZipCodes>(ContextNew.Database.GetCollection<ZipCode>("zips"),
                    r => r.ZipCode,
                    z => z.Id,
                    w=>w.ZipCodes
                ).ToList();

            return Content(report.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict }), "application/json");
        }
    }

    public class RentalWithZipCodes : Rental
    {
        public ZipCode[] ZipCodes { get; set; }
    }
}

