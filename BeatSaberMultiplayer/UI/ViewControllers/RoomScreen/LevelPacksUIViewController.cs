﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
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

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            //packsCollectionsControl.SetTexts(new string[] { "OST & EXTRAS", "MUSIC PACKS", "PLAYLISTS", "CUSTOM LEVELS" });

            Initialize();

        }

        public IAnnotatedBeatmapLevelCollection[] GetPlaylists()
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
            _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();
            if (_playerDataModel == null)
            {
                _playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
                _userFavoritesSO = Resources.FindObjectsOfTypeAll<UserFavoritesPlaylistSO>().FirstOrDefault() ??
                                    ScriptableObject.CreateInstance<UserFavoritesPlaylistSO>();
            }
            List<IAnnotatedBeatmapLevelCollection> levelPacksAndPlaylists = new List<IAnnotatedBeatmapLevelCollection>();
            levelPacksAndPlaylists.AddRange(_beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks);
            if(_playerDataModel != null)
            {
                _userFavoritesSO.SetupFromLevelPackCollection(_playerDataModel.playerData.favoritesLevelIds, _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection);
                levelPacksAndPlaylists.Add(_userFavoritesSO);
            }
            //AnnotatedBeatmapLevelCollectionsViewController[] playlistViewControllers = Resources.FindObjectsOfTypeAll<AnnotatedBeatmapLevelCollectionsViewController>();
            //AnnotatedBeatmapLevelCollectionsViewController playlistController = playlistViewControllers?.FirstOrDefault();
            //if (playlistController != null)
            //{
            //    IAnnotatedBeatmapLevelCollection[] playlists = playlistController.GetPrivateField<IAnnotatedBeatmapLevelCollection[]>("_annotatedBeatmapLevelCollections");
            //    if (playlists == null)
            //        Plugin.log.Info($"Found _playlists is null.");
            //    else
            //    {
            //        Plugin.log.Info($"Found {playlists.Length} playlists.");
            //        if (playlists.Length > 0)
            //            levelPacksAndPlaylists.AddRange(playlists);
            //    }
            //}
            //else
            //    Plugin.log.Warn("Couldn't find the PlaylistsViewController.");
            levelPacksAndPlaylists.AddRange(BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists());
            return levelPacksAndPlaylists.ToArray();
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
