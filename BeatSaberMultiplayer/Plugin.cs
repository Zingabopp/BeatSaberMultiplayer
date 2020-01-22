using BeatSaberMultiplayerLite.Misc;
using BeatSaberMultiplayerLite.UI;
using BS_Utils.Gameplay;
using Harmony;
using IPA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeatSaberMultiplayerLite.DiscordInterface;
#if DEBUG
using System.Diagnostics;
using System.IO;
#endif

namespace BeatSaberMultiplayerLite
{
    public class Plugin : IBeatSaberPlugin
    {
        public static Version ClientCompatibilityVersion = new Version(0, 7, 0, 0);
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        public static IDiscordInstance discord;
        public static GameActivity gameActivity;
        private static bool joinAfterRestart;
        private static string joinSecret;
        private static PlayerAvatarInput _playerAvatarInput;
        public static bool overrideDiscordActivity;
        public static bool DownloaderExists { get; private set; }
        public static void LogLocation(string message,
            [CallerFilePath] string memberPath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int line = -1)
        {

#if DEBUG
            log.Info($"{Path.GetFileName(memberPath)}_{memberName}({{{line}}}): {message}");
            var stackTrace = new StackTrace(1, true);
            List<StackFrame> frames = new List<StackFrame>();
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                StackFrame frame = stackTrace.GetFrame(i);
                var frameAssembly = frame.GetMethod().DeclaringType.Assembly;
                if (frameAssembly == Assembly.GetExecutingAssembly())
                {
                    frames.Add(frame);
                    Console.WriteLine(frame);
                }
            }
            foreach (var frame in frames)
            {
                log.Debug(frame.ToString());
            }
#endif
        }
        public void Init(IPA.Logging.Logger logger)
        {
            log = logger;
            _playerAvatarInput = new PlayerAvatarInput();
        }

        public void OnApplicationStart()
        {
            instance = this;

            BS_Utils.Utilities.BSEvents.OnLoad();
            BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh += MenuSceneLoadedFresh;
            BS_Utils.Utilities.BSEvents.menuSceneLoaded += MenuSceneLoaded;
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += GameSceneLoaded;
            if (Config.Load())
                log.Info("Loaded config!");
            else
                Config.Create();

            try
            {
                PresetsCollection.ReloadPresets();
            }
            catch (Exception e)
            {
                log.Warn("Unable to load presets! Exception: " + e);
            }

            Sprites.ConvertSprites();

            ScrappedData.Instance.DownloadScrappedData(null);
            if (IPA.Loader.PluginManager.GetPluginFromId("BeatSaverDownloader") != null)
                DownloaderExists = true;
            try
            {
                var harmony = HarmonyInstance.Create("com.andruzzzhka.BeatSaberMultiplayer");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Plugin.log.Error("Unable to patch assembly! Exception: " + e);
            }
            if (IPA.Loader.PluginManager.GetPluginFromId("DiscordCore") != null)
            {
                Plugin.log.Info("DiscordCore found, Discord Rich Presence will be available.");
                discord = PresenceLoader.LoadDiscord("BeatSaberMultiplayer", "Beat Saber Multiplayer", Sprites.onlineIcon, true, 661577830919962645);
                discord.ActivityJoinReceived += OnActivityJoin;
                discord.ActivityJoinRequest += ActivityManager_OnActivityJoinRequest;
                discord.ActivityInviteReceived += ActivityManager_OnActivityInvite;
                discord.Destroyed += Discord_Destroyed;
            }
        }

        private void Discord_Destroyed(object sender, EventArgs e)
        {
            if (discord == sender)
                discord = null;
        }

        private void ActivityManager_OnActivityInvite(object sender, ActivityInviteEventArgs args)
        {
            if (SceneManager.GetActiveScene().name.Contains("Menu") && args.GameActivityAction == GameActivityActionType.Join && !Client.Instance.inRoom && !Client.Instance.inRadioMode)
            {
                PluginUI.instance.ShowInvite(args.User, args.Activity);
            }
        }

        private void ActivityManager_OnActivityJoinRequest(object sender, IActivityJoinRequest joinRequest)
        {
            if (SceneManager.GetActiveScene().name.Contains("Menu"))
            {
                PluginUI.instance.ShowJoinRequest(joinRequest);
            }
        }
        
        public void OnActivityJoin(object sender, string secret)
        {
            if(string.IsNullOrEmpty(secret))
            {
                Plugin.log.Warn($"Unable to join game, room information unavailble.");
            }
            if (SceneManager.GetActiveScene().name.Contains("Menu") && !Client.Instance.inRoom && !Client.Instance.inRadioMode)
            {
                joinAfterRestart = true;
                joinSecret = secret;
                Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().First().RestartGame();
            }
        }

        private void MenuSceneLoadedFresh()
        {
            //ModelSaberAPI.HashAllAvatars();
            PluginUI.OnLoad();
            InGameOnlineController.OnLoad();
            SpectatingController.OnLoad();
            GetUserInfo.UpdateUserInfo();
            if (joinAfterRestart)
            {
                joinAfterRestart = false;
                SharedCoroutineStarter.instance.StartCoroutine(PluginUI.instance.JoinGameWithSecret(joinSecret));
                joinSecret = string.Empty;
            }
        }

        private void MenuSceneLoaded()
        {
            InGameOnlineController.Instance?.MenuSceneLoaded();
            if (Config.Instance.SpectatorMode)
                SpectatingController.Instance?.MenuSceneLoaded();
        }

        private void GameSceneLoaded()
        {
            InGameOnlineController.Instance?.GameSceneLoaded();
            if (Config.Instance.SpectatorMode)
                SpectatingController.Instance?.GameSceneLoaded();
        }

        public void OnApplicationQuit()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }
        /*
        private void DiscordLogCallback(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    {
                        log.Log(IPA.Logging.Logger.Level.Debug, $"[DISCORD] {message}");
                    }
                    break;
                case LogLevel.Info:
                    {
                        log.Log(IPA.Logging.Logger.Level.Info, $"[DISCORD] {message}");
                    }
                    break;
                case LogLevel.Warn:
                    {
                        log.Log(IPA.Logging.Logger.Level.Warning, $"[DISCORD] {message}");
                    }
                    break;
                case LogLevel.Error:
                    {
                        log.Log(IPA.Logging.Logger.Level.Error, $"[DISCORD] {message}");
                    }
                    break;
            }
        }
        */
    }
}
