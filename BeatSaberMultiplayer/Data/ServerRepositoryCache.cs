using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.Data
{
    public class ServerRepositoryCache
    {
        [JsonProperty("ServerRepositories")]
        public Dictionary<string, ServerRepository> ServerRepositories { get; set; } = new Dictionary<string, ServerRepository>();
        public static ServerRepositoryCache FromJson(string json) => JsonConvert.DeserializeObject<ServerRepositoryCache>(json, BeatSaberMultiplayerLite.Data.Converter.Settings);
    }

}
