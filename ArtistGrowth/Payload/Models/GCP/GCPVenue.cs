using Payload.Models.ArtistGrowth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.GCP
{
    public class GCPVenue
    {
        public int id { get; set; }
        public string name { get; set; }
        public string countryId { get; set; }
        public string country { get; set; }
        public int capacity { get; set; }
        public string latitude { get; set; }

        public string longitude { get; set; }
        public string address { get; set; }
        public string timezone { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string venueContact { get; set; }

        public string alternateId { get; set; }
        public string AGVenueId { get; set; }
        public string ActionTaken { get; set; }
        public int TranslateCapacity()
        {
            if (this.capacity <= 250)
            {
                //return "0 - 250";
                return 1;
            }
            else if (this.capacity <= 500)
            {
                //return "251 - 500";
                return 2;
            }
            else if (this.capacity <= 1000)
            {
                //return "501 - 1,000";
                return 3;
            }
            else if (this.capacity <= 5000)
            {
                //return "1,001 - 5,000";
                return 4;
            }
            else if (this.capacity <= 10000)
            {
                //return "5,001 - 10,000";
                return 5;
            }
            else
            {
                //return "10,001 and Above";
                return 6;
            }
        }
    }

    public class GCPVenueAddress
    {
        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zipCode { get; set; }
    }
}
