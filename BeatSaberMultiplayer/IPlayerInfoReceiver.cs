using BeatSaberMultiplayerLite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaberMultiplayerLite
{
    public interface IPlayerInfoReceiver
    {
        bool AcceptingUpdates { get; }
        void SetPlayerInfo(PlayerInfo playerInfo, Vector3 offset, bool isLocal);
        void DestroyReceiver();
    }
}
