using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerModel.Behaviours
{
    public class LegacyFingerMovement : MonoBehaviour
    {
        public GameObject[] digit_L = [];
        public GameObject[] digit_R = [];

        readonly float remapvalue = -75.0f; //degrees
        public float rightGrip;
        public float rightTrigger;
        public bool rightSecondary;

        public float leftGrip;
        public float leftTrigger;
        public bool leftSecondary;



        readonly List<GameObject> objs = [];

        bool ready = false;


        public void Start()
        {
            //Debug.Log("add smoothing script");
            for (int i = 0; i < digit_L.Length; i++)
            {
                digit_L[i].AddComponent<LegacySmoothing>();
                objs.Add(digit_L[i]);
            }

            for (int i = 0; i < digit_R.Length; i++)
            {
                digit_R[i].AddComponent<LegacySmoothing>();
                objs.Add(digit_R[i]);
            }

            for (int i = 0; i < objs.Count; i++)
            {
                ResetTransforms(objs[i]);
                ResetTransforms(objs[i].transform.GetChild(0).gameObject);
                ResetTransforms(objs[i].transform.GetChild(0).GetChild(0).GetChild(0).gameObject);
            }

            ready = true;
        }
        public void Update()
        {
            if (ready)
            {
                leftGrip = ControllerInputPoller.instance.leftControllerGripFloat;
                leftTrigger = ControllerInputPoller.instance.leftControllerIndexFloat;
                leftSecondary = ControllerInputPoller.instance.leftControllerPrimaryButton;

                rightGrip = ControllerInputPoller.instance.rightControllerGripFloat;
                rightTrigger = ControllerInputPoller.instance.rightControllerIndexFloat;
                rightSecondary = ControllerInputPoller.instance.rightControllerPrimaryButton;

                digit_L[0].GetComponent<LegacySmoothing>().input = leftTrigger;
                digit_L[1].GetComponent<LegacySmoothing>().input = leftGrip;
                digit_L[2].GetComponent<LegacySmoothing>().input = Convert.ToSingle(leftSecondary);

                digit_R[0].GetComponent<LegacySmoothing>().input = rightTrigger;
                digit_R[1].GetComponent<LegacySmoothing>().input = rightGrip;
                digit_R[2].GetComponent<LegacySmoothing>().input = Convert.ToSingle(rightSecondary);


                //Debug.Log(Convert.ToSingle(leftSecondary));
                if (digit_L[0] != null) fingermove(digit_L[0], digit_L[0].GetComponent<LegacySmoothing>().avg);
                if (digit_L[1] != null) fingermove(digit_L[1], digit_L[1].GetComponent<LegacySmoothing>().avg);
                if (digit_L[2] != null) fingermove(digit_L[2], digit_L[2].GetComponent<LegacySmoothing>().avg);

                if (digit_R[0] != null) fingermove(digit_R[0], digit_R[0].GetComponent<LegacySmoothing>().avg);
                if (digit_R[1] != null) fingermove(digit_R[1], digit_R[1].GetComponent<LegacySmoothing>().avg);
                if (digit_R[2] != null) fingermove(digit_R[2], digit_R[2].GetComponent<LegacySmoothing>().avg);
            }

        }

        void fingermove(GameObject parent, float input)//parent digit bone, float value from vr controller input (0.0->1.0)
        {
            float angle = Remap(input, remapvalue);//converts normalize value to relative angle to bone
            float angle2 = Remap(input, remapvalue);
            Vector3 localAngle = parent.transform.localEulerAngles;

            parent.transform.localEulerAngles = new Vector3(angle, localAngle.y, localAngle.z);//parent bone

            Vector3 localangle1 = parent.transform.GetChild(0).GetChild(0).localEulerAngles;

            parent.transform.GetChild(0).GetChild(0).localEulerAngles = new Vector3(angle2, localangle1.y, localangle1.z);//middle bone

            Vector3 localangle2 = parent.transform.GetChild(0).GetChild(0).GetChild(0).localEulerAngles;

            parent.transform.GetChild(0).GetChild(0).GetChild(0).localEulerAngles = new Vector3(angle2, localangle2.y, localangle2.z);//end bone

        }

        float Remap(float source, float targetTo)
        {

            float sourceTo = 1;
            float sourceFrom = 0;
            float targetFrom = 0;
            return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
        }
        void ResetTransforms(GameObject obj)
        {
            GameObject newParent = new GameObject();
            newParent.name = "newParent_" + obj.name;
            newParent.transform.SetParent(obj.transform.parent, false);
            newParent.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
            newParent.transform.localScale = obj.transform.localScale;
            obj.transform.SetParent(newParent.transform, true);
        }

    }
}
