using Payload.Models.Tempo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.GCP
{
    public class GCPItinerary
    {
        public ArtistXref Artist;
        public List<GCPShow> Shows;
        public List<GCPVenue> Venues;
    }
}
