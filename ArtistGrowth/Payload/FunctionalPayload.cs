using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payload.Models.GCP;
using Payload.Utilities;
using Payload.Workers;

namespace Payload
{
    public class FunctionalPayload
    {
        private static ILogger _log;
        private static TempoWorker _tempoWorker;
        private static GCPWorker _gcpWorker;
        private static AGWorker _agWorker;
        private static List<GCPArtist> _gcpArtists;

        public FunctionalPayload(ILogger log)
        {
            _log = log;
        }

        public async Task ExecuteAsync()
        {
            _log.LogInformation($"Artist Growth Azure Function with timer trigger (with CI/CD). Executed at: {DateTime.Now}");

            var x = GetAlternateIdsInArtistGrowth();
            x.Wait();
            await Task.CompletedTask;
        }

        static async Task<bool> ProcessArtistGrowth()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // initialize Workers
                var b = await Initialize();

                // update the GCP Artist record with the AG ID from the X-Ref
                foreach (var artist in _tempoWorker.ArtistXrefs)
                {
                    // link the calendar id to the artist record
                    artist.ArtistName = _gcpArtists.FirstOrDefault(i => i.Id == artist.GCPArtistId)?.Name;
                    Logger.WriteLine($"{artist.ArtistName}", ConsoleColor.Blue, $"  [{artist.GCPArtistId}] - linked to calendar {artist.AGCalendarId}");

                    //get the gcp itinerary for the artist (venues and shows)
                    var itin = _gcpWorker.GetItinerary(artist);

                    // IF there are shows that need to updated/inserted, then continue
                    if (itin.Shows.Any())
                    {
                        //link the venue's AG id from the xref table
                        foreach (var itinVenue in itin.Venues)
                        {
                            var venue = _tempoWorker.VenueXrefs.FirstOrDefault(i => i.GCPVenueId == itinVenue.id);
                            itinVenue.AGVenueId = venue?.AGVenueId;
                        }

                        // update existing venues in AG (if needed)
                        Logger.Write($"   Updating venues in Artist Growth... ");
                        var linkedVenues = itin.Venues.Where(w => w.AGVenueId != null).ToList();
                        await _agWorker.SyncVenues(linkedVenues);
                        var c = linkedVenues.Count(w => w.ActionTaken.StartsWith("Updated"));
                        Logger.WriteLine($"{c} ", ConsoleColor.Red, "venues updated done");

                        // find any unlinked venues and sync with AG
                        var unlinkedVenues = itin.Venues.Where(w => w.AGVenueId == null).ToList();
                        if (unlinkedVenues.Any())
                        {
                            //sync the missing unlinked venues with AG and update the xref table
                            Logger.Write($"   Inserting ");
                            Logger.Write($"{unlinkedVenues.Count}", ConsoleColor.Green);
                            Logger.Write($" new venue records into Artist Growth... ");
                            var m = await _agWorker.SyncVenues(unlinkedVenues);
                            Logger.WriteLine("done", ConsoleColor.Green);

                            // log any AG errors
                            foreach (var venue in unlinkedVenues)
                            {
                                if (venue.ActionTaken.StartsWith("Insert Failed"))
                                {
                                    Logger.Write($"      {venue.name} [{venue.alternateId}]: ");
                                    Logger.WriteLine($" {venue.ActionTaken} ", ConsoleColor.Red);
                                    _tempoWorker.WriteLog("Detail", $"{venue.name} [{venue.alternateId}]: {venue.ActionTaken}", DateTime.UtcNow);
                                }
                            }

                            // write the new xrefs to the table for future syncs
                            Logger.Write($"   Updating Venue XRef table in Tempo ");
                            _tempoWorker.AddVenueXref(unlinkedVenues);
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
                        var n = await _agWorker.SyncEvents(itin.Shows);
                        Logger.WriteLine("done", ConsoleColor.Green);

                        foreach (var show in itin.Shows)
                        {
                            if (show.ActionTaken.StartsWith("Show Skipped") || show.ActionTaken.StartsWith("Insert Failed"))
                            {
                                Logger.Write($"      {show.ShowName} [{show.alternateId}]: ");
                                Logger.WriteLine($" {show.ActionTaken} ", ConsoleColor.Red);
                                _tempoWorker.WriteLog("Detail", $"{show.ShowName} [{show.alternateId}]: {show.ActionTaken}", DateTime.UtcNow);
                            }
                        }

                        _tempoWorker.AddShowXref(itin.Shows);

                        //update the artist xref with new dates
                        _tempoWorker.UpdateArtistXref(itin.Artist);
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
                _tempoWorker.WriteLog("Summary", logData, startTime);
                _tempoWorker.CloseConnection();
                Logger.WriteLine("done", ConsoleColor.Green);
            }

            return true;
        }

        static async Task<bool> UpdateAlternateIdsInArtistGrowth()
        {
            // initialize Workers
            var b = await Initialize();

            //get all AG venues
            var agVenues = await _agWorker.GetAllVenues();

            //get all GCP venues using the alternateId's from AG
            var agvIds = agVenues.Select(s => s.alternate_id).ToList();
            var gcpVenues = _gcpWorker.GetVenueList(agvIds);

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
                        var x = await _agWorker.UpdateVenue(gcpv, false);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine($"Update Show: {venue.name} ::: {e.Message}", ConsoleColor.Red);
                    }
                }
            }

            //get all AG events
            var agShows = await _agWorker.GetAllEvents();

            //get all GCP shows/events using the alternateId's from AG
            var agsIds = agShows.Select(s => s.alternate_id).ToList();
            var gcpShows = _gcpWorker.GetShowList(agsIds);

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

                var x = await _agWorker.UpdateEvent(gcps, false);
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
            // initialize Workers
            var b = await Initialize();

            //get all AG venues
            var agVenues = await _agWorker.GetAllVenues();
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
            var agShows = await _agWorker.GetAllEvents();
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

        static async Task<bool> Initialize()
        {
            //// Tempo: Get Artist Xref
            Logger.WriteLine(":::Update Artist Growth alternateIds:::");

            _tempoWorker = new TempoWorker();

            //// GCP: Get Artists from GCP
            _gcpWorker = new GCPWorker();
            _gcpArtists = _gcpWorker.GetArtistList(_tempoWorker.ArtistXrefs);

            //// AG: Create AGWorker and Authenticate
            _agWorker = new AGWorker();
            await _agWorker.Authenticate();

            Logger.WriteLine("------------------------------------------", ConsoleColor.Blue);

            return true;
        }
    }
}