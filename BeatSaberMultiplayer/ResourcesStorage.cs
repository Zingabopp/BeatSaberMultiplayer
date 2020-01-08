using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static BeatSaberMarkupLanguage.Utilities;

namespace BeatSaberMultiplayer
{
    internal class ResourcesStorage
    {
        internal static class RoomScreenResources
        {
            public static void Load()
            {
                Plugin.log.Info("Resources loaded");
            }
            static RoomScreenResources()
            {
                try
                {
                    var assembly = Assembly.GetAssembly(typeof(ResourcesStorage));
                    RoomScreen.Add(nameof(Names.DifficultySelectionViewController), GetResourceContent(assembly, Names.DifficultySelectionViewController));
                    RoomScreen.Add(nameof(Names.LevelPacksUIViewController), GetResourceContent(assembly, Names.LevelPacksUIViewController));
                    RoomScreen.Add(nameof(Names.PlayerManagementViewController), GetResourceContent(assembly, Names.PlayerManagementViewController));
                    RoomScreen.Add(nameof(Names.PlayingNowViewController), GetResourceContent(assembly, Names.PlayingNowViewController));
                    RoomScreen.Add(nameof(Names.QuickSettingsViewController), GetResourceContent(assembly, Names.QuickSettingsViewController));
                    RoomScreen.Add(nameof(Names.ResultsViewController), GetResourceContent(assembly, Names.ResultsViewController));
                    RoomScreen.Add(nameof(Names.SongSelectionViewController), GetResourceContent(assembly, Names.SongSelectionViewController));
                }
                catch (Exception ex)
                {
                    Plugin.log.Error(ex);
                }
                foreach (var pair in RoomScreen)
                {
                    if (string.IsNullOrEmpty(pair.Value))
                        throw new NullReferenceException($"{pair.Value} is null.");
                }
            }
            internal class Names
            {
                public static readonly string DifficultySelectionViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.DifficultySelectionViewController";
                public static readonly string LevelPacksUIViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.LevelPacksUIViewController";
                public static readonly string PlayerManagementViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.PlayerManagementViewController";
                public static readonly string PlayingNowViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.PlayingNowViewController";
                public static readonly string QuickSettingsViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.QuickSettingsViewController";
                public static readonly string ResultsViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.ResultsViewController";
                public static readonly string SongSelectionViewController = "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.SongSelectionViewController";
            }
            private static Dictionary<string, string> RoomScreen = new Dictionary<string, string>();
            public static string GetRoomScreenResource(string name)
            {
                var content = RoomScreen[name];
                Plugin.log.Info($"Getting resource {name}. Length is {content.Length}");
                return content;
            }
        }

    }
}
