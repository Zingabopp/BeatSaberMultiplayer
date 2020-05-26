using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMultiplayerLite.Data;
using IPA.Utilities;
using BeatSaberMultiplayerLite.Misc;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen
{
    public class RequestsViewController : BSMLResourceViewController, TableView.IDataSource
    {
        #region Accessors
        private static readonly FieldAccessor<LevelListTableCell, TextMeshProUGUI>.Accessor LevelListTableCell_SongNameText = FieldAccessor<LevelListTableCell, TextMeshProUGUI>.GetAccessor("_songNameText");
        private static readonly FieldAccessor<LevelListTableCell, TextMeshProUGUI>.Accessor LevelListTableCell_AuthorText = FieldAccessor<LevelListTableCell, TextMeshProUGUI>.GetAccessor("_authorText");
        private static readonly FieldAccessor<LevelListTableCell, RawImage>.Accessor LevelListTableCell_CoverImage = FieldAccessor<LevelListTableCell, RawImage>.GetAccessor("_coverRawImage");
        private static readonly FieldAccessor<LevelListTableCell, RawImage>.Accessor LevelListTableCell_BadgeImage = FieldAccessor<LevelListTableCell, RawImage>.GetAccessor("_favoritesBadgeImage");
        private static readonly FieldAccessor<LevelListTableCell, Image[]>.Accessor LevelListTableCell_CharImages = FieldAccessor<LevelListTableCell, Image[]>.GetAccessor("_beatmapCharacteristicImages");
        #endregion
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action BackPressed;
        public event Action<SongInfo> SongSelected;
        public event Action<SongInfo> RemovePressed;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0649 // Field is never assigned to
        [UIComponent("song-list")]
        private CustomListTableData _songsTableView;

        [UIComponent("play-btn")]
        private Button _playButton;
        [UIComponent("remove-btn")]
        private Button _removeButton;
#pragma warning restore CS0649 // Field is never assigned to
#pragma warning restore CS0169 // Field is never used
#pragma warning restore IDE0044 // Add readonly modifier

        private LevelListTableCell songListTableCellInstance;
        private AdditionalContentModel _additionalContentModel;
        private BeatmapLevelsModel _beatmapLevelsModel;

        List<SongInfo> requestedSongs = new List<SongInfo>();
        SongInfo _selectedSong;

        private IEnumerable<IPreviewBeatmapLevel> _allBeatmaps;


        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            if (firstActivation)
            {
                _songsTableView.tableView.didSelectCellWithIdxEvent += SongsTableView_DidSelectRow;
                _songsTableView.tableView.dataSource = this;

                _additionalContentModel = Resources.FindObjectsOfTypeAll<AdditionalContentModel>().First();
                _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();
            }

            _selectedSong = null;
            _playButton.interactable = false;
            _removeButton.interactable = false;
        }

        public void SetSongs(List<SongInfo> songs)
        {
            requestedSongs = songs;

            _allBeatmaps = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels);

            _songsTableView.tableView.ReloadData();

            if (_selectedSong != null)
            {

                int index = requestedSongs.FindIndex(x => x.hash == _selectedSong.hash);

                if (index != -1)
                {
                    _songsTableView.tableView.ScrollToCellWithIdx(index, TableViewScroller.ScrollPositionType.Beginning, false);
                    _songsTableView.tableView.SelectCellWithIdx(index, false);
                }
                else
                {
                    _songsTableView.tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
                    _selectedSong = null;
                    _playButton.interactable = false;
                    _removeButton.interactable = false;
                }
            }
            else
            {
                _songsTableView.tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
                _playButton.interactable = false;
                _removeButton.interactable = false;
            }

            Plugin.log.Debug($"Set list of {songs.Count} songs!");
        }

        [UIAction("back-pressed")]
        public void BackButtonPressed()
        {
            BackPressed?.Invoke();
        }

        [UIAction("play-pressed")]
        public void PlayButtonPressed()
        {
            SongSelected?.Invoke(_selectedSong);
        }

        [UIAction("remove-pressed")]
        public void RemoveButtonPressed()
        {
            RemovePressed?.Invoke(_selectedSong);
        }

        private void SongsTableView_DidSelectRow(TableView arg1, int arg2)
        {
            _selectedSong = requestedSongs[arg2];
            _playButton.interactable = true;
            _removeButton.interactable = true;
        }

        public float CellSize()
        {
            return 10f;
        }

        public int NumberOfCells()
        {
            return requestedSongs.Count;
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
            LevelListTableCell tableCell = (LevelListTableCell)tableView.DequeueReusableCellForIdentifier(_songsTableView.reuseIdentifier);
            if (!tableCell)
            {
                if (songListTableCellInstance == null)
                    songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                tableCell = Instantiate(songListTableCellInstance);
            }

            var level = _allBeatmaps.FirstOrDefault(x => x.levelID == requestedSongs[idx].levelId);

            if (level != null)
            {
                tableCell.SetDataFromLevelAsync(level, false);
                tableCell.RefreshAvailabilityAsync(_additionalContentModel, level.levelID);
            }
            else
            {
                TextMeshProUGUI songNameText = LevelListTableCell_SongNameText(ref tableCell);
                TextMeshProUGUI authorNameText = LevelListTableCell_AuthorText(ref tableCell);
                songNameText.text = string.Format("{0} <size=80%>{1}</size>", requestedSongs[idx].songName, requestedSongs[idx].songSubName);
                authorNameText.text = "Loading info...";

                RawImage coverImage = LevelListTableCell_CoverImage(ref tableCell);
                coverImage.texture = null;
                coverImage.color = Color.clear;

                LevelListTableCell_BadgeImage(ref tableCell).enabled = false;

                Image[] chars = LevelListTableCell_CharImages(ref tableCell);

                foreach (Image img in chars)
                {
                    img.enabled = false;
                }

                SongDownloader.Instance.RequestSongByLevelID(requestedSongs[idx].hash, (info, errorMsg) =>
                {
                    // TODO: Better null handling?
                    if (info == null)
                    {
                        Plugin.log.Warn($"Error in RequestSongByLevelId: {errorMsg}");
                        return;
                    }
                    songNameText.text = string.Format("{0} <size=80%>{1}</size>", info.songName, info.songSubName);
                    authorNameText.text = info.songAuthorName;

                    StartCoroutine(LoadScripts.LoadSpriteCoroutine(info.coverURL, (cover) =>
                    {
                        coverImage.texture = cover;
                        coverImage.color = Color.white;
                    }));
                });
            }

            tableCell.reuseIdentifier = _songsTableView.reuseIdentifier;
            return tableCell;
        }
    }
}
