﻿using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMultiplayerLite.UI.FlowCoordinators;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.CreateRoomScreen
{
    class RoomCreationServerHubsListViewController : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action<ServerHubClient> selectedServerHub;

#pragma warning disable CS0649
        [UIComponent("hubs-list")]
        public CustomCellListTableData hubsList;
#pragma warning restore CS0649

        [UIValue("hubs")]
        public List<object> hubInfosList = new List<object>();

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            hubsList.tableView.ClearSelection();
        }

        public void SetServerHubs(List<ServerHubClient> hubs)
        {
            hubInfosList.Clear();

            if (hubs != null)
            {
                var availableHubs = hubs.OrderByDescending(x => x.serverHubCompatible ? 2 : (x.serverHubAvailable ? 1 : 0)).ThenBy(x => x.ping).ThenBy(x => x.availableRoomsCount).ToList();

                foreach (ServerHubClient room in availableHubs)
                {
                    hubInfosList.Add(new ServerHubListObject(room));
                }
            }

            hubsList.tableView.ReloadData();
        }

        [UIAction("hub-selected")]
        private void RoomSelected(TableView sender, ServerHubListObject obj)
        {
            if (obj.hub.serverHubCompatible)
                selectedServerHub?.Invoke(obj.hub);
        }
    }

    public class ServerHubListObject
    {
        public ServerHubClient hub;

#pragma warning disable CS0649
        [UIValue("hub-name")]
        private string hubName;

        [UIValue("hub-state")]
        private string hubStateString;

        [UIComponent("bg")]
        private RawImage background;

        [UIComponent("hub-state-text")]
        private TextMeshProUGUI hubStateText;
#pragma warning restore CS0649

        public ServerHubListObject(ServerHubClient hub)
        {
            this.hub = hub; 
            if (!string.IsNullOrEmpty(hub.serverHubName))
                hubName = $"{(!hub.serverHubCompatible ? (hub.serverHubAvailable ? "<color=yellow>" : "<color=red>") : "")}{hub.serverHubName}";
            else
                hubName = $"{(!hub.serverHubCompatible ? (hub.serverHubAvailable ? "<color=yellow>" : "<color=red>") : "")}{hub.ip}:{hub.port}";

            if (hub.serverHubCompatible)
                hubStateString = $"{hub.playersCount} players, {hub.availableRoomsCount} rooms,  ping: {Mathf.RoundToInt(hub.ping * 1000)}";
            else
                hubStateString = $"{(hub.serverHubAvailable ? "VERSION MISMATCH" : "SERVER DOWN")}";
        }

        [UIAction("refresh-visuals")]
        public void Refresh(bool selected, bool highlighted)
        {
            background.texture = Sprites.whitePixel.texture;
            background.color = new Color(1f, 1f, 1f, 0.125f);
            hubStateText.color = new Color(0.65f, 0.65f, 0.65f, 1f);
        }
    }
}
