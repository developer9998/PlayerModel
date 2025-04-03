using UnityEngine;

namespace PlayerModel.Behaviours
{
    public class LegacySmoothing : MonoBehaviour
    {
        const int samples = 3;
        readonly float[] readings = new float[samples];
        int index = 0;
        float total = 0;
        public float avg = 0;

        public float input;

        public void Start()
        {
            for (int i = 0; i < readings.Length; i++)
            {
                readings[i] = 0;
            }
        }

        public void Update()
        {
            total -= readings[index];
            readings[index] = input;
            total += readings[index];
            index++;

            if (index >= samples)
            {
                index = 0;
            }

            avg = total / samples;
        }
    }
}
