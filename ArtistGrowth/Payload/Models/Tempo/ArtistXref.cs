using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.Tempo
{
    public class ArtistXref
    {
        public int ArtistXrefId { get; set; }
        public int GCPSourceSystemId { get; set; }
        public int GCPSourceId { get; set; }
        public int GCPArtistId { get; set; }
        public string AGArtistId { get; set; }
        public string AGCalendarId { get; set; }
        public DateTime? InitialLoadDate { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public string Statuses { get; set; }
        public DateTime? ShowStartDate { get; set; }
        public bool IncludeFinancials { get; set; }
        public string RequestedBy { get; set; }
        public DateTime? RequestedOn { get; set; }
        public string Note { get; set; }
        public string ArtistName { get; set; }
    }
}
