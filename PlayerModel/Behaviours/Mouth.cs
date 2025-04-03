using PlayerModel.Extensions;
using UnityEngine;

namespace PlayerModel.Behaviours
{
    [RequireComponent(typeof(SkinnedMeshRenderer)), DisallowMultipleComponent]
    public class Mouth : MonoBehaviour
    {
        public VRRig ControllingRig;

        public AudioSource AudioDevice;

        public float Weight;

        public int BlendShape;

        private GorillaSpeakerLoudness speaker_loudness;

        private SkinnedMeshRenderer mesh_renderer;

        public void Start()
        {
            mesh_renderer = GetComponent<SkinnedMeshRenderer>();
            if (ControllingRig) speaker_loudness = ControllingRig.GetComponent<GorillaSpeakerLoudness>();
        }

        public void LateUpdate()
        {
            if (ControllingRig)
            {
                float loudness = Mathf.Clamp(speaker_loudness.Loudness, 0f, 0.2f);
                loudness = loudness <= 0.05f ? Mathf.Lerp(0f, 0.1f, loudness / 0.05f) : Mathf.Lerp(0.1f, 0.2f, (loudness - 0.05f) / (0.2f - 0.05f));
                Weight = Mathf.Clamp(loudness / 0.2f * 100f, 0f, 100f);
            }
            else if (AudioDevice)
            {
                float weight = Mathf.Clamp(AudioDevice.GetLoudness() / 0.2f, 0f, 100f);
                Weight = Mathf.Lerp(Weight, weight, weight > Weight ? 0.18f : 0.34f);
            }

            mesh_renderer.SetBlendShapeWeight(BlendShape, Weight);
        }
    }
}
