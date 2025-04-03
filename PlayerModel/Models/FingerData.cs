using System;
using PlayerModel.Behaviours;
using UnityEngine;
using UnityEngine.XR;

namespace PlayerModel.Models
{
    [Serializable]
    public class FingerData
    {
        public EDigitType Type;

        public Transform RootBone;

        [Range(1, 6)]
        public int BoneCount = 3;

        [Range(-180f, 180f), Tooltip("The angle of finger movement, use Direction to determine axis")]
        public float Angle = -75f;

        [Tooltip("The direction of finger movement, coordinates should range from 0 - 1")]
        public Vector3 Direction = Vector3.right;

        private Finger finger;

        public Finger CreateComponent(XRNode node)
        {
            if (!(bool)finger)
            {
                finger = RootBone.gameObject.AddComponent<Finger>();
                finger.Type = Type;
                finger.Angle = Angle;
                finger.Direction = Direction;
                finger.BoneCount = BoneCount;
                finger.Node = node;
            }
            return finger;
        }
    }
}
