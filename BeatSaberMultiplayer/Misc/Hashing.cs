using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.Misc
{
    public static class Hashing
    {
        /// <summary>
        /// Generates a hash for the song and assigns it to the SongHash field. Returns null if info.dat doesn't exist.
        /// Uses Kylemc1413's implementation from SongCore.
        /// TODO: Handle/document exceptions (such as if the files no longer exist when this is called).
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <returns>Hash of the song files. Null if the info.dat file doesn't exist</returns>
        public static string GenerateHash(string songDirectory, string existingHash = "")
        {
            byte[] combinedBytes = Array.Empty<byte>();
            string infoFile = Path.Combine(songDirectory, "info.dat");
            if (!File.Exists(infoFile))
                return null;
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(infoFile)).ToArray();
            var token = JToken.Parse(File.ReadAllText(infoFile));
            var beatMapSets = token["_difficultyBeatmapSets"];
            int numChars = beatMapSets.Children().Count();
            for (int i = 0; i < numChars; i++)
            {
                var diffs = beatMapSets.ElementAt(i);
                int numDiffs = diffs["_difficultyBeatmaps"].Children().Count();
                for (int i2 = 0; i2 < numDiffs; i2++)
                {
                    var diff = diffs["_difficultyBeatmaps"].ElementAt(i2);
                    string beatmapPath = Path.Combine(songDirectory, diff["_beatmapFilename"].Value<string>());
                    if (File.Exists(beatmapPath))
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath)).ToArray();
                    else
                        Plugin.log?.Debug($"Missing difficulty file {beatmapPath.Split('\\', '/').LastOrDefault()}");
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            if (!string.IsNullOrEmpty(existingHash) && !string.Equals(existingHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                Plugin.log?.Warn($"Hash doesn't match the existing hash for {songDirectory}");
                Plugin.log?.Debug($"Expected: {existingHash} | Actual: {hash}");
            }
            return hash;
        }

        /// <summary>
        /// Returns the Sha1 hash of the provided byte array.
        /// Uses Kylemc1413's implementation from SongCore.
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <param name="input">Byte array to hash.</param>
        /// <returns>Sha1 hash of the byte array.</returns>
        public static string CreateSha1FromBytes(byte[] input)
        {
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = input;
                var hashBytes = sha1.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }
    }
}
