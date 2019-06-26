using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Payload.Utilities;
using Payload.Workers;

namespace Payload
{
    public class FunctionalPayload
    {
        private readonly ILogger _log;
        private static TempoWorker tempoWorker;
        private static GCPWorker gcpWorker;

        public FunctionalPayload(ILogger log)
        {
            _log = log;
        }

        public async Task ExecuteAsync()
        {
            _log.LogInformation($"Artist Growth Azure Function with timer trigger (with CI/CD). Executed at: {DateTime.Now}");

            await Task.CompletedTask;
        }

        static async Task<bool> ProcessArtistGrowth()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // TEMPO: Get Cross Reference Data from Tempo
                Logger.WriteLine("Get Tempo Artist Xref List: ");
                tempoWorker = new TempoWorker();
                Logger.Write($"   {tempoWorker.ArtistXrefs.Count} ", ConsoleColor.Green);
                Logger.WriteLine("artist records retrieved");
                Logger.Write($"   {tempoWorker.VenueXrefs.Count} ", ConsoleColor.Green);
                Logger.WriteLine("venue records retrieved");

                //// GCP: Get Artists from GCP
                Logger.WriteLine("Get GCP Artists:");
                gcpWorker = new GCPWorker();
                var gcpArtists = gcpWorker.GetArtistList(tempoWorker.ArtistXrefs);
                Logger.Write($"  {gcpArtists.Count} ", ConsoleColor.Green);
                Logger.WriteLine("Artist records retrieved");

                // AG: Create AGWorker and Authenticate
                var agWorker = new AGWorker();
                Logger.Write("Authenticating with Artist Growth... ");
                await agWorker.Authenticate();
                Logger.WriteLine("done", ConsoleColor.Green);

                Logger.WriteLine("------------------------------------------");

                // update the GCP Artist record with the AG ID from the X-Ref
                foreach (var artist in tempoWorker.ArtistXrefs)
                {
                    // link the calendar id to the artist record
                    artist.ArtistName = gcpArtists.FirstOrDefault(i => i.Id == artist.GCPArtistId)?.Name;
                    Logger.Write($"{artist.ArtistName}", ConsoleColor.Blue);
                    Logger.WriteLine($"  [{artist.GCPArtistId}] - linked to calendar {artist.AGCalendarId}");

                    //get the gcp itinerary for the artist (venues and shows)
                    var itin = gcpWorker.GetItinerary(artist);
                    Logger.Write($"   {itin.Shows.Count()}", ConsoleColor.Green);
                    Logger.WriteLine($" shows found");
                    Logger.Write($"   {itin.Venues.Count()}", ConsoleColor.Green);
                    Logger.WriteLine($" distinct venues found");

                    // IF there are shows that need to updated/inserted, then continue
                    if (itin.Shows.Any())
                    {
                        //link the venue's AG id from the xref table
                        foreach (var itinVenue in itin.Venues)
                        {
                            var venue = tempoWorker.VenueXrefs.FirstOrDefault(i => i.GCPVenueId == itinVenue.id);
                            itinVenue.AGVenueId = venue?.AGVenueId;
                        }

                        // update existing venues in AG (if needed)
                        Logger.Write($"   Updating venues in Artist Growth... ");
                        var linkedVenues = itin.Venues.Where(w => w.AGVenueId != null).ToList();
                        await agWorker.SyncVenues(linkedVenues);
                        var c = linkedVenues.Count(w => w.ActionTaken.StartsWith("Updated"));
                        Logger.Write($"{c} ", ConsoleColor.Red);
                        Logger.WriteLine("venues updated done");

                        // find any unlinked venues and sync with AG
                        var unlinkedVenues = itin.Venues.Where(w => w.AGVenueId == null).ToList();
                        if (unlinkedVenues.Any())
                        {
                            //sync the missing unlinked venues with AG and update the xref table
                            Logger.Write($"   Inserting ");
                            Logger.Write($"{unlinkedVenues.Count}", ConsoleColor.Green);
                            Logger.Write($" new venue records into Artist Growth... ");
                            var m = await agWorker.SyncVenues(unlinkedVenues);
                            Logger.WriteLine("done", ConsoleColor.Green);

                            // log any AG errors
                            foreach (var venue in unlinkedVenues)
                            {
                                if (venue.ActionTaken.StartsWith("Insert Failed"))
                                {
                                    Logger.Write($"      {venue.name} [{venue.alternateId}]: ");
                                    Logger.WriteLine($" {venue.ActionTaken} ", ConsoleColor.Red);
                                    tempoWorker.WriteLog("Detail", $"{venue.name} [{venue.alternateId}]: {venue.ActionTaken}", DateTime.UtcNow);
                                }
                            }

                            // write the new xrefs to the table for future syncs
                            Logger.Write($"   Updating Venue XRef table in Tempo ");
                            tempoWorker.AddVenueXref(unlinkedVenues);
                            Logger.WriteLine("done", ConsoleColor.Green);
                        }

                        // populate artist and venue data in the show
                        foreach (var show in itin.Shows)
                        {
                            show.AGCalendar_pk = artist.AGCalendarId;
                            show.ArtistName = artist.ArtistName;
                            var venue = itin.Venues.FirstOrDefault(w => w.id == show.venueId);
                            if (venue != null)
                            {
                                show.AGLocation_pk = venue.AGVenueId;
                                show.VenueName = venue.name;
                            }
                        }

                        //at this point we hopefully have all the venues related to the show for this client in AG
                        //  so we should be able to update/insert all the shows for the client now
                        Logger.Write($"   Syncing ");
                        Logger.Write($"{itin.Shows.Count} ", ConsoleColor.Green);
                        Logger.Write($"unique show records with Artist Growth... ");
                        var n = await agWorker.SyncEvents(itin.Shows);
                        Logger.WriteLine("done", ConsoleColor.Green);

                        foreach (var show in itin.Shows)
                        {
                            if (show.ActionTaken.StartsWith("Show Skipped") || show.ActionTaken.StartsWith("Insert Failed"))
                            {
                                Logger.Write($"      {show.ShowName} [{show.alternateId}]: ");
                                Logger.WriteLine($" {show.ActionTaken} ", ConsoleColor.Red);
                                tempoWorker.WriteLog("Detail", $"{show.ShowName} [{show.alternateId}]: {show.ActionTaken}", DateTime.UtcNow);
                            }
                        }

                        tempoWorker.AddShowXref(itin.Shows);

                        //update the artist xref with new dates
                        tempoWorker.UpdateArtistXref(itin.Artist);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine($"***ERROR: {e.Message}");
            }
            finally
            {
                Logger.Write($"Writing Summary Logger... ");
                var logData = Logger.sbLog.ToString();
                tempoWorker.WriteLog("Summary", logData, startTime);
                tempoWorker.CloseConnection();
                Logger.WriteLine("done", ConsoleColor.Green);
            }

            return true;
        }

