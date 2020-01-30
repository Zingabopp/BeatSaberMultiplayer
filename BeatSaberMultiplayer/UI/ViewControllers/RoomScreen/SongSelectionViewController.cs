using BeatSaberMultiplayerLite.Misc;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMultiplayerLite.UI.FlowCoordinators;
using BeatSaberMultiplayerLite.Interop;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen
{
    public enum TopButtonsState { Select, SortBy, Search, Mode };

    class SongSelectionViewController : BSMLResourceViewController, TableView.IDataSource
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);
        public RoomFlowCoordinator ParentFlowCoordinator { get; internal set; }
        private readonly IPAUtilities.PropertyAccessor<TableViewScroller, float>.Setter SetScrollPosition = IPAUtilities.PropertyAccessor<TableViewScroller, float>.GetSetter("position");
        private readonly IPAUtilities.FieldAccessor<TableViewScroller, float>.Accessor TableViewScrollerTargetPosition = IPAUtilities.FieldAccessor<TableViewScroller, float>.GetAccessor("_targetPosition");
        private readonly IPAUtilities.FieldAccessor<TableView, TableViewScroller>.Accessor GetTableViewScroller = IPAUtilities.FieldAccessor<TableView, TableViewScroller>.GetAccessor("_scroller");
        public event Action<IPreviewBeatmapLevel> SongSelected;
        public event Action<string> SearchPressed;
        public event Action<SortMode> SortPressed;
        private ISongDownloader downloader;

        [UIParams]
        BSMLParserParams _parserParams;

        [UIComponent("host-selects-song-text")]
        TextMeshProUGUI _hostIsSelectingSongText;

        [UIComponent("song-list")]
        CustomListTableData _songsTableView;

        [UIComponent("song-list-rect")]
        RectTransform _songListRect;
        [UIComponent("select-btns-rect")]
        RectTransform _selectBtnsRect;
        [UIComponent("sort-btns-rect")]
        RectTransform _sortBtnsRect;

        [UIComponent("search-keyboard")]
        ModalKeyboard _searchKeyboard;

        [UIComponent("diff-sort-btn")]
        Button _diffButton;

        [UIValue("search-value")]
        string searchValue;
        private bool _moreSongsAvailable = true;
        [UIValue("more-btn-active")]
        public bool MoreSongsAvailable
        {
            get { return _moreSongsAvailable; }
            set
            {
                if (_moreSongsAvailable == value)
                    return;
                _moreSongsAvailable = value;
                NotifyPropertyChanged();
            }
        }
        
        private TableViewScroller _songListScroller;
        public TableViewScroller SongListScroller
        {
            get
            {
                if (_songListScroller == null)
                {
                    _songListScroller = GetTableViewScroller(ref _songsTableView.tableView);
                    _songListScroller.positionDidChangeEvent += _songListScroller_positionDidChangeEvent;
                }
                return _songListScroller;
            }
        }
        
        private LevelListTableCell songListTableCellInstance;
        private PlayerDataModelSO _playerDataModel;
        private AdditionalContentModel _additionalContentModel;
        private Action _moreSongsAction;

        List<IPreviewBeatmapLevel> availableSongs = new List<IPreviewBeatmapLevel>();

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            if (firstActivation)
            {
                if (Plugin.DownloaderExists)
                {
                    downloader = new BeatSaverDownloaderInterop();
                    if (downloader == null)
                        Plugin.log.Warn($"{nameof(BeatSaverDownloaderInterop)} could not be created.");
                    else
                    {
                        MoreSongsAvailable = downloader.CanCreate;
                        Plugin.log.Debug($"{nameof(MoreSongsAvailable)} is {MoreSongsAvailable}");
                    }
                }
                else
                {
                    Plugin.log.Warn($"SongDownloader not found, More Songs button won't be created.");
                    MoreSongsAvailable = false;
                }
                _songsTableView.tableView.didSelectCellWithIdxEvent += SongsTableView_DidSelectRow;
                _songsTableView.tableView.dataSource = this;

                _playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
                _additionalContentModel = Resources.FindObjectsOfTypeAll<AdditionalContentModel>().First();
            }

            SelectTopButtons(TopButtonsState.Select);
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            _parserParams.EmitEvent("closeAllMPModals");
            base.DidDeactivate(deactivationType);
        }

        public void SetSongs(List<IPreviewBeatmapLevel> levels)
        {
            if (availableSongs == levels)
                return;
            availableSongs = levels;
            _songsTableView.tableView.ReloadData();
            _songsTableView.tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
            Plugin.log.Debug($"Set list of {levels.Count} levels!");
        }

        public void ClearSelection()
        {
            _songsTableView.tableView.ClearSelection();
        }

        public void ScrollToLevel(string levelId, TableViewScroller.ScrollPositionType scrollType = TableViewScroller.ScrollPositionType.Beginning, bool animated = false)
        {
            if (string.IsNullOrEmpty(levelId))
                return;
            if (availableSongs.Any(x => x.levelID == levelId))
            {
                int row = availableSongs.FindIndex(x => x.levelID == levelId);
                _songsTableView.tableView.ScrollToCellWithIdx(row, scrollType, animated);
            }
        }

        public bool ScrollToPosition(float position, bool animated = false)
        {
            TableViewScroller scroller = SongListScroller;
#if DEBUG
            Plugin.LogLocation($"Scrolling to position: {position} from {scroller.position}");
#endif
            if (position >= 0 && position <= scroller.scrollableSize)
            {
                float offset = 5;
                if (position + offset > scroller.scrollableSize)
                    offset *= -1;
                if (position + offset < 0)
                    offset = 0;
                //Plugin.log.Debug($"Scrolling to ({position} - {offset}) / {scroller.scrollableSize}");
                if (!animated)
                {
                    SetScrollPosition(ref scroller, position + offset);
                }
                TableViewScrollerTargetPosition(ref scroller) = position; // Need this so the list isn't blank when returning from a song

                scroller.enabled = true;
                return true;
            }
            else
                Plugin.log.Warn($"Scroll position out of range: {position} / {scroller.scrollableSize}");
            return false;
        }

        public void ScrollToIndex(int index, TableViewScroller.ScrollPositionType scrollType = TableViewScroller.ScrollPositionType.Beginning, bool animated = false)
        {
            if (index >= 0 && index < _songsTableView.tableView.numberOfCells)
            {
                _songsTableView.tableView.ScrollToCellWithIdx(index, scrollType, animated);
            }
        }

        public void UpdateViewController(bool isHost)
        {
            _songListRect.gameObject.SetActive(isHost);
            _hostIsSelectingSongText.gameObject.SetActive(!isHost);
        }

        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _selectBtnsRect.gameObject.SetActive(true);
                        _sortBtnsRect.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _selectBtnsRect.gameObject.SetActive(false);
                        _sortBtnsRect.gameObject.SetActive(true);

                        //_diffButton.interactable = ScrappedData.Downloaded;
                    }; break;
                case TopButtonsState.Search:
                    {
                        _selectBtnsRect.gameObject.SetActive(false);
                        _sortBtnsRect.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.Mode:
                    {
                        _selectBtnsRect.gameObject.SetActive(false);
                        _sortBtnsRect.gameObject.SetActive(false);
                    }; break;
            }
        }

        [UIAction("sort-btn-pressed")]
        public void SortButtonPressed()
        {
            SelectTopButtons(TopButtonsState.SortBy);
        }

        [UIAction("search-btn-pressed")]
        public void SearchButtonPressed()
        {
            SelectTopButtons(TopButtonsState.Select);
            _searchKeyboard.modalView.Show(true);
        }

        [UIAction("more-btn-pressed")]
        public void MoreButtonPressed()
        {
            downloader?.PresentDownloaderFlowCoordinator(ParentFlowCoordinator, MoreSongsFinishedCallback);
        }

        int LastRandomSong;
        [UIAction("random-btn-pressed")]
        public void RandomButtonPressed()
        {
            int songIndex = UnityEngine.Random.Range(0, availableSongs.Count - 1);//Plugin.log.Warn($"Position for cell size {_songsTableView.tableView.cellSize} is {songIndex * _songsTableView.tableView.cellSize}");
            ScrollToIndex(songIndex, TableViewScroller.ScrollPositionType.Center, false);
            if (LastRandomSong == songIndex)
                return;
            Plugin.log.Debug($"Random song: {availableSongs[songIndex].songName}");

            _songsTableView.tableView.SelectCellWithIdx(songIndex, true);
            LastRandomSong = songIndex;
        }

        public void MoreSongsFinishedCallback()
        {
            Plugin.log.Info("More Songs Finished");
        }

        [UIAction("search-pressed")]
        public void SearchStartPressed(string value)
        {
            SelectTopButtons(TopButtonsState.Select);
            _searchKeyboard.modalView.Hide(true);
            SearchPressed?.Invoke(value);
        }

        [UIAction("def-sort-btn-pressed")]
        public void DefaultSotrPressed()
        {
            SelectTopButtons(TopButtonsState.Select);
            SortPressed?.Invoke(SortMode.Default);
        }

        [UIAction("new-sort-btn-pressed")]
        public void NewestSotrPressed()
        {
            SelectTopButtons(TopButtonsState.Select);
            SortPressed?.Invoke(SortMode.Newest);
        }

        [UIAction("diff-sort-btn-pressed")]
        public void DifficultySotrPressed()
        {
            SelectTopButtons(TopButtonsState.Select);
            SortPressed?.Invoke(SortMode.Difficulty);
        }

        private void SongsTableView_DidSelectRow(TableView sender, int row)
        {
            SongSelected?.Invoke(availableSongs[row]);
        }

        public float CellSize()
        {
            return 10f;
        }

        public int NumberOfCells()
        {
            return availableSongs.Count;
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

            tableCell.SetDataFromLevelAsync(availableSongs[idx], _playerDataModel.playerData.favoritesLevelIds.Contains(availableSongs[idx].levelID));
            tableCell.RefreshAvailabilityAsync(_additionalContentModel, availableSongs[idx].levelID);
            
            tableCell.reuseIdentifier = _songsTableView.reuseIdentifier;
            return tableCell;
        }


        private void _songListScroller_positionDidChangeEvent(TableViewScroller arg1, float arg2)
        {
            ParentFlowCoordinator.LastScrollPosition = arg2;
        }

        [UIAction("fast-scroll-down-pressed")]
        private void FastScrollDown()
        {
            for (int i = 0; i < 5; i++)
                _parserParams.EmitEvent("song-list#PageDown");
        }

        [UIAction("fast-scroll-up-pressed")]
        private void FastScrollUp()
        {
            for (int i = 0; i < 5; i++)
                _parserParams.EmitEvent("song-list#PageUp");
        }

    }
}
