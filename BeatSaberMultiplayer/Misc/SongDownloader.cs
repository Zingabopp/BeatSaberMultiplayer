﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static BeatSaberMultiplayerLite.Misc.ZipUtilities;

namespace BeatSaberMultiplayerLite.Misc
{
    public class SongDownloader : MonoBehaviour
    {
        public event Action<Song> songDownloaded;

        private static SongDownloader _instance = null;
        public static SongDownloader Instance
        {
            get
            {
                if (!_instance)
                    _instance = new GameObject("SongDownloader").AddComponent<SongDownloader>();
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private List<Song> _alreadyDownloadedSongs;
        private static bool _extractingZip;

        private static BeatmapLevelsModel _beatmapLevelsModel;

        public static UnityWebRequest GetRequestForUri(Uri uri)
        {
            UnityWebRequest www = UnityWebRequest.Get(uri);
            www.SetRequestHeader("User-Agent", UserAgent);
            return www;
        }

        public static string UserAgent { get; } = $"{Assembly.GetExecutingAssembly().GetName().Name}/{Assembly.GetExecutingAssembly().GetName().Version}";
        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (!SongCore.Loader.AreSongsLoaded)
            {
                SongCore.Loader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }
            else
            {
                SongLoader_SongsLoadedEvent(null, SongCore.Loader.CustomLevels);
            }

        }

        private void SongLoader_SongsLoadedEvent(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            _alreadyDownloadedSongs = levels.Values.Select(x => new Song(x)).ToList();
        }

        public void DownloadSong(Song songInfo, Action<bool, string> downloadedCallback, Action<float> progressChangedCallback)
        {
            StartCoroutine(DownloadSongCoroutine(songInfo, downloadedCallback, progressChangedCallback));
        }

        public IEnumerator DownloadSongCoroutine(Song songInfo, Action<bool, string> downloadedCallback, Action<float> progressChangedCallback)
        {
            if (songInfo == null)
            {
                Plugin.log.Error($"songInfo is null on DownloadSongCoroutine, this shouldn't happen.");
                yield break;
            }
            string songIdentifier = songInfo?.songName;
            if (string.IsNullOrEmpty(songIdentifier))
                songIdentifier = songInfo?.hash;
            songInfo.songQueueState = SongQueueState.Downloading;

            if (SongCore.Collections.songWithHashPresent(songInfo.hash.ToUpper()))
            {
                Plugin.log.Debug($"Song already downloaded: {songIdentifier}");
                songInfo.downloadingProgress = 1f;
                yield return new WaitForSeconds(0.1f);
                songInfo.songQueueState = SongQueueState.Downloaded;
                songDownloaded?.Invoke(songInfo);
                downloadedCallback?.Invoke(true, "Finished");
                yield break;
            }
            Plugin.log.Info($"Attempting to download song '{songIdentifier}' from {songInfo.downloadURL}");
            UnityWebRequest www;
            bool timeout = false;
            float time = 0f;
            UnityWebRequestAsyncOperation asyncRequest;

            try
            {
                Uri uri = new Uri(songInfo.downloadURL);
                www = GetRequestForUri(uri);

                asyncRequest = www.SendWebRequest();
            }
            catch (Exception e)
            {
                Plugin.log.Error($"Error downloading song from '{songInfo.downloadURL}': {e.Message}");
                Plugin.log.Debug(e);
                songInfo.songQueueState = SongQueueState.Error;
                songInfo.downloadingProgress = 0f;
                downloadedCallback?.Invoke(false, "Error downloading song");
                yield break;
            }

            while ((!asyncRequest.isDone || songInfo.downloadingProgress < 1f) && songInfo.songQueueState != SongQueueState.Error)
            {
                yield return null;

                time += Time.deltaTime;

                if (time >= 5f && asyncRequest.progress <= float.Epsilon)
                {
                    www.Abort();
                    timeout = true;
                    Plugin.log.Error("Connection timed out!");
                }

                songInfo.downloadingProgress = asyncRequest.progress;
                progressChangedCallback?.Invoke(asyncRequest.progress);
            }

            if (songInfo.songQueueState == SongQueueState.Error && (!asyncRequest.isDone || songInfo.downloadingProgress < 1f))
                www.Abort();

            if (www.isNetworkError || www.isHttpError || timeout || songInfo.songQueueState == SongQueueState.Error)
            {
                songInfo.songQueueState = SongQueueState.Error;
                Plugin.log.Error("Unable to download song! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
                if (www.responseCode == 404)
                    downloadedCallback?.Invoke(false, "Song not found on Beat Saver.");
                else
                    downloadedCallback?.Invoke(false, "Network error downloading song.");
            }
            else
            {
                Plugin.log.Debug("Received response from BeatSaver.com...");
                string customSongsPath = "";

                byte[] data = www.downloadHandler.data;

                Stream zipStream = null;

                try
                {
                    customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    zipStream = new MemoryStream(data);
                    Plugin.log.Debug("Downloaded zip!");
                }
                catch (Exception e)
                {
                    Plugin.log.Critical(e);
                    songInfo.songQueueState = SongQueueState.Error;
                    downloadedCallback?.Invoke(false, "Error extracting zip.");
                    Plugin.log.Error($"Error extracting zip: {e.Message}");
                    Plugin.log.Debug(e);
                    yield break;
                }

                yield return new WaitWhile(() => _extractingZip); //because extracting several songs at once sometimes hangs the game

                Task extract = ExtractZipAsync(songInfo, zipStream, customSongsPath);
                yield return new WaitWhile(() => !extract.IsCompleted);
                Plugin.log.Debug($"ExtractZipAsync complete for song: {songInfo.path}");
                try
                {
                    string generatedHash = Hashing.GenerateHash(songInfo.path, songInfo.hash);
                }
                catch (Exception ex)
                {
                    Plugin.log.Error($"Error hashing song in directory {songInfo.path}: {ex.Message}");
                    Plugin.log.Debug(ex);
                }
                if (songDownloaded != null)
                    songDownloaded.Invoke(songInfo);
                else
                    Plugin.log.Debug($"{nameof(songDownloaded)} has no handlers to invoke.");
                if (downloadedCallback != null)
                    downloadedCallback.Invoke(true, string.Empty);
                else
                    Plugin.log.Debug($"No callbacks assigned to {downloadedCallback}");
            }
        }

        private async Task ExtractZipAsync(Song songInfo, Stream zipStream, string customSongsPath)
        {
            try
            {
                Plugin.log.Debug("Extracting...");
                _extractingZip = true;
                //ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                string basePath = songInfo.key + " (" + songInfo.songName + " - " + songInfo.levelAuthorName + ")";
                basePath = string.Join("", basePath.Split((Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray())));
                string path = Path.GetFullPath(Path.Combine(customSongsPath, basePath));

                if (Directory.Exists(path))
                {
                    int pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
                    path += $" ({pathNum})";
                }
                Plugin.log.Debug($"Extracing to '{path}'");
                ZipExtractResult result = await Task.Run(() => ZipUtilities.ExtractZip(zipStream, path, true));
                if (result.ResultStatus != ZipExtractResultStatus.Success || result.Exception != null)
                {
                    Plugin.log.Error($"Error extracting song zip to folder: {result.Exception.Message}");
                    Plugin.log.Debug(result.Exception);
                    _extractingZip = false;
                    return;
                }
                //await Task.Run(() => archive.ExtractToDirectory(path)).ConfigureAwait(false);
                //archive.Dispose();
                if (result.OutputDirectory != path)
                {
                    Plugin.log.Warn($"ZipExtractResult OutputDirectory ({result.OutputDirectory}) does not match path ({path})");
                    songInfo.path = result.OutputDirectory;
                }
                else
                    songInfo.path = path;
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to extract ZIP! Exception: {e.Message}");
                Plugin.log.Debug(e);
                songInfo.songQueueState = SongQueueState.Error;
                _extractingZip = false;
                return;
            }
            zipStream.Close();

            if (string.IsNullOrEmpty(songInfo.path))
            {
                songInfo.path = customSongsPath;
            }

            _extractingZip = false;
            songInfo.songQueueState = SongQueueState.Downloaded;
            _alreadyDownloadedSongs.Add(songInfo);
            Plugin.log.Debug($"Extracted {songInfo.songName} {songInfo.songSubName}!");

        }

        public bool IsSongDownloaded(Song song)
        {
            if (SongCore.Loader.AreSongsLoaded)
                return _alreadyDownloadedSongs.Any(x => x.Compare(song));
            else
                return false;
        }

        public static CustomPreviewBeatmapLevel GetLevel(string levelId)
        {
            if (_beatmapLevelsModel == null)
            {
                _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();
            }
            return _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID == levelId) as CustomPreviewBeatmapLevel;
        }

        public static bool CreateMD5FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path)) return false;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }

        public void RequestSongByLevelID(string levelId, Action<Song, string> callback)
        {
            StartCoroutine(RequestSongByLevelIDCoroutine(levelId, callback));
        }

        public IEnumerator RequestSongByLevelIDCoroutine(string levelId, Action<Song, string> callback)
        {
            string requestUrl = $"{Config.Instance.MultiplayerSettings.BeatSaverURL}/api/maps/by-hash/{levelId.ToLower()}";
            Uri uri;
            UnityWebRequest wwwId;
            try
            {
                uri = new Uri(requestUrl);
                wwwId = GetRequestForUri(uri);
                wwwId.timeout = 10;
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error downloading song by levelId from '{requestUrl}': {ex.Message}");
                Plugin.log.Debug(ex);
                callback?.Invoke(null, "Error downloading song.");
                yield break;
            }

            yield return wwwId.SendWebRequest();


            if (wwwId.isNetworkError || wwwId.isHttpError)
            {
                Plugin.log.Error($"Unable to fetch song by hash! {wwwId.error}\nURL:" + $"{Config.Instance.MultiplayerSettings.BeatSaverURL}/api/maps/by-hash/" + levelId.ToLower());
                if (wwwId.responseCode == 404)
                    callback?.Invoke(null, "Song not found on Beat Saver.");
                else
                    callback?.Invoke(null, "Network error downloading song.");
            }
            else
            {
                JObject jNode = JObject.Parse(wwwId.downloadHandler.text);
                if (jNode.Children().Count() == 0)
                {
                    string message = $"Song {levelId} doesn't exist on BeatSaver!";
                    Plugin.log.Error(message);
                    callback?.Invoke(null, message);
                    yield break;
                }

                Song _tempSong = Song.FromSearchNode(jNode);
                callback?.Invoke(_tempSong, string.Empty);
            }
        }

        public void RequestSongByKey(string key, Action<Song> callback)
        {
            StartCoroutine(RequestSongByKeyCoroutine(key, callback));
        }

        public IEnumerator RequestSongByKeyCoroutine(string key, Action<Song> callback)
        {
            UnityWebRequest wwwId;
            string url = $"{Config.Instance.MultiplayerSettings.BeatSaverURL}/api/maps/detail/{key.ToLower()}";
            try
            {
                Uri uri = new Uri(url);
                wwwId = GetRequestForUri(uri);
                wwwId.timeout = 10;
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error requesting song by key using '{url}': {ex.Message}");
                Plugin.log.Debug(ex);
                yield break;
            }

            yield return wwwId.SendWebRequest();


            if (wwwId.isNetworkError || wwwId.isHttpError)
            {
                Plugin.log.Error(wwwId.error);
            }
            else
            {
                JObject node = JObject.Parse(wwwId.downloadHandler.text);

                Song _tempSong = new Song((JObject)node, false);
                callback?.Invoke(_tempSong);
            }
        }
    }
}
