using UnityEngine;

namespace PlayerModel.Behaviours.UI
{
    [RequireComponent(typeof(CosmeticWardrobe)), DisallowMultipleComponent]
    public class PortableStand : MonoBehaviour
    {
        public static bool StandActive;

        private Stand stand;
        private PortableStandButton remote_stand_toggle;
        private Transform stand_base, stand_platform;
        private GameObject base_display_head;

        public void Awake()
        {
            // Create and position call button to have it in optimal position (currently at the bottom-left corner of the support holding the turntable)
            var button = Instantiate(Singleton<Main>.Instance.CallButtonAsset, transform, true);
            button.transform.localPosition = new Vector3(-0.4117f, 0.088f, 0.3025f);
            button.transform.localEulerAngles = new Vector3(270f, 90f, 0f);
            button.transform.localScale = new Vector3(1f, 2f, 2f);

            // Implement call button behaviour
            remote_stand_toggle = button.AddComponent<PortableStandButton>();
            remote_stand_toggle.PortableStand = this;

            // Get turntable and display head objects
            var lazy_susan_turntable = transform.Find("WornDisplay/SpinningObjects") ?? transform.FindChildRecursive("SpinningObjects");
            base_display_head = lazy_susan_turntable.FindChildRecursive("DisplayHead").gameObject;

            // Create and position stand to have platform in optimal position
            stand_base = Instantiate(Singleton<Main>.Instance.StandAsset, lazy_susan_turntable, true).transform;
            stand_base.localPosition = new Vector3(0f, 0.0349f, 0.0277f);
            stand_base.localEulerAngles = Vector3.zero;
            stand_base.localScale = Vector3.one * 0.3f;

            // Implement stand behaviour
            stand = stand_base.gameObject.AddComponent<Stand>();
            //stand.WriteCreditState(Singleton<Stand>.HasInstance && Singleton<Stand>.Instance.CreditModel.activeSelf);

            // Parent platform to turntable
            stand_platform = stand_base.Find("Platform");
            stand_platform.Find("Circle").gameObject.SetActive(false);
            stand_platform.SetParent(lazy_susan_turntable);

            // Position stand to have menu in optimal position
            stand_base.localPosition = new Vector3(0.0107f, -0.0466f, 0.0386f);
            stand_base.localEulerAngles = Vector3.right * 32f;
            stand_base.localScale = Vector3.one * 0.21f;
            stand_base.SetParent(transform);

            WriteStandState(StandActive);
        }

        public void WriteStandState(bool showStand)
        {
            base_display_head.SetActive(!showStand);
            stand_base.gameObject.SetActive(showStand);
            stand_platform.gameObject.SetActive(showStand);
            remote_stand_toggle.UpdateColour();
            if (stand)
            {
                stand.enabled = showStand;
                if (stand.player_preview) stand.player_preview.SetActive(showStand);
                else stand.WriteCreditState(Singleton<Stand>.HasInstance && Singleton<Stand>.Instance.CreditModel.activeSelf);
            }
        }

        public void ToggleStandState()
        {
            StandActive ^= true;
            WriteStandState(StandActive);
        }
    }
}
