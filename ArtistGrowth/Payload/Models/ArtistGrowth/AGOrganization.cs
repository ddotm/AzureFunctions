using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.ArtistGrowth
{
    public class AGOrganization
    {
        public string pk { get; set; }
        public string name { get; set; }
        public string calendaPk { get; set; }
        public calendar calendar { get; set; }
    }
    public class calendar
    {
        public string pk { get; set; }
    }

    public class AGOrganizationList
    {
        public List<AGOrganization> results { get; set; }
    }
}
