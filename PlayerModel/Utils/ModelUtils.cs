using PlayerModel.Behaviours;
using PlayerModel.Behaviours.IK;
using PlayerModel.Models;
using UnityEngine;

namespace PlayerModel.Utils
{
    public static class ModelUtils
    {
        public static MonoBehaviour[] SetupIK(ModelDescriptor descriptor, Transform target_left, Transform target_right)
        {
            if (descriptor.IKType == EIKType.GorillaIK)
            {
                // Use "GorillaIndependentIK", an accurate IK solver used by Gorilla Tag

                var ik = descriptor.gameObject.AddComponent<GorillaIndependentIK>();
                ik.targetLeft = target_left;
                ik.targetRight = target_right;
                ik.leftUpperArm = descriptor.leftHand.transform.parent.parent;
                ik.leftLowerArm = descriptor.leftHand.transform.parent;
                ik.leftHand = descriptor.leftHand.transform;
                ik.rightUpperArm = descriptor.rightHand.transform.parent.parent;
                ik.rightLowerArm = descriptor.rightHand.transform.parent;
                ik.rightHand = descriptor.rightHand.transform;

                return [ik];
            }

            // Use "FastIKFabric", a popular IK solver using Fabrik

            // Unlike GorillaIndependentIK, FastIKFabric is placed in each hand of the rig, instead of assigning them to one component

            // FastIKFabric also uses poles, making it the better option for player models with differing arm lengths (compared to the base game gorilla arms)

            var poleL = new GameObject("Left Hand Pole");
            poleL.transform.SetParent(descriptor.body.transform, false);
            poleL.transform.localPosition = new Vector3(-5f, -5f, -10);
            var left_ik = descriptor.leftHand.gameObject.AddComponent<FastIKFabric>();
            left_ik.Target = target_left;
            left_ik.Pole = poleL.transform;

            var poleR = new GameObject("Right Hand Pole");
            poleR.transform.SetParent(descriptor.body.transform, false);
            poleR.transform.localPosition = new Vector3(5f, -5f, -10);
            var right_ik = descriptor.rightHand.gameObject.AddComponent<FastIKFabric>();
            right_ik.Target = target_right;
            right_ik.Pole = poleR.transform;

            return [left_ik, right_ik];
        }
    }
}
