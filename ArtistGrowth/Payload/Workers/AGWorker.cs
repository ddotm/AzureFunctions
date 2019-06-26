using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Payload.Models.ArtistGrowth;
using Payload.Models.GCP;
using Payload.Models.Tempo;
using Newtonsoft.Json;
using Payload.Utilities;

namespace Payload.Workers
{
    public class AGWorker
    {
        private HttpClient _httpClient;
        private string _token;

        public async Task<bool> Authenticate()
        {
            Logger.Write("Authenticating with Artist Growth... ");

            // set up the basic client
            this._httpClient = new HttpClient();

//            // base url - stage
//            _httpClient.BaseAddress = new Uri("https://staging.artistgrowth.com/api/v2/");

            // base url - production
            _httpClient.BaseAddress = new Uri("https://artistgrowth.com/api/v2/");


            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (string.IsNullOrWhiteSpace(_token))
            {
                //// stage credentials
                //var userName = "dev+paradigm@artistgrowth.com";
                //var password = "Y,3TrEN7cBUN+VC(PCMrm,P4n2cReoWG";

                // prod credentials
                var userName = "dev+paradigm@artistgrowth.com";
                var password = "&V6Mc37WqvbsozwUPPMR(%T?7dVCHt";

                var jsonCreds = new { password = password, email = userName };

                // authenticate
                HttpResponseMessage authResp = await _httpClient.PostAsJsonAsync("auth/", jsonCreds);
                authResp.EnsureSuccessStatusCode();
                var authResult = await authResp.Content.ReadAsStringAsync();
                var token = Newtonsoft.Json.JsonConvert.DeserializeObject<AGToken>(authResult);
                _token = token.auth_token;
            }

            // add auth token to header fpr future requests
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);

            Logger.WriteLine("done", ConsoleColor.Green);

            return true;
        }

        public async Task<List<AGOrganization>> GetOrganizations()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("organizations/?alternate_id=");
            var responseContent = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<AGOrganizationList>(responseContent);

            foreach (var row in list.results)
            {
                if (row.calendar != null)
                {
                    row.calendaPk = row.calendar.pk;
                }
            }

