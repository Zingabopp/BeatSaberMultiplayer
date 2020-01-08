using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components;
using System;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using UnityEngine;
using System.Collections.Generic;
using BS_Utils.Utilities;

namespace BeatSaberMultiplayer.UI.ViewControllers.RoomScreen
{
    class LevelPacksUIViewController : BSMLViewController
    {
        public string ResourceName => "BeatSaberMultiplayer.UI.ViewControllers.RoomScreen.LevelPacksUIViewController";
        public override string Content => ResourcesStorage.RoomScreenResources.GetRoomScreenResource(nameof(LevelPacksUIViewController));

        public event Action<IAnnotatedBeatmapLevelCollection> packSelected;

        [UIComponent("packs-list-table")]
        CustomListTableData levelPacksTableData;

        //[UIComponent("packs-collections-control")]
        //TextSegmentedControl packsCollectionsControl;

        private bool _initialized;
        private BeatmapLevelsModel _beatmapLevelsModel;
        private IAnnotatedBeatmapLevelCollection[] _visiblePacks;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            //packsCollectionsControl.SetTexts(new string[] { "OST & EXTRAS", "MUSIC PACKS", "PLAYLISTS", "CUSTOM LEVELS" });

            Initialize();

        }

        public IAnnotatedBeatmapLevelCollection[] GetAllPlaylists()
        {
            //try
            //{
            //    List<IAnnotatedBeatmapLevelCollection> levelPacksAndPlaylists = new List<IAnnotatedBeatmapLevelCollection>();
            //    var beatmapLevelsModels = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>()?.ToArray();
            //    PlaylistCore.PlaylistCore.instance.LoadedPlaylistSO.First().setup
            //    Plugin.log.Info($"BeatmapLevelsModels count: {beatmapLevelsModels.Length}");
            //    foreach (var levelsModel in beatmapLevelsModels)
            //    {
            //        Plugin.log.Info($"LevelsModel: {levelsModel.name}");
            //        levelsModel.UpdateAllLoadedBeatmapLevelPacks();
            //        var beatmapLevelPackCollection = levelsModel.GetPrivateField<IBeatmapLevelPackCollection>("_allLoadedBeatmapLevelPackCollection");
            //        if (beatmapLevelPackCollection?.beatmapLevelPacks != null)
            //        {
            //            Plugin.log.Info($"  beatmapLevelPack count: {beatmapLevelPackCollection.beatmapLevelPacks.Length}");
            //            foreach (var item in beatmapLevelPackCollection.beatmapLevelPacks)
            //            {
            //                Plugin.log.Warn($"     Level Pack: {item.collectionName}");
            //            }
            //        }
            //        else
            //            Plugin.log.Info($"  beatmapLevelPack is null.");
            //    }
            //}catch(Exception ex)
            //{
            //    Plugin.log.Error(ex);
            //}
            return null;
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (_beatmapLevelsModel == null)
                _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();
            GetAllPlaylists();
            List<IAnnotatedBeatmapLevelCollection> levelPacksAndPlaylists = new List<IAnnotatedBeatmapLevelCollection>();
            levelPacksAndPlaylists.AddRange(_beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks);
            var playlistViewControllers = Resources.FindObjectsOfTypeAll<PlaylistsViewController>();
            Plugin.log.Info($"Found {playlistViewControllers?.Length ?? 0} PlaylistsViewControllers");
            var playlistController = playlistViewControllers?.FirstOrDefault();
            if (playlistController != null)
            {
                IAnnotatedBeatmapLevelCollection[] playlists = playlistController.GetPrivateField<IAnnotatedBeatmapLevelCollection[]>("_playlists");
                if (playlists == null)
                    Plugin.log.Info($"Found _playlists is null.");
                else
                {
                    Plugin.log.Info($"Found {playlists.Length} playlists.");
                    if (playlists.Length > 0)
                        levelPacksAndPlaylists.AddRange(playlists);
                }
            }
            else
                Plugin.log.Warn("Couldn't find the PlaylistsViewController.");
            _visiblePacks = levelPacksAndPlaylists.ToArray();

            SetPacks(_visiblePacks);

            if (_visiblePacks.Length > 0)
                packSelected?.Invoke(_visiblePacks[0]);

            this._initialized = true;
        }

        public void SetPacks(IAnnotatedBeatmapLevelCollection[] packs)
        {
            levelPacksTableData.data.Clear();
            Plugin.log.Debug($"{packs.Length} level packs found in LevelPacksUIViewController.SetPacks()");
            foreach (var pack in packs)
            {
                levelPacksTableData.data.Add(new CustomListTableData.CustomCellInfo(pack.collectionName, $"{pack.beatmapLevelCollection.beatmapLevels.Length} levels", pack.coverImage.texture));
            }

            levelPacksTableData.tableView.ReloadData();
        }

        [UIAction("pack-selected")]
        public void PackSelected(TableView sender, int index)
        {
            packSelected?.Invoke(_visiblePacks[index]);
        }
    }
}
