﻿using ServerHub.Data;
using ServerHub.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;

namespace ServerHub.Rooms
{
    class Room
    {
        public uint roomId;

        public List<Client> roomClients = new List<Client>();

        private List<PlayerInfo> _readyPlayers = new List<PlayerInfo>();
        private Dictionary<PlayerInfo, SongInfo> _votes = new Dictionary<PlayerInfo, SongInfo>();

        public RoomSettings roomSettings;
        public RoomState roomState;

        public PlayerInfo roomHost;

        public SongInfo selectedSong;
        public byte selectedDifficulty;

        private DateTime _songStartTime;
        private DateTime _resultsStartTime;
        private DateTime _votingStartTime;

        public const float resultsShowTime = 15f;
        public const float votingTime = 30f;

        public Room(uint id, RoomSettings settings, PlayerInfo host)
        {
            roomId = id;
            roomSettings = settings;
            roomHost = host;
        }

        public void StartRoom()
        {
            HighResolutionTimer.LoopTimer.Elapsed += RoomLoop;
            if(roomSettings.SelectionType == SongSelectionType.Voting)
            {
                _votingStartTime = DateTime.Now;
            }
        }

        public void StopRoom()
        {
            HighResolutionTimer.LoopTimer.Elapsed -= RoomLoop;
        }

        public RoomInfo GetRoomInfo()
        {
            return new RoomInfo() { roomId = roomId, name = roomSettings.Name, usePassword = roomSettings.UsePassword, players = roomClients.Count, maxPlayers = roomSettings.MaxPlayers, noFail = roomSettings.NoFail, roomHost = roomHost, roomState = roomState, songSelectionType=roomSettings.SelectionType, selectedSong = selectedSong, selectedDifficulty = selectedDifficulty };
        }

        public byte[] GetSongInfos()
        {
            List<byte> buffer = new List<byte>();

            buffer.AddRange(BitConverter.GetBytes(roomSettings.AvailableSongs.Count));

            roomSettings.AvailableSongs.ForEach(x => buffer.AddRange(x.ToBytes()));

            return buffer.ToArray();
        }

