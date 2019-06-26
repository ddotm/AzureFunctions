using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.ArtistGrowth
{

    public class AGEventList
    {
        public List<AGEvent> results { get; set; }
    }

    public class AGEvent
    {
        public AGEvent()
        {
            this.finance = new EventFinance();
        }

        public string pk { get; set; }
        public string alternate_id { get; set; }
        public string name { get; set; }
        public string source { get; set; } = "vnd-paradigm";
        public string calendar { get; set; }
        public bool is_all_day { get; set; } = true;
        public int duration_minutes { get; set; } = 120;
        public string start_date { get; set; }
        public string event_type { get; set; } = "PF";
        public string status { get; set; }
        public string location { get; set; }

        public EventFinance finance { get; set; }

    }

    public class EventFinance
    {
        public string guarantee { get; set; }
        public string promoter_credit { get; set; }
        public string management_credit { get; set; }
        public string pro_reimbursement { get; set; }
        public string total_deposit { get; set; }
        public string walk_out_potential { get; set; }
        public string engagement_notes { get; set; }
    }
}
