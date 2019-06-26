using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.Tempo
{
    public class VenueXref
    {
        public int Id { get; set; }
        public int GCPVenueId { get; set; }
        public string AlternateId { get; set; }
        public string AGVenueId { get; set; }
        public string Note { get; set; }
    }
}