        private void RoomLoop(object sender, HighResolutionTimerElapsedEventArgs e)
        {
            switch (roomState)
            {
                case RoomState.InGame:
                    {
                        if (DateTime.Now.Subtract(_songStartTime).TotalSeconds >= selectedSong.songDuration)
                        {
                            roomState = RoomState.Results;
                            _resultsStartTime = DateTime.Now;
                            List<byte> roomInfoBuffer = new List<byte>();
                            roomInfoBuffer.Add(0);
                            roomInfoBuffer.AddRange(GetRoomInfo().ToBytes(false));
                            BroadcastPacket(new BasePacket(CommandType.GetRoomInfo, roomInfoBuffer.ToArray()));
                        }
                    }
                    break;
                case RoomState.Results:
                    {
                        if (DateTime.Now.Subtract(_resultsStartTime).TotalSeconds >= resultsShowTime)
                        {
                            roomState = RoomState.SelectingSong;
                            selectedSong = null;
                            BroadcastPacket(new BasePacket(CommandType.SetSelectedSong, new byte[0]));
                        }
                    }
                    break;
                case RoomState.SelectingSong:
                    {
                        switch (roomSettings.SelectionType)
                        {
                            case SongSelectionType.Random:
                                {
                                    roomState = RoomState.Preparing;
                                    Random rand = new Random();
                                    selectedSong = roomSettings.AvailableSongs[rand.Next(roomSettings.AvailableSongs.Count)];
                                    BroadcastPacket(new BasePacket(CommandType.SetSelectedSong, selectedSong.ToBytes(false)));
                                    ReadyStateChanged(roomHost, true);
                                }
                                break;
                            case SongSelectionType.Voting:
                                {
                                    if (DateTime.Now.Subtract(_votingStartTime).TotalSeconds >= votingTime)
                                    {
                                        roomState = RoomState.Preparing;
                                        if (_votes.Count > 0)
                                        {
                                            selectedSong = _votes.GroupBy(x => x.Value).OrderByDescending(y => y.Count()).First().Key;
                                        }
                                        else
                                        {
                                            Random rand = new Random();
                                            selectedSong = roomSettings.AvailableSongs[rand.Next(roomSettings.AvailableSongs.Count)];
                                        }
                                        BroadcastPacket(new BasePacket(CommandType.SetSelectedSong, selectedSong.ToBytes(false)));
                                        ReadyStateChanged(roomHost, true);
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }


            List<byte> buffer = new List<byte>();
            switch (roomState)
            {
                case RoomState.SelectingSong:
                    {
                        if(roomSettings.SelectionType == SongSelectionType.Voting)
                        {
                            buffer.AddRange(BitConverter.GetBytes((float)DateTime.Now.Subtract(_votingStartTime).TotalSeconds));
                            buffer.AddRange(BitConverter.GetBytes(votingTime));
                        }
                        else
                        {
                            buffer.AddRange(new byte[8]);
                        }
                    }
                    break;
                case RoomState.Preparing:
                    {
                        buffer.AddRange(new byte[8]);
                    }
                    break;
                case RoomState.InGame:
                    {
                        buffer.AddRange(BitConverter.GetBytes((float)DateTime.Now.Subtract(_songStartTime).TotalSeconds));
                        buffer.AddRange(BitConverter.GetBytes(selectedSong.songDuration));
                    }
                    break;
                case RoomState.Results:
                    {
                        buffer.AddRange(BitConverter.GetBytes((float)DateTime.Now.Subtract(_resultsStartTime).TotalSeconds));
                        buffer.AddRange(BitConverter.GetBytes(resultsShowTime));
                    }
                    break;
            }
            buffer.AddRange(BitConverter.GetBytes(roomClients.Count));
            roomClients.ForEach(x => buffer.AddRange(x.playerInfo.ToBytes()));
            BroadcastPacket(new BasePacket(CommandType.UpdatePlayerInfo, buffer.ToArray()));
        }

        public void BroadcastPacket(BasePacket packet)
        {
            foreach (Client client in roomClients)
            {
                try
                {
                    client.SendData(packet);
                }
                catch (Exception e)
                {
                    Logger.Instance.Warning($"Can't send packet to {client.playerInfo.playerName}! Exception: {e}");
                }
            }
        }

        public void SetSelectedSong(PlayerInfo sender, SongInfo song)
        {
            if (sender.Equals(roomHost))
            {
                selectedSong = song;
                if (selectedSong == null)
                {
                    switch (roomSettings.SelectionType)
                    {
                        case SongSelectionType.Manual:
                            {

                                roomState = RoomState.SelectingSong;
                                BroadcastPacket(new BasePacket(CommandType.SetSelectedSong, new byte[0]));
                                if(roomSettings.SelectionType == SongSelectionType.Voting)
                                {
                                    _votingStartTime = DateTime.Now;
                                }
                            }
                            break;
                        case SongSelectionType.Random:
                            {
                                roomState = RoomState.Preparing;
                                Random rand = new Random();
                                selectedSong = roomSettings.AvailableSongs[rand.Next(0, roomSettings.AvailableSongs.Count - 1)];
                                BroadcastPacket(new BasePacket(CommandType.SetSelectedSong, selectedSong.ToBytes(false)));
                                ReadyStateChanged(roomHost, true);
                            }
                            break;
                    }
                }
                else
                {
                    if (roomSettings.SelectionType == SongSelectionType.Voting)
                    {
                        _votes.Remove(sender);
                        _votes.Add(sender, song);
                    }
                    else
                    {
                        roomState = RoomState.Preparing;
                        BroadcastPacket(new BasePacket(CommandType.SetSelectedSong, selectedSong.ToBytes(false)));
                        ReadyStateChanged(roomHost, true);
                    }
                }
            }
            else
            {
                if (roomSettings.SelectionType == SongSelectionType.Voting)
                {
                    _votes.Remove(sender);
                    _votes.Add(sender, song);
                }
                else
                {
                    Logger.Instance.Warning($"{sender.playerName}:{sender.playerId} tried to select song, but he is not the host");
                }
            }
        }

        public void StartLevel(PlayerInfo sender, byte difficulty, SongInfo song)
        {
            if (sender.Equals(roomHost))
            {
                selectedSong = song;
                selectedDifficulty = difficulty;

                List<byte> buffer = new List<byte>();
                buffer.Add(selectedDifficulty);
                buffer.AddRange(selectedSong.ToBytes(false));

                BroadcastPacket(new BasePacket(CommandType.StartLevel, buffer.ToArray()));

                roomState = RoomState.InGame;
                _songStartTime = DateTime.Now;
            }
            else
            {
                Logger.Instance.Warning($"{sender.playerName}:{sender.playerId} tried to start the level, but he is not the host");
            }
        }

        public void TransferHost(PlayerInfo sender, PlayerInfo newHost)
        {
            if (sender.Equals(roomHost))
            {
                roomHost = newHost;
                BroadcastPacket(new BasePacket(CommandType.TransferHost, roomHost.ToBytes(false)));
            }
            else
            {
                Logger.Instance.Warning($"{sender.playerName}:{sender.playerId} tried to transfer host, but he is not the host");
            }
        }

        public void ReadyStateChanged(PlayerInfo sender, bool ready)
        {
            if (ready)
            {
                if (!_readyPlayers.Contains(sender))
                {
                    _readyPlayers.Add(sender);
                }
            }
            else
            {
                _readyPlayers.Remove(sender);
            }

            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(_readyPlayers.Count));
            buffer.AddRange(BitConverter.GetBytes(roomClients.Count));
            BroadcastPacket(new BasePacket(CommandType.PlayerReady, buffer.ToArray()));
        }

        public void DestroyRoom(PlayerInfo sender)
        {
            if (roomHost.Equals(sender))
            {
                RoomsController.DestroyRoom(roomId);
            }
            else
            {
                Logger.Instance.Warning($"{sender.playerName}:{sender.playerId} tried to destroy the room, but he is not the host");
            }
        }
    }
}
