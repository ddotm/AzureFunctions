using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Models.Tempo
{
    public class ShowXref
    {
        public int Id { get; set; }
        public int GCPShowId { get; set; }
        public string AlternateId { get; set; }
        public string AGEventId { get; set; }
        public string Note { get; set; }
    }
}
