using Payload.Models.GCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.ArtistGrowth
{

    public class AGVenueList
    {
        public List<AGVenue> results { get; set; }
    }
    public class AGVenue
    {
        public string pk { get; set; }
        public string url { get; set; }
        public string uuid { get; set; }
        public string date_created { get; set; }
        public string date_modified { get; set; }
        public string name { get; set; }
        public string alternate_id { get; set; }
        public string source { get; set; } = "vnd-paradigm";
        public string formatted_address { get; set; }
        public string address_line_1 { get; set; }
        public string address_line_2 { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public string postal_code { get; set; }
        public string country { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string tz { get; set; }
        public string phone { get; set; }
        public string website { get; set; }
        public string contact_name { get; set; }
        public string venue_type { get; set; }
        public string capacity { get; set; }
    }
}
