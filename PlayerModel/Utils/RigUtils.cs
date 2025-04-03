using System;
using System.Reflection;
using HarmonyLib;
using Photon.Realtime;

namespace PlayerModel.Utils
{
    public static class RigUtils
    {
        private static Assembly Assembly => typeof(RigContainer).Assembly;
        private static Type RefContainerType => Assembly.GetType("RigContainer&");
        private static Type RigCacheType => Assembly.GetType("VRRigCache");
        private static object RigCacheInstance => AccessTools.Property(RigCacheType, "Instance").GetValue(RigCacheType, null);

        public static bool TryGetVRRig(NetPlayer targetPlayer, out RigContainer playerRig)
        {
            if (RigCacheInstance == null)
            {
                playerRig = null;
                return false;
            }

            object[] parameters = [targetPlayer, null];
            bool has_vr_rig = (bool)AccessTools.Method(RigCacheType, "TryGetVrrig", [typeof(NetPlayer), RefContainerType]).Invoke(RigCacheInstance, parameters);

            playerRig = has_vr_rig ? (RigContainer)parameters[1] : null;
            return has_vr_rig;
        }

        public static bool TryGetVRRig(Player targetPlayer, out RigContainer playerRig)
        {
            return TryGetVRRig(NetworkSystem.Instance.GetPlayer(targetPlayer.ActorNumber), out playerRig);
        }
    }
}