        static async Task<bool> UpdateAlternateIdsInArtistGrowth()
        {
            //// Tempo: Get Artist Xref
            Logger.WriteLine(":::Update Artist Growth alternateIds:::");
            Logger.WriteLine("Get Tempo Artist Xref List: ");
            tempoWorker = new TempoWorker();
            Logger.Write($"   {tempoWorker.ArtistXrefs.Count} ", ConsoleColor.Green);
            Logger.WriteLine("artist records retrieved");
            Logger.Write($"   {tempoWorker.VenueXrefs.Count} ", ConsoleColor.Green);
            Logger.WriteLine("venue records retrieved");

            //// GCP: Get Artists from GCP
            Logger.WriteLine("Get GCP Artists:");
            gcpWorker = new GCPWorker();
            var gcpArtists = gcpWorker.GetArtistList(tempoWorker.ArtistXrefs);
            Logger.Write($"  {gcpArtists.Count} ", ConsoleColor.Green);
            Logger.WriteLine("Artist records retrieved");

            //// AG: Create AGWorker and Authenticate
            var agWorker = new AGWorker();
            Logger.Write("Authenticating with Artist Growth... ");
            await agWorker.Authenticate();
            Logger.WriteLine("done", ConsoleColor.Green);

            Logger.WriteLine("------------------------------------------");


            //get all AG venues
            var agVenues = await agWorker.GetAllVenues();

            //get all GCP venues using the alternateId's from AG
            var agvIds = agVenues.Select(s => s.alternate_id).ToList();
            var gcpVenues = gcpWorker.GetVenueList(agvIds);

            // loop through AG venues
            //      find GCP venue
            //      update AG venue with new alternateId
            foreach (var venue in agVenues)
            {

                var altIdString = venue.alternate_id;
                if (altIdString.Contains("."))
                {
                    continue;
                }

                var altId = int.Parse(altIdString);
                var gcpv = gcpVenues.FirstOrDefault(w => w.id == altId);
                if (gcpv != null)
                {
                    gcpv.AGVenueId = venue.pk;
                    Logger.WriteLine($"Update Venue: {venue.name} ::::: {venue.alternate_id} => {gcpv.alternateId}");
                    try
                    {
                        var x = await agWorker.UpdateVenue(gcpv, false);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine($"Update Show: {venue.name} ::: {e.Message}", ConsoleColor.Red);
                    }
                }
            }

            //get all AG events
            var agShows = await agWorker.GetAllEvents();

            //get all GCP shows/events using the alternateId's from AG
            var agsIds = agShows.Select(s => s.alternate_id).ToList();
            var gcpShows = gcpWorker.GetShowList(agsIds);

            // loop through AG events
            //      find GCP show
            //      if GCP event is found
            //          update AG event with new alternateId and new Title using just the venue name
            //      if GCP show is NOT found
            //          update AG even with status = CANCELED
            var venuesNotFound = 0;
            foreach (var show in agShows)
            {
                var altIdString = show.alternate_id;
                if (altIdString.Contains("."))
                {
                    continue;
                }

                var altId = int.Parse(altIdString);
                var gcps = gcpShows.FirstOrDefault(w => w.id == altId);
                if (gcps != null)
                {
                    var venue = gcpVenues.FirstOrDefault(w => w.id == gcps.venueId);
                    if (venue != null)
                    {
                        gcps.VenueName = venue.name;
                        gcps.AGEvent_pk = show.pk;
                        gcps.AGLocation_pk = show.location;
                        gcps.AGCalendar_pk = show.calendar;
                    }
                    else
                    {
                        gcps.VenueName = show.name;
                        gcps.AGEvent_pk = show.pk;
                        gcps.AGLocation_pk = show.location;
                        gcps.AGCalendar_pk = show.calendar;
                        venuesNotFound++;
                    }

                    Logger.WriteLine($"Update Show: {show.name} ::::: {show.alternate_id} => {gcps.alternateId}");
                    Logger.WriteLine($"     {show.name} => {gcps.VenueName}");
                }
                else
                {
                    Logger.WriteLine($"Update Show: {show.name}", ConsoleColor.Red);
                    Logger.WriteLine($"     show not found in GCP, update status to CANCELED");
                    show.status = "CANCELED";
                }

                var x = await agWorker.UpdateEvent(gcps, false);
            }

            Logger.WriteLine($"{agShows.Count - venuesNotFound} shows found for name update", ConsoleColor.Green);
            if (venuesNotFound > 0)
            {
                Logger.WriteLine($"{venuesNotFound} shows not found for name update", ConsoleColor.Red);
            }

            return true;
        }

