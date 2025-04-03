using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PlayerModel.Behaviours;
using PlayerModel.Behaviours.Networking;
using UnityEngine;

namespace PlayerModel
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource PluginLogger;

        public static ConfigFile PluginConfig;

        public void Awake()
        {
            PluginLogger = Logger;
            PluginConfig = Config;

            Harmony.CreateAndPatchAll(GetType().Assembly, Constants.Guid);
            GorillaTagger.OnPlayerSpawned(() => new GameObject(Constants.Name, typeof(Main), typeof(NetworkHandler)));
        }
    }
}
