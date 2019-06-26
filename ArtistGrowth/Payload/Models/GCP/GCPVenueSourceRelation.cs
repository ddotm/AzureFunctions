using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.GCP
{
    public class GCPVenueSourceRelation
    {
        public int VenueId { get; set; }
        public int ForeignId { get; set; }
        public int SystemId { get; set; }
        public string OriginalTable { get; set; }
    }
}
