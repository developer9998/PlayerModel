using System.Collections.Generic;
using PlayerModel.Models;
using UnityEngine;
using UnityEngine.XR;

#if PLUGIN
using PlayerModel.Tools;
#endif

namespace PlayerModel.Behaviours
{
    [DisallowMultipleComponent]
    public class Finger : MonoBehaviour
    {
        public List<Transform> Bones => bones;

        public EDigitType Type;

        public float Angle;

        public Vector3 Direction;

        public int BoneCount = 3;

        public XRNode Node;

#if PLUGIN
        public float Value = -1;

        public VRRig ControllingRig;

        private readonly List<Transform> bones = [];

        private readonly Dictionary<Transform, Quaternion> bone_euler_cache = [];
#else
        private readonly List<Transform> bones = new List<Transform>();

        private readonly Dictionary<Transform, Quaternion> bone_euler_cache = new Dictionary<Transform, Quaternion>();
#endif

        public void Awake()
        {
#if PLUGIN
            if (Node != XRNode.LeftHand && Node != XRNode.RightHand)
            {
                Logging.Info($"Finger {name} has unexpected node {Node}");
                enabled = false;
                return;
            }
#endif
            Transform bone = transform;
            for (int i = 0; i < BoneCount; i++)
            {
                bones.Add(bone);
                bone_euler_cache.Add(bone, bone.localRotation);
                if (i == BoneCount - 1) break;
                if (bone.childCount == 0)
                {
#if PLUGIN
                    Logging.Info($"Finger {name} can only satisfy {i + 1}/{BoneCount} bones");
#else
                    Debug.LogWarning($"{name} can only satisfy {i + 1}/{BoneCount} bones");
#endif
                    break;
                }
                bone = bone.GetChild(0);
            }
        }

        public void Move(float input)
        {
            float angle = Remap(input, Angle);
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                var direction = Direction;
                bone.transform.localRotation = bone_euler_cache[bone] * Quaternion.Euler(new Vector3(direction.x, Node == XRNode.RightHand ? direction.y : -direction.y, Node == XRNode.RightHand ? direction.z : -direction.z) * angle);
            }
        }

        private float Remap(float source, float targetTo)
        {
            float sourceTo = 1;
            float sourceFrom = 0;
            float targetFrom = 0;
            return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
        }
#if PLUGIN

        public void LateUpdate()
        {
            if (ControllingRig)
            {
                float target = Node == XRNode.LeftHand ? Type switch
                {
                    EDigitType.Index => ControllingRig.leftIndex.calcT,
                    EDigitType.Ring => ControllingRig.leftMiddle.calcT,
                    EDigitType.Thumb => ControllingRig.leftThumb.calcT,
                    _ => 0f
                } : Type switch
                {
                    EDigitType.Index => ControllingRig.rightIndex.calcT,
                    EDigitType.Ring => ControllingRig.rightMiddle.calcT,
                    EDigitType.Thumb => ControllingRig.rightThumb.calcT,
                    _ => 0f
                };

                Value = Value == -1 ? target : Mathf.Lerp(Value, target, 0.34f);
                Move(Value);
            }
        }
#endif
    }
}
