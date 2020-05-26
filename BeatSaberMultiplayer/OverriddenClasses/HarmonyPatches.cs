﻿using BeatSaberMultiplayerLite.Data;
using HarmonyLib;
using IPA.Utilities;
using System;
using System.Reflection;

namespace BeatSaberMultiplayerLite.OverriddenClasses
{
    public static class HarmonyPatcher
    {
        internal static Harmony harmony;
        public static void PatchAll()
        {
            Plugin.log.Debug($"Applying Harmony patches");
            BindingFlags allBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if(harmony == null)
              harmony = new Harmony(Plugin.HarmonyId);
            MethodInfo original = typeof(BeatmapObjectManager).GetMethod(nameof(BeatmapObjectManager.HandleNoteWasCut), allBindingFlags);
            HarmonyMethod prefix = new HarmonyMethod(typeof(SpectatorNoteWasCutEventPatch).GetMethod("Prefix", allBindingFlags));
            HarmonyMethod postfix = null;
            ApplyPatch(harmony, original, prefix, postfix);
            
            original = typeof(BeatmapObjectManager).GetMethod(nameof(BeatmapObjectManager.HandleNoteWasMissed), allBindingFlags);
            prefix = new HarmonyMethod(typeof(SpectatorNoteWasMissedEventPatch).GetMethod("Prefix", allBindingFlags));
            postfix = null;
            ApplyPatch(harmony, original, prefix, postfix);

            original = typeof(GameEnergyCounter).GetMethod(nameof(GameEnergyCounter.AddEnergy), allBindingFlags);
            prefix = new HarmonyMethod(typeof(SpectatorGameEnergyCounterPatch).GetMethod("Prefix", allBindingFlags));
            postfix = null;
            ApplyPatch(harmony, original, prefix, postfix);

            original = typeof(PauseController).GetMethod(nameof(PauseController.Pause), allBindingFlags);
            prefix = new HarmonyMethod(typeof(GameplayManagerPausePatch).GetMethod("Prefix", allBindingFlags));
            postfix = null;
            ApplyPatch(harmony, original, prefix, postfix);

            //original = typeof(PauseMenuManager).GetMethod(nameof(PauseMenuManager.ContinueButtonPressed), allBindingFlags);
            //prefix = new HarmonyMethod(typeof(PauseMenuManagerContinueButtonPressed).GetMethod("Prefix", allBindingFlags));
            //postfix = null;
            //ApplyPatch(harmony, original, prefix, postfix);
        }

