using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.GCP
{
    public class GCPShow
    {
        public int id { get; set; }
        public int artistId { get; set; }
        public int currencyId { get; set; }
        public int venueId { get; set; }
        public string status { get; set; }
        public double fee { get; set; }
        public DateTime dateUtc { get; set; }
        public DateTime updatedAt { get; set; }

        public string alternateId { get; set; }
        public string AGEvent_pk { get; set; }
        public string ShowName { get; set; }
        public string TranslateStatus()
        {
            switch (this.status.ToUpper())
            {
                case "CONFIRMED":
                case "CONTRACT":
                case "CONTRACT ISSUED":
                case "FINALS":
                case "PERFORMED":
                    return "CONFIRMED";
                case "DEALMEMO":
                case "OFFER":
                case "PENDING":
                    return null; // "TENTATIVE";
                case "CANCELLED":
                case "REJECTED":
                    return "CANCELLED";
                default:
                    return null;
            }
        }

        public string AGCalendar_pk { get; set; }
        public string ArtistName { get; set; }

        public string AGLocation_pk { get; set; }
        public string VenueName { get; set; }

        public string ActionTaken { get; set; }
        public bool IncludeFinancials { get; set; }
    }
}
