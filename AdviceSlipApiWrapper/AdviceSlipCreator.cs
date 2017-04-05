using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace AdviceSlipApiWrapper
{
    public static class AdviceSlipCreator
    {
        private const string baseUrl = "http://api.adviceslip.com/advice";

        /// <summary>
        /// Gets either a random or a specific (based on ID) advice slip from the API.
        /// </summary>
        /// <param name="slipId">The Advice Slip's ID. If unspecified, returns a random one instead.</param>
        /// <returns>A random advice slip, or the specified one. Returns null if slip with specified ID is not found.</returns>
        public static async Task<AdviceSlip> Get(int? slipId = null)
        {
            var resObject = await GetResponseObjectFromApi(slipId?.ToString());

            // Check for error:
            if (resObject.slip == null)
                return null;

            return new AdviceSlip(resObject.slip.advice, resObject.slip.slip_id);
        }
        public static AdviceSlip GetSync(int? slipId = null) => Get(slipId).GetAwaiter().GetResult();

        /// <summary>
        /// Searches the Advice slip API for a specific term.
        /// </summary>
        /// <param name="searchQuery">The string to be looked for in the API. Do not enter invalid characters like "/".</param>
        /// <returns>An array of all Advice Slips that fit the search term, or null if none were found.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<AdviceSlip[]> Search(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
                throw new ArgumentException("Enter a search term!");

            ResponseObject resObject;
            try
            {
                resObject = await GetResponseObjectFromApi("search/" + searchQuery);
            }
            catch (WebException exc)
            {
                if (exc.Status == WebExceptionStatus.ProtocolError && (exc.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                    throw new ArgumentException("Enter a valid search term!");
                else
                    throw;
            }

            if (resObject.message?.type == ResponseObject.Message.MessageType.Notice)
                return null;

            AdviceSlip[] result = new AdviceSlip[resObject.total_results.Value];
            int index = 0;
            foreach (var slip in resObject.slips)
            {
                result[index++] = new AdviceSlip(slip.advice, slip.slip_id);
            }

            return result;
        }
        public static AdviceSlip[] SearchSync(string searchQuery) => Search(searchQuery).GetAwaiter().GetResult();

        /// <summary>
        /// Gets the JSON from the Advice Slip API. Can also specify arguments.
        /// </summary>
        /// <param name="args">The parameters to be appended to the URL (for the GET request). The starting "/" character will be added automatically.</param>
        /// <returns>Returns the resulting JSON as a ResponseObject.</returns>
        /// <exception cref="WebException"></exception>
        private static async Task<ResponseObject> GetResponseObjectFromApi(string args = "")
        {
            string reqUrl = (args == "" || args == null) ? baseUrl : baseUrl + "/" + args;
            HttpWebRequest req = WebRequest.CreateHttp(reqUrl);

            HttpWebResponse res = await req.GetResponseAsync() as HttpWebResponse;
            if (res.StatusCode != HttpStatusCode.OK)
                throw new WebException();

            return JsonConvert.DeserializeObject<ResponseObject>(await new StreamReader(res.GetResponseStream()).ReadToEndAsync());
        }

        // Classes to handle the JSON deserialization, for internal use
        private class ResponseObject
        {
            // Gets set for a random advice slip
            public ActualSlip slip { get; set; }

            // This is a message (comes up for errors, for example)
            public Message message { get; set; }

            // These are for search results
            public int? total_results { get; set; }
            public string query { get; set; } // This one seems to be empty no matter what
            public ActualSlip[] slips { get; set; }

            internal class ActualSlip
            {
                public string advice { get; set; }
                public int slip_id { get; set; }
            }

            internal class Message
            {
                [JsonConverter(typeof(StringEnumConverter))]
                public MessageType type { get; set; }
                public string text { get; set; }

                internal enum MessageType
                {
                    Notice,
                    Warning,
                    Error
                }
            }
        }
    }
}