        public static bool ApplyPatch(Harmony harmony, MethodInfo original, HarmonyMethod prefix = null, HarmonyMethod postfix = null)
        {
            try
            {
                string patchTypeName = null;
                if (prefix != null)
                    patchTypeName = prefix.method.DeclaringType?.Name;
                else if (postfix != null)
                    patchTypeName = postfix.method.DeclaringType?.Name;
                Plugin.log.Debug($"Harmony patching {original.Name} with {patchTypeName}");
                harmony.Patch(original, prefix, postfix);
                return true;
            }
            catch (Exception e)
            {
                Plugin.log.Error($"Unable to patch method {original.Name}: {e.Message}");
                Plugin.log.Debug(e);
                return false;
            }
        }
    }
    [HarmonyPatch(typeof(BeatmapObjectManager))]
    [HarmonyPatch(nameof(BeatmapObjectManager.HandleNoteWasCut))]
    [HarmonyPatch(new Type[] { typeof(NoteController), typeof(NoteCutInfo) })]
    class SpectatorNoteWasCutEventPatch
    {
        #region Property Setters
        private static readonly PropertyAccessor<NoteCutInfo, bool>.Setter SetCutTooSoon = PropertyAccessor<NoteCutInfo, bool>.GetSetter("wasCutTooSoon");
        private static readonly PropertyAccessor<NoteCutInfo, bool>.Setter SetDirectionOk = PropertyAccessor<NoteCutInfo, bool>.GetSetter("directionOK");
        private static readonly PropertyAccessor<NoteCutInfo, bool>.Setter SetSaberTypeOK = PropertyAccessor<NoteCutInfo, bool>.GetSetter("saberTypeOK");
        private static readonly PropertyAccessor<NoteCutInfo, bool>.Setter SetSpeedOK = PropertyAccessor<NoteCutInfo, bool>.GetSetter("speedOK");
        #endregion
        static bool Prefix(BeatmapObjectManager __instance, NoteController noteController, NoteCutInfo noteCutInfo)
        {
            try
            {
                if (Config.Instance.SocialSettings.SpectatorMode && SpectatingController.Instance != null && SpectatingController.active && Client.Instance != null && Client.Instance.connected && SpectatingController.Instance.spectatedPlayer != null && SpectatingController.Instance.spectatedPlayer.playerInfo != null)
                {
                    ulong playerId = SpectatingController.Instance.spectatedPlayer.playerInfo.playerId;

                    if (SpectatingController.Instance.playerUpdates.ContainsKey(playerId) && SpectatingController.Instance.playerUpdates[playerId].hits.Count > 0)
                    {
                        if (SpectatingController.Instance.playerUpdates[playerId].hits.TryGetValue(noteController.noteData.id, out HitData hit))
                        {
                            bool allIsOKExpected = hit.noteWasCut && hit.speedOK && hit.saberTypeOK && hit.directionOK && !hit.wasCutTooSoon;

                            if (hit.noteWasCut)
                            {
                                if (noteCutInfo.allIsOK == allIsOKExpected)
                                {
                                    return true;
                                }
                                else if (!noteCutInfo.allIsOK && allIsOKExpected)
                                {
#if DEBUG
                                Plugin.log.Warn("Oopsie, we missed it, let's forget about that");
#endif
                                    __instance.Despawn(noteController);

                                    return false;
                                }
                                else if (noteCutInfo.allIsOK && !allIsOKExpected)
                                {
#if DEBUG
                                Plugin.log.Warn("We cut the note, but the player cut it wrong");
#endif
                                    
                                    SetCutTooSoon(ref noteCutInfo, hit.wasCutTooSoon);
                                    SetDirectionOk(ref noteCutInfo, hit.directionOK);
                                    SetSaberTypeOK(ref noteCutInfo, hit.saberTypeOK);
                                    SetSpeedOK(ref noteCutInfo, hit.speedOK);

                                    return true;
                                }
                            }
                            else
                            {
#if DEBUG
                            Plugin.log.Warn("We cut the note, but the player missed it");
#endif
                                __instance.HandleNoteWasMissed(noteController);

                                return false;
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    return true;
                }
            }catch(Exception e)
            {
                Plugin.log.Error("Exception in Harmony patch BeatmapObjectSpawnController.NoteWasCut: " + e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectManager))]
    [HarmonyPatch(nameof(BeatmapObjectManager.HandleNoteWasMissed))]
    [HarmonyPatch(new Type[] { typeof(NoteController) })]
    class SpectatorNoteWasMissedEventPatch
    {
        static bool Prefix(BeatmapObjectManager __instance, NoteController noteController)
        {
            try
            {
                if (Config.Instance.SocialSettings.SpectatorMode && SpectatingController.Instance != null && SpectatingController.active && Client.Instance != null && Client.Instance.connected && SpectatingController.Instance.spectatedPlayer != null && SpectatingController.Instance.spectatedPlayer.playerInfo != null)
                {
                    ulong playerId = SpectatingController.Instance.spectatedPlayer.playerInfo.playerId;

                    if (SpectatingController.Instance.playerUpdates.ContainsKey(playerId) && SpectatingController.Instance.playerUpdates[playerId].hits.Count > 0)
                    {
                        if (SpectatingController.Instance.playerUpdates[playerId].hits.TryGetValue(noteController.noteData.id, out HitData hit))
                        {
                            if (hit.noteWasCut)
                            {
#if DEBUG
                            Plugin.log.Warn("We missed the note, but the player cut it");
#endif
                                __instance.Despawn(noteController);
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Plugin.log.Error("Exception in Harmony patch BeatmapObjectSpawnController.NoteWasMissed: " + e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameEnergyCounter))]
    [HarmonyPatch(nameof(GameEnergyCounter.AddEnergy))]
    [HarmonyPatch(new Type[] { typeof(float) })]
    class SpectatorGameEnergyCounterPatch
    {
        static bool Prefix(GameEnergyCounter __instance, float value)
        {
            try
            {
                if (Config.Instance.SocialSettings.SpectatorMode && SpectatingController.Instance != null && SpectatingController.active && Client.Instance != null && Client.Instance.connected && SpectatingController.Instance.spectatedPlayer != null && SpectatingController.Instance.spectatedPlayer.playerInfo != null)
                {
                    if (__instance.energy + value <= 1E-05f && SpectatingController.Instance.spectatedPlayer.playerInfo.updateInfo.playerEnergy > 1E-04f)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Plugin.log.Error("Exception in Harmony patch GameEnergyCounter.AddEnergy: " + e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PauseController))]
    [HarmonyPatch(nameof(PauseController.Pause))]
    class GameplayManagerPausePatch
    {
        static bool Prefix(StandardLevelGameplayManager __instance, PauseMenuManager ____pauseMenuManager)
        {
            try
            {
                if (Client.Instance.connected)
                {
                    ____pauseMenuManager.ShowMenu();
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Plugin.log.Error("Exception in Harmony patch StandardLevelGameplayManager.Pause: " + e);
                return true;
            }
        }
    }
    //[HarmonyPatch(typeof(PauseMenuManager))]
    //[HarmonyPatch(nameof(PauseMenuManager.ContinueButtonPressed))]
    //class PauseMenuManagerContinueButtonPressed
    //{
    //    static bool Prefix(PauseMenuManager __instance)
    //    {
    //        try
    //        {
    //            if (Client.Instance.connected)
    //            {
    //                __instance.enabled = false;
    //                __instance.StartResumeAnimation();
    //                return false;
    //            }
    //            return true;
    //        }
    //        catch (Exception e)
    //        {
    //            Plugin.log.Error("Exception in Harmony patch PauseMenuManager.ContinueButtonPressed: " + e);
    //            return true;
    //        }
    //    }
    //}
}
