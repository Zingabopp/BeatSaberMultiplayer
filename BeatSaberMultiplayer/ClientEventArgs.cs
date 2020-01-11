using System;
using Lidgren.Network;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMultiplayerLite.Data;

namespace BeatSaberMultiplayerLite
{
    public class ConnectionStatusChangedEventArgs
        : EventArgs
    {
        public NetConnectionStatus PreviousStatus { get; }
        public NetConnectionStatus NewStatus { get; }
        public ConnectionStatusChangedEventArgs(NetConnectionStatus previousStatus, NetConnectionStatus newStatus)
        {
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
        }
    }

    public class EventMessageReceivedEventArgs
        : EventArgs
    {
        public string Header { get; }
        public string Data { get; }
        public EventMessageReceivedEventArgs(string header, string data)
        {
            Header = header;
            Data = data;
        }
    }

    public class ClientJoinedRoomEventArgs
        : EventArgs
    {

    }

    public class StartLevelEventArgs
        : EventArgs
    {

    }


    public class PlayerInfoUpdatedEventArgs
        : EventArgs
    {
        public ulong[] PlayerIds { get; }
        public PlayerScore[] PlayerScores { get; }
        public bool SpectatorInRoom { get; }
        public bool IsFullUpdate { get; }



        public PlayerInfoUpdatedEventArgs()
        {
            
        }
    }

    public class RoomInfoReceivedEventArgs
        : EventArgs
    {
        public uint RoomId { get; }

        public string RoomName { get; }
        public bool UsePassword { get; }
        public bool PerPlayerDifficulty { get; }
        public bool NoHost { get; }

        public RoomState RoomState { get; }
        public SongSelectionType SongSelectionType { get; }
        public PlayerInfo RoomHost { get; }

        public int PlayersInRoom { get; }
        public int MaxPlayers { get; }

        public bool SongIsSelected => SelectedSong != null && RoomState != RoomState.SelectingSong;

        public LevelOptionsInfo StartLevelInfo { get; }

        public SongInfo SelectedSong { get; }

        public RoomInfoReceivedEventArgs(RoomInfo roomInfo)
        {
            RoomId = roomInfo.roomId;
            RoomName = roomInfo.name;
            UsePassword = roomInfo.usePassword;
            PerPlayerDifficulty = roomInfo.perPlayerDifficulty;
            NoHost = roomInfo.noHost;
            RoomState = roomInfo.roomState;
            SongSelectionType = roomInfo.songSelectionType;
            RoomHost = roomInfo.roomHost;
            PlayersInRoom = roomInfo.players;
            MaxPlayers = roomInfo.maxPlayers;
            StartLevelInfo = roomInfo.startLevelInfo;
            SelectedSong = roomInfo.selectedSong;
        }
    }
}