        static async Task<bool> GetAlternateIdsInArtistGrowth()
        {
            //// Tempo: Get Artist Xref
            Logger.WriteLine(":::Update Artist Growth alternateIds:::");
            Logger.WriteLine("Get Tempo Artist Xref List: ");
            tempoWorker = new TempoWorker();
            Logger.Write($"   {tempoWorker.ArtistXrefs.Count} ", ConsoleColor.Green);
            Logger.WriteLine("artist records retrieved");
            Logger.Write($"   {tempoWorker.VenueXrefs.Count} ", ConsoleColor.Green);
            Logger.WriteLine("venue records retrieved");

            //// GCP: Get Artists from GCP
            Logger.WriteLine("Get GCP Artists:");
            gcpWorker = new GCPWorker();
            var gcpArtists = gcpWorker.GetArtistList(tempoWorker.ArtistXrefs);
            Logger.Write($"  {gcpArtists.Count} ", ConsoleColor.Green);
            Logger.WriteLine("Artist records retrieved");

            //// AG: Create AGWorker and Authenticate
            var agWorker = new AGWorker();
            Logger.Write("Authenticating with Artist Growth... ");
            await agWorker.Authenticate();
            Logger.WriteLine("done", ConsoleColor.Green);

            Logger.WriteLine("------------------------------------------", ConsoleColor.Blue);


            //get all AG venues
            var agVenues = await agWorker.GetAllVenues();
            foreach (var venue in agVenues)
            {
                if (venue.alternate_id.Contains("."))
                {
                    Logger.WriteLine($"{venue.alternate_id}:::::{venue.name}");
                }
                else
                {
                    Logger.WriteLine($"{venue.alternate_id}:::::{venue.name}", ConsoleColor.Red);
                }
            }

            Logger.WriteLine("------------------------------------------", ConsoleColor.Blue);

            //get all AG events
            var agShows = await agWorker.GetAllEvents();
            foreach (var show in agShows)
            {
                if (show.alternate_id.Contains("."))
                {
                    Logger.WriteLine($"{show.alternate_id}:::::{show.name}:::::{show.status}");
                }
                else
                {
                    Logger.WriteLine($"{show.alternate_id}:::::{show.name}:::::{show.status}", ConsoleColor.Red);
                }
            }

            return true;
        }
    }
}