#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PlayerModel.Behaviours.IK
{
    /// <summary>
    /// Fabrik IK Solver
    /// </summary>
    // https://github.com/ditzel/SimpleIK/blob/c3af974d047bd1d5f8971144f2aec95da2a8030c/FastIK/Assets/FastIK/Scripts/FastIK/FastIKFabric.cs
    public class FastIKFabric : MonoBehaviour
    {
        public int ChainLength = 2;
        public Transform Target;
        public Transform Pole;

        [Header("Solver Parameters")]
        public int Iterations = 10;
        public float Delta = 0.001f;
        [Range(0, 1)]
        public float SnapBackStrength = 1f;

        protected float[] BonesLength;
        protected float CompleteLength;
        protected Transform[] Bones;
        protected Vector3[] Positions;
        protected Vector3[] StartDirectionSucc;
        protected Quaternion[] StartRotationBone;
        protected Quaternion StartRotationTarget;
        protected Transform Root;

        protected Vector3[] InitialLocalPositions;

        public void Reset()
        {
            for (int i = 0; i < Bones.Length; i++)
                Bones[i].localPosition = InitialLocalPositions[i];
            Init();
        }

        public void Start() => Init();

        public void Init()
        {
            Bones = new Transform[ChainLength + 1];
            Positions = new Vector3[ChainLength + 1];
            BonesLength = new float[ChainLength];
            StartDirectionSucc = new Vector3[ChainLength + 1];
            StartRotationBone = new Quaternion[ChainLength + 1];
            InitialLocalPositions = new Vector3[ChainLength + 1];

            Root = transform;
            for (var i = 0; i <= ChainLength; i++)
            {
                if (Root == null)
                    throw new UnityException("Chain length exceeds hierarchy.");
                Root = Root.parent;
            }

            if (Target == null)
            {
                Target = new GameObject(gameObject.name + " Target").transform;
                SetPositionRootSpace(Target, GetPositionRootSpace(transform));
            }
            StartRotationTarget = GetRotationRootSpace(Target);

            var current = transform;
            CompleteLength = 0;
            for (var i = Bones.Length - 1; i >= 0; i--)
            {
                Bones[i] = current;
                StartRotationBone[i] = GetRotationRootSpace(current);
                InitialLocalPositions[i] = current.localPosition;

                if (i == Bones.Length - 1)
                    StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
                else
                {
                    StartDirectionSucc[i] = GetPositionRootSpace(Bones[i + 1]) - GetPositionRootSpace(current);
                    BonesLength[i] = StartDirectionSucc[i].magnitude;
                    CompleteLength += BonesLength[i];
                }
                current = current.parent;
            }

            LateUpdate();
        }

        public void LateUpdate()
        {
            ResolveIK();
            transform.rotation = Target.rotation;
        }

        private void ResolveIK()
        {
            if (Target == null) return;

            if (BonesLength.Length != ChainLength) Reset();

            for (int i = 0; i < Bones.Length; i++)
                Bones[i].localPosition = InitialLocalPositions[i];

            for (int i = 0; i < Bones.Length; i++)
                Positions[i] = GetPositionRootSpace(Bones[i]);

            Vector3 targetPosition = GetPositionRootSpace(Target);
            Quaternion targetRotation = GetRotationRootSpace(Target);

            if ((targetPosition - Positions[0]).sqrMagnitude >= CompleteLength * CompleteLength)
            {
                var direction = (targetPosition - Positions[0]).normalized;
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
            }
            else
            {
                for (int i = 0; i < Positions.Length - 1; i++)
                    Positions[i + 1] = Vector3.Lerp(Positions[i + 1], Positions[i] + StartDirectionSucc[i], SnapBackStrength);

                for (int iteration = 0; iteration < Iterations; iteration++)
                {
                    for (int i = Positions.Length - 1; i > 0; i--)
                        Positions[i] = i == Positions.Length - 1 ? targetPosition : Positions[i + 1] + (Positions[i] - Positions[i + 1]).normalized * BonesLength[i];

                    for (int i = 1; i < Positions.Length; i++)
                        Positions[i] = Positions[i - 1] + (Positions[i] - Positions[i - 1]).normalized * BonesLength[i - 1];

                    if ((Positions[Positions.Length - 1] - targetPosition).sqrMagnitude < Delta * Delta)
                        break;
                }
            }

            if (Pole != null)
            {
                Vector3 polePosition = GetPositionRootSpace(Pole);
                for (int i = 1; i < Positions.Length - 1; i++)
                {
                    var plane = new Plane(Positions[i + 1] - Positions[i - 1], Positions[i - 1]);
                    var projectedPole = plane.ClosestPointOnPlane(polePosition);
                    var projectedBone = plane.ClosestPointOnPlane(Positions[i]);
                    var angle = Vector3.SignedAngle(projectedBone - Positions[i - 1], projectedPole - Positions[i - 1], plane.normal);
                    Positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (Positions[i] - Positions[i - 1]) + Positions[i - 1];
                }
            }

            for (int i = 0; i < Positions.Length; i++)
            {
                if (i == Positions.Length - 1)
                    SetRotationRootSpace(Bones[i], Quaternion.Inverse(targetRotation) * StartRotationTarget * Quaternion.Inverse(StartRotationBone[i]));
                else
                    SetRotationRootSpace(Bones[i], Quaternion.FromToRotation(StartDirectionSucc[i], Positions[i + 1] - Positions[i]) * Quaternion.Inverse(StartRotationBone[i]));
                SetPositionRootSpace(Bones[i], Positions[i]);
            }
        }

        private Vector3 GetPositionRootSpace(Transform current) => Root == null ? current.position : Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
        private void SetPositionRootSpace(Transform current, Vector3 position) => current.position = Root == null ? position : Root.rotation * position + Root.position;
        private Quaternion GetRotationRootSpace(Transform current) => Root == null ? current.rotation : Quaternion.Inverse(current.rotation) * Root.rotation;
        private void SetRotationRootSpace(Transform current, Quaternion rotation) => current.rotation = Root == null ? rotation : Root.rotation * rotation;

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {

            var current = transform;
            for (int i = 0; i < ChainLength && current != null && current.parent != null; i++)
            {
                var scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
                Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
                Handles.color = Color.green;
                Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
                current = current.parent;
            }
        }
#endif
    }
}
