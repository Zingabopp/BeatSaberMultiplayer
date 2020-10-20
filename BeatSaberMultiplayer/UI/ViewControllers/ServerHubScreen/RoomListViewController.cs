﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMultiplayerLite.Data;
using BeatSaberMultiplayerLite.UI.FlowCoordinators;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.ServerHubScreen
{
    class RoomListViewController : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action createRoomButtonPressed;
        public event Action<ServerHubRoom, string> selectedRoom;
        public event Action refreshPressed;
#pragma warning disable CS0649
        [UIParams]
        private BSMLParserParams parserParams;

        [UIComponent("refresh-btn")]
        private Button _refreshButton;

        [UIComponent("rooms-list")]
        public CustomCellListTableData roomsList;

        [UIComponent("password-keyboard")]
        ModalKeyboard _passwordKeyboard;

        [UIComponent("hubs-text")]
        TextMeshProUGUI _hubsCountText;
        [UIComponent("no-rooms-text")]
        TextMeshProUGUI _noRoomsText;

        [UIValue("rooms")]
        public List<object> roomInfosList = new List<object>();
#pragma warning restore CS0649
        private ServerHubRoom _selectedRoom;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                _hubsCountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 35f;
            }

            roomsList.tableView.ClearSelection();
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            parserParams.EmitEvent("closeAllMPModals");
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        public void SetRooms(List<ServerHubRoom> rooms)
        {
            roomInfosList.Clear();

            if (rooms != null)
            {
                var availableRooms = rooms
                    .OrderBy(y => y.roomInfo.usePassword)
                    .ThenBy(y => y.roomInfo.players == y.roomInfo.maxPlayers)
                    .ThenByDescending(y => y.roomInfo.players);

                foreach (ServerHubRoom room in availableRooms)
                {
                    roomInfosList.Add(new RoomListObject(room));
                }
            }

            if (rooms == null || rooms.Count == 0)
                _noRoomsText.enabled = true;
            else
                _noRoomsText.enabled = false;

            roomsList.tableView.ReloadData();

        }

        public void SetServerHubsCount(int online, int total)
        {
            _hubsCountText.text = $"  Hubs online: {online}/{total}";

        }

        public void SetRefreshButtonState(bool enabled)
        {
            _refreshButton.interactable = enabled;
        }

        [UIAction("room-selected")]
        private void RoomSelected(TableView sender, RoomListObject obj)
        {
            if (!obj.room.roomInfo.usePassword)
            {
                selectedRoom?.Invoke(obj.room, null);
            }
            else
            {
                _selectedRoom = obj.room;
                _passwordKeyboard.modalView.Show(true);
            }
        }

        [UIAction("join-pressed")]
        private void PasswordEntered(string pass)
        {
            selectedRoom?.Invoke(_selectedRoom, pass);
        }

        [UIAction("create-room-btn-pressed")]
        private void CreateRoomBtnPressed()
        {
            createRoomButtonPressed?.Invoke();
        }

        [UIAction("refresh-btn-pressed")]
        private void RefreshBtnPressed()
        {
            refreshPressed?.Invoke();
        }

        public class RoomListObject
        {
            public ServerHubRoom room;

#pragma warning disable CS0649
            [UIValue("room-name")]
            private string roomName;

            [UIValue("room-state")]
            private string roomStateString;

            [UIComponent("locked-icon")]
            private RawImage lockedIcon;
            private bool locked;

            [UIComponent("bg")]
            private RawImage background;

            [UIComponent("room-state-text")]
            private TextMeshProUGUI roomStateText;
#pragma warning restore CS0649

            public RoomListObject(ServerHubRoom room)
            {
                this.room = room;
                roomName = $"({room.roomInfo.players}/{((room.roomInfo.maxPlayers == 0) ? "INF" : room.roomInfo.maxPlayers.ToString())}) {room.roomInfo.name}";
                switch (room.roomInfo.roomState)
                {
                    case RoomState.InGame:
                        roomStateString = "In game";
                        break;
                    case RoomState.Preparing:
                        roomStateString = "Preparing";
                        break;
                    case RoomState.Results:
                        roomStateString = "Results";
                        break;
                    case RoomState.SelectingSong:
                        roomStateString = "Selecting song";
                        break;
                    default:
                        roomStateString = room.roomInfo.roomState.ToString();
                        break;
                }
                locked = room.roomInfo.usePassword;
            }

            [UIAction("refresh-visuals")]
            public void Refresh(bool selected, bool highlighted)
            {
                lockedIcon.texture = Sprites.lockedRoomIcon.texture;
                lockedIcon.enabled = locked;
                background.texture = Sprites.whitePixel.texture;
                background.color = new Color(1f, 1f, 1f, 0.125f);
                roomStateText.color = new Color(0.65f, 0.65f, 0.65f, 1f);
            }
        }
    }
}
