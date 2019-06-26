using Payload.Models.GCP;
using Payload.Models.Tempo;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payload.Workers
{
    public class TempoWorker : IDisposable
    {
        public List<ArtistXref> ArtistXrefs;
        public List<VenueXref> VenueXrefs;
        public List<ShowXref> ShowXrefs;

        private string _syncId = Guid.NewGuid().ToString();
        private SqlConnection db;

        public TempoWorker()
        {
//            // dev credentials
//            var conString = "Server=tempodev.database.windows.net;Database=ContactDev;User ID=TempoDevAdmin;Password=tb9$8oN5T6w79N9g;Trusted_Connection=False;Connection Timeout=120";

            // stage credentials
            var conString = "Server=tempostage.database.windows.net;Database=ContactStage;User ID=TempoStageAdmin;Password=5U8mPnyG#F#J563j;Trusted_Connection=False;Connection Timeout=120";

            db = new SqlConnection(conString);
            db.Open();

            var x = db.QueryMultiple("SELECT * FROM artistgrowth.ArtistXref WHERE IsActive = 1");
            this.ArtistXrefs = x.Read<ArtistXref>().ToList();

            GetVenueXrefs();
            GetShowXrefs();
        }


        public bool AddVenueXref(List<GCPVenue> venues)
        {
            var sbQuery = new System.Text.StringBuilder();
            foreach (var venue in venues)
            {
                var xref = this.VenueXrefs.FirstOrDefault(w => w.GCPVenueId == venue.id);
                if ((xref == null) && (string.IsNullOrWhiteSpace(venue.AGVenueId) == false))
                {
                    sbQuery.AppendLine($"INSERT INTO artistgrowth.VenueXref (GCPVenueId, AlternateId, AGVenueId, Note) ");
                    var note = $"{venue.name}".Replace("'", "''");
                    sbQuery.AppendLine($"   VALUES ({venue.id}, '{venue.alternateId}', '{venue.AGVenueId}', 'AG Sync: {note}') ");
                    db.Execute(sbQuery.ToString());
                    sbQuery.Clear();
                }
            }

            //refresh the xref
            GetVenueXrefs();

            return true;
        }

        public bool AddShowXref(List<GCPShow> shows)
        {
            var sbQuery = new System.Text.StringBuilder();
            foreach (var show in shows)
            {
                var xref = this.ShowXrefs.FirstOrDefault(w => w.GCPShowId == show.id);
                if (xref == null && (string.IsNullOrWhiteSpace(show.AGEvent_pk) == false))
                {
                    sbQuery.AppendLine($"INSERT INTO artistgrowth.ShowXref (GCPShowId, AlternateId, AGEventId, Note) ");
                    var note = $"{show.ArtistName} @ {show.VenueName}".Replace("'","''");
                    sbQuery.AppendLine($"   VALUES ({show.id}, '{show.alternateId}', '{show.AGEvent_pk}', 'AG Sync: {note}') ");
                    db.Execute(sbQuery.ToString());
                    sbQuery.Clear();
                }
            }

            //refresh the xref
            GetShowXrefs();

            return true;
        }

        public bool UpdateArtistXref(ArtistXref artist)
        {
            var sql = new System.Text.StringBuilder($"UPDATE artistgrowth.ArtistXref SET LastSyncDate = '{DateTime.UtcNow}'");
            if (artist.InitialLoadDate.HasValue == false)
            {
                sql.Append($", InitialLoadDate = '{DateTime.UtcNow}'");
            }
            sql.Append($" WHERE GCPArtistId = '{artist.GCPArtistId}'");
            db.Execute(sql.ToString());

            return true;
        }

        public void WriteLog(string type, string text, DateTime started)
        {
            if (db.State != ConnectionState.Open) db.Open();

            var sql = $"INSERT INTO artistgrowth.SyncLog (SyncId, Type, Text, Started, Finished) VALUES ('{_syncId}', '{type}', '{text}', '{started}', '{DateTime.UtcNow}')";
            db.Execute(sql);
        }

        public void CloseConnection()
        {
            if (db.State != ConnectionState.Closed) db.Close();
        }

        private void GetVenueXrefs()
        {
            var y = db.QueryMultiple("SELECT * FROM artistgrowth.VenueXref");
            this.VenueXrefs = y.Read<VenueXref>().ToList();
        }

        private void GetShowXrefs()
        {
            var y = db.QueryMultiple("SELECT * FROM artistgrowth.ShowXref");
            this.ShowXrefs = y.Read<ShowXref>().ToList();
        }




        public void Dispose()
        {
            CloseConnection();
        }
    }
}
