extern alias ScoreSaberGlobal;
using ScoreSaberGlobal.ScoreSaber;
using System;

namespace BeatSaberMultiplayerLite.Interop
{
    internal static class ScoreSaberInterop
    {
        public static void InitAndSignIn()
        {
            try
            {
                Handler.instance.Initialize();
                Handler.instance.SignIn();
            }
            catch(Exception e)
            {
                Plugin.log.Error($"Error signing into ScoreSaber, score submission unavailble: {e.Message}");
                Plugin.log.Debug(e);
            }
        }
    }
}
