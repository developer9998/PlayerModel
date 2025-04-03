using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Photon.Realtime;
using PlayerModel.Models;
using PlayerModel.Tools;
using UnityEngine;

namespace PlayerModel.Behaviours.Networking
{
    // https://github.com/developer9998/GorillaInfoWatch/blob/fbfcf043668cd4e90963836a5aecdbbbc56e8e6a/GorillaInfoWatch/Behaviours/Networking/NetworkedPlayer.cs
    [RequireComponent(typeof(RigContainer)), DisallowMultipleComponent]
    public class NetworkedPlayer : MonoBehaviour
    {
        public VRRig Rig;
        public NetPlayer Owner;
        public bool HasPlayerModel;

        private PlayerRig player_model_rig;

        public async Task Start()
        {
            player_model_rig = Rig.gameObject.AddComponent<PlayerRig>();
            player_model_rig.ControllingRig = Rig;

            NetworkHandler.Instance.OnPlayerPropertyChanged += OnPlayerPropertyChanged;

            await Task.Delay(300);

            Player player = Owner.GetPlayerRef();
            NetworkHandler.Instance.OnPlayerPropertiesUpdate(player, player.CustomProperties);
        }

        public void OnDestroy()
        {
            NetworkHandler.Instance.OnPlayerPropertyChanged -= OnPlayerPropertyChanged;

            if (HasPlayerModel)
            {
                HasPlayerModel = false;
                player_model_rig.UnloadModel();
                Destroy(player_model_rig);
            }
        }

        public void OnPlayerPropertyChanged(NetPlayer player, Dictionary<string, object> properties)
        {
            if (player == Owner)
            {
                Logging.Info($"{player.NickName} got properties: {string.Join(", ", properties.Select(prop => $"[{prop.Key}: {prop.Value}]"))}");

                if (properties.TryGetValue("Model", out object player_mode_object) && player_mode_object is string player_model_name)
                {
                    IModel model = Singleton<Main>.Instance.ModelLoader.Models.Find(model => string.Concat(model.Descriptor.ModelName, model.Descriptor.Author) == player_model_name);
                    if (model != null) player_model_rig.LoadModel(model);
                    else player_model_rig.UnloadModel();
                }
            }
        }
    }
}
