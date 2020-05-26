﻿using BeatSaberMultiplayerLite.Misc;
using BeatSaberMultiplayerLite.OverriddenClasses;
using BeatSaberMultiplayerLite.UI;
using BS_Utils.Gameplay;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeatSaberMultiplayerLite.RichPresence;
using System.Globalization;
using System.IO;
using IPA.Loader;
#if DEBUG
using System.Diagnostics;
using System.IO;
#endif

namespace BeatSaberMultiplayerLite
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static readonly string PluginID = "BeatSaberMultiplayerLite";
        public static readonly string PluginName = "Beat Saber Multiplayer Lite";
        public static readonly string HarmonyId = "com.Zingabopp.BeatSaberMultiplayerLite";
        public static string PluginVersion { get; private set; }
        public static Version ClientCompatibilityVersion = new Version(0, 7, 2, 0);
        internal static PluginMetadata PluginMetadata;
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        public static PresenceManager PresenceManager { get; private set; }
        internal static LocalPlayerPosition _playerPosition;
        public static bool IsSteam { get; private set; }
        private static bool joinAfterRestart;
        private static string joinSecret;
        public static FirstPersonFlyingController fpfc;
        public static bool DownloaderExists { get; private set; }
        private bool _fullMultiplayerExists = false;
        private bool _fullMultiplayerCheckComplete = false;
        public static string Username;
        public static ulong UserId;

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
                    log.Info(frame.ToString());
                }
            }
#endif
        }

        private bool FullMultiplayerExists
        {
            get
            {
                if (!_fullMultiplayerCheckComplete)
                {
                    _fullMultiplayerExists = PluginManager.GetPluginFromId("BeatSaberMultiplayer") != null && PluginManager.GetDisabledPluginFromId("BeatSaberMultiplayer") == null;
                    _fullMultiplayerCheckComplete = true;
                }
                return _fullMultiplayerExists;
            }
        }

        [Init]
        public void Init(IPA.Logging.Logger logger, PluginMetadata pluginMetadata)
        {
            log = logger;
            PluginMetadata = pluginMetadata;
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            PluginVersion = $"{v.Major}.{v.Minor}.{v.Build}";
            log.Info($"{PluginMetadata.Name} v{PluginVersion} initialized. Current culture is {CultureInfo.CurrentCulture.Name}");
        }

        [Init]
        public void InitWithConfig(IPA.Config.Config conf)
        {
            var config = conf.Generated<Config>();
            Config.Instance = config;
            string oldModVersion = config.MultiplayerSettings.ModVersion;
            config.MultiplayerSettings.ModVersion = PluginVersion;
            log.Debug("Config loaded");
        }

        [OnStart]
        public void OnApplicationStart()
        {

            if (FullMultiplayerExists)
            {
                Plugin.log.Critical("BeatSaberMultiplayer mod detected, disabling BeatSaberMultiplayerLite.");
                return;
            }
            instance = this;
            _playerPosition = new LocalPlayerPosition();
            //BS_Utils.Utilities.BSEvents.OnLoad();
            BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh += MenuSceneLoadedFresh;
            BS_Utils.Utilities.BSEvents.menuSceneLoaded += MenuSceneLoaded;
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += GameSceneLoaded;

            try
            {
                PresetsCollection.ReloadPresets();
            }
            catch (Exception e)
            {
                log.Warn("Unable to load presets! Exception: " + e);
            }

            Sprites.ConvertSprites();
#if DEBUG
            //var thing = new GameObject().AddComponent<InputTestController>();
            //GameObject.DontDestroyOnLoad(thing);
#endif
            ControllersHelper.Init();
            ScrappedData.Instance.DownloadScrappedData(null);
            if (IPA.Loader.PluginManager.GetPluginFromId("BeatSaverDownloader") != null)
                DownloaderExists = true;
            OverriddenClasses.HarmonyPatcher.PatchAll();
            PresenceManager = new PresenceManager();
            var connectString = Environment.GetCommandLineArgs().Where(a => a.Contains("connect:")).FirstOrDefault();
            if (!string.IsNullOrEmpty(connectString))
            {
                joinSecret = connectString.Replace("connect:", string.Empty).Trim('|');
                joinAfterRestart = true;
                Plugin.log.Info($"Connect string {joinSecret} retrieved from launch args.");
            }
            IsSteam = SteamExists();
            PresenceManager.Initialize("BeatSaberMultiplayer", "Beat Saber Multiplayer", Sprites.onlineIcon, true, 661577830919962645);
            PresenceManager.ActivityJoinReceived += OnActivityJoin;
            PresenceManager.ActivityJoinRequest += ActivityManager_OnActivityJoinRequest;
            PresenceManager.ActivityInviteReceived += ActivityManager_OnActivityInvite;
        }

        private bool SteamExists()
        {
            string filePath = Path.GetFullPath(Path.Combine(CustomLevelPathHelper.baseProjectPath, "Plugins", "steam_api64.dll"));
            Plugin.log.Debug($"Checking '{filePath}' for Steam.");
            return File.Exists(filePath);
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
            if (string.IsNullOrEmpty(secret))
            {
                Plugin.log.Warn($"Unable to join game, room information unavailble.");
            }
            if (SceneManager.GetActiveScene().name.Contains("Menu") && !Client.Instance.inRoom && !Client.Instance.inRadioMode)
            {
                joinAfterRestart = true;
                joinSecret = secret;
                Plugin.log.Debug($"Restarting game for activity join.");
                Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().First().RestartGame();
            }
        }

        public static bool ReadUserInfo()
        {
            UserId = GetUserInfo.GetUserID();
            Username = GetUserInfo.GetUserName();
            if (UserId != 0)
                return true;
            return false;
        }
        
        private void MenuSceneLoadedFresh()
        {
            //ModelSaberAPI.HashAllAvatars();
            PluginUI.OnLoad();
            InGameOnlineController.OnLoad();
            SpectatingController.OnLoad();
            GetUserInfo.UpdateUserInfo();
            if (Environment.CommandLine.Contains("fpfc"))
            {
                fpfc = Resources.FindObjectsOfTypeAll<FirstPersonFlyingController>().FirstOrDefault();
                if (fpfc == null)
                    Plugin.log.Warn("Failed to get FirstPersonFlyingController");
            }
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
            SpectatingController.Instance?.MenuSceneLoaded();
        }

        private void GameSceneLoaded()
        {
            InGameOnlineController.Instance?.GameSceneLoaded();
            SpectatingController.Instance?.GameSceneLoaded();
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
