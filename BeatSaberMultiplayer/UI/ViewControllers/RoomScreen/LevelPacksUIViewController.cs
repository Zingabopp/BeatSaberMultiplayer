using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMultiplayerLite.Interop;
using BS_Utils.Utilities;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen
{
    class LevelPacksUIViewController : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action<IAnnotatedBeatmapLevelCollection> packSelected;

#pragma warning disable CS0649
        [UIComponent("packs-list-table")]
        CustomListTableData levelPacksTableData;
#pragma warning restore CS0649

        //[UIComponent("packs-collections-control")]
        //TextSegmentedControl packsCollectionsControl;

        private BeatmapLevelsModel _beatmapLevelsModel;
        private PlayerDataModel _playerDataModel;
        private UserFavoritesPlaylistSO _userFavoritesSO;
        private IAnnotatedBeatmapLevelCollection[] _visiblePacks;
        private IPlaylistLoader PlaylistLoader;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            //packsCollectionsControl.SetTexts(new string[] { "OST & EXTRAS", "MUSIC PACKS", "PLAYLISTS", "CUSTOM LEVELS" });

            SetPlaylistLoader();
            Initialize();
        }

        protected void SetPlaylistLoader()
        {
            if (IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null)
            {
                PlaylistLoader = new BeatSaberPlaylistsLibLoader();
            }
            else
            {
                PlaylistLoader = new FallbackPlaylistLoader();
            }
        }

        public IAnnotatedBeatmapLevelCollection[] GetPlaylists()
        {
            _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();

            List<IAnnotatedBeatmapLevelCollection> levelPacksAndPlaylists = new List<IAnnotatedBeatmapLevelCollection>();
            levelPacksAndPlaylists.AddRange(_beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks);

            IAnnotatedBeatmapLevelCollection[] customPlaylists = PlaylistLoader?.GetCustomPlaylists() ?? Array.Empty<IAnnotatedBeatmapLevelCollection>();
            if (PlaylistLoader != null)
            {
                customPlaylists = PlaylistLoader.GetCustomPlaylists();
            }
            else
            {
                Plugin.log?.Warn($"PlaylistLoader is null, unable to load custom playlists."); 
                customPlaylists = Array.Empty<IAnnotatedBeatmapLevelCollection>();
            }

            if (!(PlaylistLoader?.IncludesBasePlaylists ?? false) || customPlaylists.Length == 0)
            {
                IPlaylist favorites = GetFavoritesPlaylist();
                if (favorites != null)
                    levelPacksAndPlaylists.Add(favorites);
            }

            if (customPlaylists.Length > 0)
            {
                levelPacksAndPlaylists.AddRange(PlaylistLoader.GetCustomPlaylists());
                Plugin.log?.Debug($"{customPlaylists.Length} custom playlists loaded.");
            }
            else
            {
                Plugin.log?.Debug($"No custom playlists loaded.");
            }
            return levelPacksAndPlaylists.ToArray();
        }

        public IPlaylist GetFavoritesPlaylist()
        {
            if (_playerDataModel == null)
            {
                _playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
            }

            if (_userFavoritesSO == null && _playerDataModel != null)
            {
                _userFavoritesSO = Resources.FindObjectsOfTypeAll<UserFavoritesPlaylistSO>().FirstOrDefault();
                if (_userFavoritesSO == null)
                {
                    // TODO: Setup like this doesn't get the favorites cover image, but the above call to get the existing UserFavoritesPlaylistSO object seems to work fine.
                    _userFavoritesSO = ScriptableObject.CreateInstance<UserFavoritesPlaylistSO>();
                }
            }
            if (_userFavoritesSO != null && _playerDataModel != null && _beatmapLevelsModel != null)
            {
                _userFavoritesSO.SetupFromLevelPackCollection(_playerDataModel.playerData.favoritesLevelIds, _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection);
                return _userFavoritesSO;
            }
            return null;
        }

        public void Initialize()
        {

            //if (_initialized)
            //{
            //    return;
            //}

            if (_beatmapLevelsModel == null)
                _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();

            _visiblePacks = GetPlaylists();
            try
            {
                SetPacks(_visiblePacks);
                bool packWasSelected = false;
                if (_visiblePacks.Length > lastSelectedPackIndex)
                {
                    if (_visiblePacks[lastSelectedPackIndex].collectionName == lastSelectedPackName)
                    {
                        SelectPack(lastSelectedPackIndex);
                        packWasSelected = true;
                    }
                    else
                    {
                        for (int i = 0; i < _visiblePacks.Length; i++)
                            if (_visiblePacks[i].collectionName == lastSelectedPackName)
                            {
                                SelectPack(i);
                                packWasSelected = true;
                            }
                    }

                }
                if (!packWasSelected && _visiblePacks.Length > 0)
                    SelectPack(0);
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error initializing LevelPacksUIViewController: {ex.Message}");
                Plugin.log.Debug(ex);
            }
        }

        string lastSelectedPackName = string.Empty;
        int lastSelectedPackIndex = 0;

        public void SetPacks(IAnnotatedBeatmapLevelCollection[] packs)
        {
            levelPacksTableData.data.Clear();
            Plugin.log.Debug($"{packs.Length} level packs found in LevelPacksUIViewController.SetPacks()");
            foreach (IAnnotatedBeatmapLevelCollection pack in packs)
            {
                levelPacksTableData.data.Add(new CustomListTableData.CustomCellInfo(pack.collectionName, $"{pack.beatmapLevelCollection.beatmapLevels.Length} levels", pack.coverImage?.texture));
            }

            levelPacksTableData.tableView.ReloadData();
        }

        public void SelectPack(int index)
        {
            try
            {
                PackSelected(levelPacksTableData.tableView, index);
                levelPacksTableData.tableView.SelectCellWithIdx(index);
                lastSelectedPackIndex = index;
                lastSelectedPackName = _visiblePacks[index].collectionName;
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error selecting level pack with index {index}: {ex.Message}");
                Plugin.log.Debug(ex);
            }
        }

        [UIAction("pack-selected")]
        public void PackSelected(TableView sender, int index)
        {
            packSelected?.Invoke(_visiblePacks[index]);
            lastSelectedPackIndex = index;
            lastSelectedPackName = _visiblePacks[index].collectionName;
        }
    }
}
