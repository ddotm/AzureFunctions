using Payload.Models;
using Payload.Models.GCP;
using Payload.Models.Tempo;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Payload.Utilities;

namespace Payload.Workers
{
    public class GCPWorker : IDisposable
    {
        public List<GCPArtist> Artists;
        public List<GCPShow> Shows;
        public List<GCPVenue> Venues;

        IDbConnection dbCon;

        public GCPWorker()
        {
            Logger.Write("Open connection to GCP... ");
            var conString = "server=52.54.92.183;user id=ParadigmUser;password=aRXeqkp_!KGRWwX5h5&d;persistsecurityinfo=True;database=phase1";
            dbCon = new MySql.Data.MySqlClient.MySqlConnection(conString);
            dbCon.Open();
            dbCon.ChangeDatabase("phase1");
            Logger.WriteLine("done", ConsoleColor.Green);
        }

        public List<GCPArtist> GetArtistList(List<ArtistXref> xrefArtists)
        {
            Logger.WriteLine("Get GCP Artists:");

            var errors = ValidateArtistXRef(xrefArtists);

            if (string.IsNullOrWhiteSpace(errors) == false)
            {
                throw new Exception(errors);
            }

            var ids = xrefArtists.Select(i => i.GCPArtistId).ToList();
            var artistIds = string.Join(",", ids);
            var temp = $"SELECT * FROM Artist Where `id` in ({artistIds})";
            var x = dbCon.QueryMultiple(temp);
            var result = x.Read<GCPArtist>().ToList();

            Logger.WriteLine($"  {result.Count} ", ConsoleColor.Green, "artist records retrieved");

            return result;
        }

        public List<GCPVenue> GetVenueList(List<ArtistXref> xrefArtists)
        {
            Logger.WriteLine("Get GCP Venues:");

            var ids = xrefArtists.Select(i => i.GCPArtistId).ToList();
            var idList = string.Join(",", ids);
            var temp = $"SELECT DISTINCT `venueId` FROM `Show` Where `artistId` in ({idList}) AND `venueId` IS NOT null";
            ids = dbCon.Query<int>(temp).ToList();

            idList = string.Join(",", ids);

            var query = new StringBuilder("SELECT DISTINCT v.`id`, v.`name`, v.countryId, lu.code AS country, vc.contactId, c.address, ");
            query.Append("v.capacity, v.lat, v.long, v.timezone, v.createdAt, v.updatedAt");
            query.Append(" FROM Venue v");
            query.Append(" INNER JOIN VenueContact vc ON v.id = vc.venueId");
            query.Append(" INNER JOIN Contact c ON vc.contactId = c.id");
            query.Append(" INNER JOIN Country lu ON v.countryId = lu.id");
            query.Append($" WHERE v.`id` in ({idList})");

            temp = query.ToString();
            var venues = dbCon.Query<GCPVenue>(temp).ToList();

            Logger.WriteLine($"  {venues.Count} ", ConsoleColor.Green, "venue records retrieved");

            return venues;
        }

        public GCPItinerary GetItinerary(ArtistXref xrefArtist)
        {
            var result = new GCPItinerary()
            {
                Artist = xrefArtist
            };

            //##### Get the shows based on criteria set in the xref table
            var query = new StringBuilder($"SELECT * FROM `Show` WHERE `artistId` = {xrefArtist.GCPArtistId}");
            if (xrefArtist.ShowStartDate.HasValue)
            {
                query.Append($" AND `dateUtc` >= '{xrefArtist.ShowStartDate.Value.ToString("yyyy-MM-dd")}'");
            }

            if (xrefArtist.LastSyncDate.HasValue)
            {
                query.Append($" AND `updatedAt` >= '{xrefArtist.LastSyncDate.Value.ToString("yyyy-MM-dd")}'");
            }

            if (string.IsNullOrWhiteSpace(xrefArtist.Statuses) == false)
            {
                var temp = $"'{xrefArtist.Statuses.ToUpper().Replace(" ", "").Replace(",", "','")}'";
                query.Append($" AND `status` IN ({temp})");
            }

            var qry = query.ToString();
            result.Shows = dbCon.Query<GCPShow>(qry).ToList();

            //update the IncludeFinancials bit for each show
            result.Shows.ForEach(f => f.IncludeFinancials = xrefArtist.IncludeFinancials);

            //if there any shows to update/insert, get the associated venues
            if (result.Shows.Any())
            {
                var ids = result.Shows.Select(s => s.venueId).Distinct();
                var venueIds = string.Join(",", ids);

                //##### now lets get a list of venues for the shows we just got!
                query.Clear();
                query.Append("SELECT DISTINCT v.`id`, v.`name`, v.countryId, lu.code AS country, vc.contactId, c.address, ");
                query.Append("v.capacity, v.lat, v.long, v.timezone, v.createdAt, v.updatedAt, c.`name` AS venueContact");
                query.Append(" FROM Venue v");
                query.Append(" LEFT JOIN VenueContact vc ON v.id = vc.venueId");
                query.Append(" LEFT JOIN Contact c ON vc.contactId = c.id");
                query.Append(" LEFT JOIN Country lu ON v.countryId = lu.id");
                query.Append($" WHERE v.`id` in ({venueIds}) AND vc.type = 'venue'");

                qry = query.ToString();
                result.Venues = dbCon.Query<GCPVenue>(qry).ToList();
            }
            else
            {
                result.Venues = new List<GCPVenue>();
            }

            MapAlternateIds(result);

            Logger.WriteLine($"   {Shows.Count()}", ConsoleColor.Green, " shows found");
            Logger.WriteLine($"   {Venues.Count()}", ConsoleColor.Green, " distinct venues found");

            return result;
        }

