using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BeatSaberMultiplayerLite.Data
{
    public partial class ServerRepository
    {
        [JsonProperty("RepositoryName")]
        public string RepositoryName { get; set; }

        [JsonProperty("RepositoryDescription", NullValueHandling = NullValueHandling.Ignore)]
        public string RepositoryDescription { get; set; }

        [JsonProperty("Servers")]
        public List<Server> Servers { get; set; }
    }

    public partial class Server
    {
        [JsonProperty("ServerName", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerName { get; set; }

        [JsonProperty("ServerAddress")]
        public string ServerAddress { get; set; }

        [JsonProperty("ServerPort")]
        public long ServerPort { get; set; }
    }

    public partial class ServerRepository
    {
        public static ServerRepository FromJson(string json) => JsonConvert.DeserializeObject<ServerRepository>(json, BeatSaberMultiplayerLite.Data.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ServerRepository self) => JsonConvert.SerializeObject(self, BeatSaberMultiplayerLite.Data.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            //Converters =
            //{
            //    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            //},
        };
    }
}
