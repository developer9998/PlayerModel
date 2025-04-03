using UnityEngine;

namespace PlayerModel.Behaviours.UI
{
    public class PortableStandButton : MonoBehaviour
    {
        public PortableStand PortableStand;

        private float press_time;
        private MeshRenderer mesh_renderer;
        private Material material;

        public void Awake()
        {
            mesh_renderer = GetComponent<MeshRenderer>();
            material = mesh_renderer.sharedMaterials[1];
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (Time.realtimeSinceStartup > press_time && collider.TryGetComponent(out GorillaTriggerColliderHandIndicator component))
            {
                press_time = Time.realtimeSinceStartup + 0.25f;

                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, component.isLeftHand, 0.05f);
                GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                PortableStand.ToggleStandState();
            }
        }

        public void UpdateColour()
        {
            material.color = PortableStand.StandActive ? Color.grey : Color.white;
        }
    }
}