        public List<GCPVenue> GetVenueList(List<string> idList)
        {
            var ids = string.Join(",", idList);

            var queryString = new StringBuilder("SELECT DISTINCT v.`id`, v.`name`, v.countryId, lu.code AS country, vc.contactId, c.address, ");
            queryString.Append("v.capacity, v.lat, v.long, v.timezone, v.createdAt, v.updatedAt");
            queryString.Append(" FROM Venue v");
            queryString.Append(" INNER JOIN VenueContact vc ON v.id = vc.venueId");
            queryString.Append(" INNER JOIN Contact c ON vc.contactId = c.id");
            queryString.Append(" INNER JOIN Country lu ON v.countryId = lu.id");
            queryString.Append($" WHERE v.`id` in ({ids})");

            var temp = queryString.ToString();
            var venues = dbCon.Query<GCPVenue>(temp).ToList();

            MapAlternateIds(null, venues);

            return venues;
        }

        public List<GCPShow> GetShowList(List<string> idList)
        {
            var ids = string.Join(",", idList);

            var queryString = new StringBuilder($"SELECT * FROM `Show` WHERE `Id`  in ({ids})");

            var temp = queryString.ToString();
            var shows = dbCon.Query<GCPShow>(temp).ToList();


            MapAlternateIds(shows, null);
            return shows;
        }

        public void CloseConnection()
        {
            if (dbCon.State != ConnectionState.Closed) dbCon.Close();
        }

        public void Dispose()
        {
            CloseConnection();
        }

        private void MapAlternateIds(GCPItinerary itinerary)
        {
            MapAlternateIds(itinerary.Shows, itinerary.Venues);
        }

        private void MapAlternateIds(List<GCPShow> shows, List<GCPVenue> venues)
        {
            if (shows != null && shows.Any())
            {
                var showIdList = GetShowSourceRelations(shows);
                foreach (var show in shows)
                {
                    var ssrMatch = showIdList.FirstOrDefault(w => w.ShowId == show.id);
                    show.alternateId = ssrMatch == null ? $"no match for {show.id}" : $"{ssrMatch.ForeignId}.{ssrMatch.SystemId}";
                }
            }

            if (venues != null && venues.Any())
            {
                var venueIdList = GetVenueSourceRelations(venues);
                foreach (var venue in venues)
                {
                    var srMatch = venueIdList.FirstOrDefault(w => w.VenueId == venue.id);
                    venue.alternateId = srMatch == null ? $"no match for {venue.id}" : $"{srMatch.ForeignId}.{srMatch.SystemId}";
                }
            }
        }

        private List<GCPShowSourceRelation> GetShowSourceRelations(List<GCPShow> shows)
        {
            var showIdList = shows.Select(s => s.id).Distinct();
            var showIds = string.Join(",", showIdList);
            var temp1 = $"SELECT * FROM ShowSourceRelation Where `showId` in ({showIds})";
            var x = dbCon.QueryMultiple(temp1);
            return x.Read<GCPShowSourceRelation>().ToList();
        }

        private List<GCPVenueSourceRelation> GetVenueSourceRelations(List<GCPVenue> venues)
        {
            var venueIdList = venues.Select(s => s.id).Distinct();
            var venueIds = string.Join(",", venueIdList);
            var temp2 = $"SELECT * FROM VenueSourceRelation Where `venueId` in ({venueIds})";
            var y = dbCon.QueryMultiple(temp2);
            return y.Read<GCPVenueSourceRelation>().ToList();
        }

        private string ValidateArtistXRef(List<ArtistXref> xrefArtists)
        {
            var artistMismatchErrors = new StringBuilder();

            var sql = "SELECT * FROM ArtistSourceRelation WHERE ";
            var sqlOrClause = new StringBuilder();
            foreach (var artist in xrefArtists)
            {
                if (sqlOrClause.Length != 0)
                {
                    sqlOrClause.Append(" OR ");
                }

                sqlOrClause.Append($"(systemId = {artist.GCPSourceSystemId} AND foreignId = {artist.GCPSourceId})");
            }

            sql += sqlOrClause.ToString();
            var r = dbCon.QueryMultiple(sql);
            var asrItems = r.Read<GCPArtistSourceRelation>().ToList();

            //now we need to compare the artistId in the results to the GCPArtistId from xrefArtists
            foreach (var artist in xrefArtists)
            {
                var match = asrItems.FirstOrDefault(w => w.ForeignId == artist.GCPSourceId && w.SystemId == artist.GCPSourceSystemId);
                if (match == null)
                {
                    artistMismatchErrors.AppendLine($"Artist not found in GCP: {artist.Note} -- (SSId: {artist.GCPSourceSystemId}, SId: {artist.GCPSourceId}), AId: {artist.GCPArtistId}");
                }
                else
                {
                    if (match.ArtistId != artist.GCPArtistId)
                    {
                        artistMismatchErrors.AppendLine($"Artist Id mismatch: {artist.Note} -- gcp.artistId: {match.ArtistId} - xref.GCPArtistId: {artist.GCPArtistId} (SSId: {artist.GCPSourceSystemId}, SId: {artist.GCPSourceId}), AId: {artist.GCPArtistId}");
                    }
                }
            }

            return artistMismatchErrors.ToString();
        }
    }
}