            return list.results;
        }


        // VENUES
        public async Task<bool> SyncVenues(List<GCPVenue> gcpVenues)
        {
            Logger.Write($"   Updating venues in Artist Growth... ");
            Logger.Write($"   Inserting ");
            Logger.Write($"{gcpVenues.Count}", ConsoleColor.Green);
            Logger.Write($" new venue records into Artist Growth... ");

            foreach (var gcpVenue in gcpVenues)
            {
                var agVenue = await this.GetVenueByAltId(gcpVenue.alternateId);
                if (agVenue != null)
                {
                    //todo: only call if updated dates differ
                    var agDate = DateTime.Parse(agVenue.date_modified);

                    if (gcpVenue.updatedAt > agDate)
                    {
                        gcpVenue.AGVenueId = agVenue.pk;
                        agVenue = await UpdateVenue(gcpVenue, false);
                        gcpVenue.ActionTaken = "Updated";
                    }
                    else
                    {
                        gcpVenue.AGVenueId = agVenue.pk;
                        gcpVenue.ActionTaken = "Venue already up to date";
                    }
                } else
                {
                    try
                    {
                        agVenue = await UpdateVenue(gcpVenue, true);
                        gcpVenue.AGVenueId = (agVenue == null) ? "" : agVenue.pk;
                        gcpVenue.ActionTaken = "Inserted";
                    }
                    catch (Exception e)
                    {
                        gcpVenue.ActionTaken = $"Insert Failed: {e.Message}";
                    }
                }
            }

            var c = gcpVenues.Count(w => w.ActionTaken.StartsWith("Updated"));
            Logger.WriteLine($"{c} ", ConsoleColor.Red, "venues updated done");

            return true;
        }
        public async Task<AGVenue> GetVenueByAltId(string altId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync("venues/?alternate_id=" + altId);
            var responseContent = await response.Content.ReadAsStringAsync();
            var venueList = JsonConvert.DeserializeObject<AGVenueList>(responseContent);
            return (venueList.results.Count > 0 ? venueList.results.FirstOrDefault() : null);
        }
        public async Task<AGVenue> UpdateVenue(GCPVenue gcpVenue, bool isInsert)
        {
            GCPVenueAddress addr;

            if (string.IsNullOrWhiteSpace(gcpVenue.address))
            {
                addr = new GCPVenueAddress();
            }
            else
            {
                addr = JsonConvert.DeserializeObject<GCPVenueAddress>(gcpVenue.address);
            }

            var agVenue = new AGVenue()
            {
                alternate_id = gcpVenue.alternateId,
                name = gcpVenue.name,
                address_line_1 = addr.address,
                address_line_2 = "",
                city = addr.city,
                region = (string.IsNullOrWhiteSpace(addr.state) ? addr.country : addr.state),
                country = gcpVenue.country,
                postal_code = string.IsNullOrWhiteSpace(addr.zipCode) ? "" : addr.zipCode,
                //tz = "UTC",
                source = "vnd-paradigm",
                contact_name = "",
                website = "",
                phone = "",
                capacity = gcpVenue.TranslateCapacity().ToString()
            };

            HttpResponseMessage response;

            if (isInsert)
            {
                response = await _httpClient.PostAsJsonAsync("venues/", agVenue);
            }
            else
            {
                agVenue.pk = gcpVenue.AGVenueId;
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(agVenue, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                response = await _httpClient.PutAsJsonAsync($"venues/{gcpVenue.AGVenueId}/", agVenue);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                case System.Net.HttpStatusCode.Accepted:
                    break;

                default:
                    throw new Exception($"Error {response.StatusCode} -- {responseContent}");
            }

            var venue = JsonConvert.DeserializeObject<AGVenue>(responseContent);
            return venue;
        }
        public async Task<List<AGEvent>> GetAllEvents()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("events/?source=vnd-paradigm");
            var responseContent = await response.Content.ReadAsStringAsync();
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                case System.Net.HttpStatusCode.Accepted:
                    break;

                default:
                    throw new Exception($"Error {response.StatusCode} -- {responseContent}");
            }
            var eventList = JsonConvert.DeserializeObject<AGEventList>(responseContent);
            return eventList.results;
        }


        // SHOWS/EVENTS
        public async Task<bool> SyncEvents(List<GCPShow> gcpShows)
        {
            foreach (var gcpShow in gcpShows)
            {
                if (string.IsNullOrWhiteSpace(gcpShow.TranslateStatus()) == false)
                {
                    var agEvent= await this.GetEventByAltId(gcpShow.alternateId);
                    if (agEvent != null)
                    {
                        gcpShow.AGEvent_pk = agEvent.pk;
                        agEvent = await UpdateEvent(gcpShow, false);
                        gcpShow.ActionTaken = "None";
                    }
                    else
                    {
                        try
                        {
                            agEvent = await UpdateEvent(gcpShow, true);
                            gcpShow.AGEvent_pk = (agEvent == null) ? "" : agEvent.pk;
                            gcpShow.ActionTaken = "Inserted";
                        }
                        catch (Exception e)
                        {
                            gcpShow.ActionTaken = $"Insert Failed: {e.Message}";
                        }
                    }
                }
                else {
                    gcpShow.ActionTaken = $"Show Skipped for INVALID status: {gcpShow.status}";
                }
            }

            return true;
        }
        public async Task<AGEvent> GetEventByAltId(string altId)
        {
            HttpResponseMessage response = await _httpClient.GetAsync("events/?alternate_id=" + altId);
            var responseContent = await response.Content.ReadAsStringAsync();
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                case System.Net.HttpStatusCode.Accepted:
                    break;

                default:
                    throw new Exception($"Error {response.StatusCode} -- {responseContent}");
            }
            var eventList = JsonConvert.DeserializeObject<AGEventList>(responseContent);
            return (eventList.results.Count > 0 ? eventList.results.FirstOrDefault() : null);
        }
        public async Task<AGEvent> UpdateEvent(GCPShow gcpShow, bool isInsert)
        {
            var agEvent= new AGEvent()
            {
                alternate_id = gcpShow.alternateId,
                name = gcpShow.VenueName,
                calendar = gcpShow.AGCalendar_pk,
                is_all_day = true,
                location = gcpShow.AGLocation_pk,
                start_date = gcpShow.dateUtc.ToString("yyyy-MM-dd"),
                status = gcpShow.TranslateStatus(),
                source = "vnd-paradigm",
            };

            if (gcpShow.IncludeFinancials)
            {
                agEvent.finance.guarantee = gcpShow.fee.ToString();
            }

            HttpResponseMessage response;

            if (isInsert)
            {
                response = await _httpClient.PostAsJsonAsync("events/", agEvent);
            } else
            {
                agEvent.pk = gcpShow.AGEvent_pk;
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(agEvent, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                
                response = await _httpClient.PutAsJsonAsync($"events/{gcpShow.AGEvent_pk}/", agEvent);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                case System.Net.HttpStatusCode.Accepted:
                    break;

                default:
                    throw new Exception($"Error {response.StatusCode} -- {responseContent}");
            }

            var result = JsonConvert.DeserializeObject<AGEvent>(responseContent);
            return result;
        }
        public async Task<List<AGVenue>> GetAllVenues()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("venues/?source=vnd-paradigm");
            var responseContent = await response.Content.ReadAsStringAsync();
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                case System.Net.HttpStatusCode.Accepted:
                    break;

                default:
                    throw new Exception($"Error {response.StatusCode} -- {responseContent}");
            }
            var eventList = JsonConvert.DeserializeObject<AGVenueList>(responseContent);
            return eventList.results;
        }
    }
}
