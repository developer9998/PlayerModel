using System;
using System.Collections.Generic;
using PlayerModel.Models;
using UnityEngine;

#if !PLUGIN
using UnityEngine.XR;
#endif

namespace PlayerModel.Behaviours
{
    [DisallowMultipleComponent]
    public class ModelDescriptor : MonoBehaviour
    {
        public string ModelName;
        public string Author;

        public SkinnedMeshRenderer MainMesh;
        public Material DisplayMaterial;

        public bool CustomColors = false;
        public Material BaseMaterial;

        public bool GameModeMaterials;
        // private EModeMaterialType GameModeMaterialType = EModeMaterialType.None;
        public Vector2 GameModeMaterialSize = Vector2.one;
        public Material GameMaterial;

        public bool LipSync = false;
        public string LipShapeName;

        public Transform body;

        public Transform head;
        public Vector3 headPositionOffset;
        public Vector3 headRotationOffset;

        public Transform leftHand;
        public Vector3 leftHandPositionOffset;
        public Vector3 leftHandRotationOffset;

        public Transform rightHand;
        public Vector3 rightHandPositionOffset;
        public Vector3 rightHandRotationOffset;

        public EIKType IKType;

        public List<Finger> Digits;

#if PLUGIN

        public EModelVersion Version;

        public int? BaseMaterialIndex;

        public int? GameMaterialIndex;

#else

        public List<FingerData> leftHandDigits = new List<FingerData>()
        {
            new FingerData()
        };
        public List<FingerData> rightHandDigits = new List<FingerData>()
        {
            new FingerData()
        };

        private readonly List<Finger> fingers = new List<Finger>();

        private string[] digit_type_names;

        private static Dictionary<EDigitType, float> editor_runtime_value = new Dictionary<EDigitType, float>();

        private bool is_primary_instance;

        private bool dirty = true;

        public void Awake()
        {
            is_primary_instance = FindObjectOfType<ModelDescriptor>() == this;

            digit_type_names = Enum.GetNames(typeof(EDigitType));

            if (leftHand && leftHandDigits != null && leftHandDigits.Count != 0)
            {
                foreach (var left_hand_digit in leftHandDigits)
                {
                    if ((bool)left_hand_digit.RootBone && left_hand_digit.RootBone.IsChildOf(leftHand))
                    {
                        var finger = left_hand_digit.CreateComponent(XRNode.LeftHand);
                        fingers.Add(finger);
                    }
                }
            }

            if (rightHand && rightHandDigits != null && rightHandDigits.Count != 0)
            {
                foreach (var right_hand_digit in rightHandDigits)
                {
                    if ((bool)right_hand_digit.RootBone && right_hand_digit.RootBone.IsChildOf(rightHand))
                    {
                        var finger = right_hand_digit.CreateComponent(XRNode.RightHand);
                        fingers.Add(finger);
                    }
                }
            }
        }

        public void OnGUI()
        {
            if (!is_primary_instance) return;

            for(int i = 0; i < digit_type_names.Length; i++)
            {
                var digit_type = (EDigitType)Enum.Parse(typeof(EDigitType), digit_type_names[i]);
                var value = editor_runtime_value.ContainsKey(digit_type) ? editor_runtime_value[digit_type] : 0;
                value = GUI.HorizontalSlider(new Rect(25, 25 + (i * 35), 100, 30), value, 0f, 1f);
                GUI.Label(new Rect(130, 25 + (i * 35), 100, 30), digit_type_names[i]);
                if (editor_runtime_value.ContainsKey(digit_type) && editor_runtime_value[digit_type] != value)
                {
                    editor_runtime_value[digit_type] = value;
                    dirty = true;
                }
                else if (!editor_runtime_value.ContainsKey(digit_type))
                {
                    editor_runtime_value.Add(digit_type, value);
                    dirty = true;
                }
            }

            if (dirty)
            {
                dirty = false;
                MoveFingers();
            }
        }

        public void MoveFingers()
        {
            foreach(var finger in fingers)
            {
                var digit_type = finger.Type;
                var value = editor_runtime_value.ContainsKey(digit_type) ? editor_runtime_value[digit_type] : 0;
                finger.Move(value);
            }
        }

#endif
    }
}