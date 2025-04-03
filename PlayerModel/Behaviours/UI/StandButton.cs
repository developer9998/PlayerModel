using PlayerModel.Models;
using UnityEngine;

namespace PlayerModel.Behaviours.UI
{
    [DisallowMultipleComponent]
    public class StandButton : MonoBehaviour
    {
        public static float GlobalButtonCoolown;

        public EButtonClass ButtonClass;
        public bool SimpleButton;

        private float local_press_time;
        private Vector3 base_position, bump_position;
        private MeshRenderer renderer;

        public void Start()
        {
            base_position = transform.localPosition;
            bump_position = base_position - (Vector3.up * 0.015f); //transform.parent.InverseTransformPoint(transform.parent.TransformPoint(base_position) - (Vector3.up * 0.015f));
            renderer = GetComponent<MeshRenderer>();
            UpdateButton();
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (Time.realtimeSinceStartup > local_press_time && Time.realtimeSinceStartup > GlobalButtonCoolown && collider.TryGetComponent(out GorillaTriggerColliderHandIndicator component))
            {
                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, component.isLeftHand, 0.05f);
                GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                Press();
            }
        }

        public void Press()
        {
            local_press_time = Time.realtimeSinceStartup + 0.25f;
            Singleton<Main>.Instance.ButtonPress(ButtonClass);
            UpdateButton();
        }

        public void Update()
        {
            UpdateButton(); // TODO: not do this in update
        }

        public void UpdateButton()
        {
            if (SimpleButton) return;

            if (GlobalButtonCoolown > Time.realtimeSinceStartup)
            {
                float value = 1f - Mathf.Clamp01((GlobalButtonCoolown - Time.realtimeSinceStartup) / 0.5f);
                transform.localPosition = Vector3.Lerp(bump_position, base_position, value);
                renderer.material.color = new Color(1f, value, value);
                return;
            }

            bool local_pressed = local_press_time > Time.realtimeSinceStartup;
            transform.localPosition = local_pressed ? bump_position : base_position;
            renderer.material.color = local_pressed ? Color.grey : Color.white;
        }
    }
}
