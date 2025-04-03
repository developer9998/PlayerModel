using System;
using System.Linq;
using System.Reflection;
using GorillaExtensions;
using HarmonyLib;
using PlayerModel.Behaviours.IK;
using PlayerModel.Models;
using PlayerModel.Utils;
using PlayerModel.Extensions;
using UnityEngine;
using Model = PlayerModel.Models.IModel;

namespace PlayerModel.Behaviours
{
    [RequireComponent(typeof(VRRig))]
    public class PlayerRig : MonoBehaviour
    {
        public VRRig ControllingRig;

        public SkinnedMeshRenderer Fur;
        public Renderer Face;

        private Transform head_bone, body_bone, left_hand_bone, right_hand_bone;

        private float scaleFactor = 1f;
        private FieldInfo scaleFactor_field;

        private int setMatIndex;

        // custom player model

        public event Action<ModelDescriptor> OnModelLoaded;
        public event Action OnModelUnloaded;

        public string PlayerModelAlias = "PlayerModel";

        public bool PlayerModelLoaded;

        public Model Model;

        private GameObject model_object;
        private ModelDescriptor model_descriptor;

        // appearence

        public UnityLayer Layer;

        private Material[] materials;
        private int base_material_index, mode_material_index;
        private Material original_base_material, original_game_material;

        // inverse kinematics

        private GameObject target_left, target_right;

        private GorillaIndependentIK gorilla_ik;

        private FastIKFabric left_fabrik_ik, right_fabrik_ik;

        private Quaternion head_offset = Quaternion.Euler(-8f, 0f, 0f);

        private Quaternion left_hand_offset = Quaternion.Euler(0f, 0f, 20f);

        private Quaternion right_hand_offset = Quaternion.Euler(0f, 0f, -20f);

        public void Start()
        {
            Fur = ControllingRig.mainSkin;
            Face = ControllingRig.faceSkin;

            left_hand_bone = ControllingRig.leftHandTransform.parent;
            right_hand_bone = ControllingRig.rightHandTransform.parent;
            head_bone = ControllingRig.headMesh.transform;
            body_bone = head_bone.parent;

            scaleFactor_field = AccessTools.Field(typeof(VRRig), "lastScaleFactor");
            scaleFactor = (float)scaleFactor_field.GetValue(ControllingRig);

            ControllingRig.OnColorChanged += OnColourChanged;
        }

        public void OnDisable() => ControllingRig.OnColorChanged -= OnColourChanged;

        public void LoadModel(Model model)
        {
            if (PlayerModelLoaded) UnloadModel();
            PlayerModelLoaded = true;
            WriteRenderState(false);

            Model = model;

            model_object = Instantiate(model.Template);
            model_object.SetActive(true);
            model_object.transform.localScale = Vector3.one * scaleFactor;
            model_object.transform.SetParent(ControllingRig.transform);
            model_object.name = PlayerModelAlias;

            model_descriptor = model_object.GetComponent<ModelDescriptor>();

            // appearence
            model_descriptor.MainMesh.gameObject.SetLayer(Layer);
            materials = model_descriptor.MainMesh.materials;

            base_material_index = Model.Descriptor.BaseMaterialIndex ?? 0;
            original_base_material = new Material(materials[base_material_index]);

            mode_material_index = Model.Descriptor.GameMaterialIndex ?? 0;
            original_game_material = materials[mode_material_index];

            materials[base_material_index] = original_base_material;
            model_descriptor.MainMesh.materials = materials;

            setMatIndex = ControllingRig.setMatIndex;

            ApplyModel();

            model_descriptor.head.localScale = Layer != UnityLayer.FirstPersonOnly ? model_descriptor.head.localScale : Vector3.one * 0.001f;

            if (model_descriptor.Version > EModelVersion.Legacy1 && model_descriptor.Version < EModelVersion.Current)
            {
                static void AssignDigit(GameObject hand, GameObject[] digits)
                {
                    int index = -1;
                    int mid = -1;
                    int thumb = -1;

                    for (int i = 0; i < hand.transform.childCount; i++)
                    {
                        if (hand.transform.GetChild(i).name.Contains("index"))
                        {
                            index = i;
                        }
                        if (hand.transform.GetChild(i).name.Contains("middle"))
                        {
                            mid = i;
                        }
                        if (hand.transform.GetChild(i).name.Contains("thumb"))
                        {
                            thumb = i;
                        }
                    }

                    if (index != -1) digits[0] = hand.transform.GetChild(index).gameObject;
                    if (mid != -1) digits[1] = hand.transform.GetChild(mid).gameObject;
                    if (thumb != -1) digits[2] = hand.transform.GetChild(thumb).gameObject;
                }

                GameObject[] digit_L = new GameObject[3];
                AssignDigit(model_descriptor.leftHand.gameObject, digit_L);
                GameObject[] digit_R = new GameObject[3];
                AssignDigit(model_descriptor.rightHand.gameObject, digit_R);
                var hand_controller = model_object.AddComponent<LegacyFingerMovement>();
                hand_controller.digit_L = digit_L;
                hand_controller.digit_R = digit_R;
            }
            else if (model_descriptor.Version == EModelVersion.Current)
            {
                var fingers = model_descriptor.Digits;
                if (fingers != null && fingers.Count > 0)
                {
                    fingers.ForEach(finger => finger.ControllingRig = ControllingRig);
                }
            }

            bool lip_sync = model_descriptor.LipSync;
            if (lip_sync)
            {
                int blend_shape = (!string.IsNullOrEmpty(model_descriptor.LipShapeName) && !string.IsNullOrWhiteSpace(model_descriptor.LipShapeName)) ? model_descriptor.MainMesh.sharedMesh.GetBlendShapeIndex(model_descriptor.LipShapeName) : -1;
                blend_shape = blend_shape > -1 ? blend_shape : 0;
                var mouth = model_descriptor.MainMesh.gameObject.GetOrAddComponent<Mouth>();
                mouth.BlendShape = blend_shape;
                mouth.ControllingRig = ControllingRig;
            }

            // inverse kinematics

            target_left = new GameObject("Left Hand Target");
            target_left.transform.SetParent(model_object.transform);

            target_right = new GameObject("Right Hand Target");
            target_right.transform.SetParent(model_object.transform);

            var inverse_kinematics = ModelUtils.SetupIK(model_descriptor, target_left.transform, target_right.transform);

            if (inverse_kinematics.Length == 1 && inverse_kinematics.First() is GorillaIndependentIK)
            {
                gorilla_ik = inverse_kinematics.First() as GorillaIndependentIK;
            }
            else if (inverse_kinematics.Length == 2 && inverse_kinematics.All(ik => ik is FastIKFabric))
            {
                left_fabrik_ik = inverse_kinematics.First() as FastIKFabric;
                right_fabrik_ik = inverse_kinematics.Last() as FastIKFabric;
            }

            OnModelLoaded?.SafeInvoke(model_descriptor);
            UpdateRig();
        }

        public void UnloadModel()
        {
            if (!PlayerModelLoaded) return;
            PlayerModelLoaded = false;
            WriteRenderState(true);

            if (model_object)
            {
                Destroy(model_object);
                model_descriptor = null;
            }

            OnModelUnloaded?.SafeInvoke();
            Model = null;
        }

        public void WriteRenderState(bool render)
        {
            if (ControllingRig.isOfflineVRRig && Layer == UnityLayer.MirrorOnly) return;
            Fur.gameObject.SetLayer(render ? UnityLayer.Default : UnityLayer.Bake);
            Face.gameObject.SetLayer(render ? (ControllingRig.isOfflineVRRig ? UnityLayer.MirrorOnly : UnityLayer.Default) : UnityLayer.Bake);
        }

        public void OnColourChanged(Color colour)
        {
            if (PlayerModelLoaded) ApplyModel();
        }

        public void LateUpdate()
        {
            if (PlayerModelLoaded)
            {
                var current_sf = (float)scaleFactor_field.GetValue(ControllingRig);
                if (current_sf != scaleFactor)
                {
                    scaleFactor = current_sf;
                    if (left_fabrik_ik && right_fabrik_ik)
                    {
                        left_fabrik_ik.Reset();
                        right_fabrik_ik.Reset();
                    }
                    // TODO: implement gorillaik reset function
                }

                var mat_index = ControllingRig.setMatIndex;
                if (mat_index != setMatIndex)
                {
                    setMatIndex = mat_index;
                    ApplyModel();
                }

                UpdateRig();
            }
        }

        public void UpdateRig()
        {
            target_left.transform.rotation = left_hand_bone.rotation * Quaternion.Euler(model_descriptor.leftHandRotationOffset) * left_hand_offset;
            target_left.transform.position = left_hand_bone.position + target_left.transform.rotation * model_descriptor.leftHandPositionOffset * scaleFactor;
            target_right.transform.rotation = right_hand_bone.rotation * Quaternion.Euler(model_descriptor.rightHandRotationOffset) * right_hand_offset;
            target_right.transform.position = right_hand_bone.position + target_right.transform.rotation * model_descriptor.rightHandPositionOffset * scaleFactor;
            
            model_descriptor.body.SetPositionAndRotation(body_bone.position, body_bone.rotation);
            model_descriptor.head.rotation = head_bone.rotation * Quaternion.Euler(model_descriptor.headRotationOffset);
            var local_head_position = model_descriptor.head.parent.InverseTransformPoint(head_bone.position);
            local_head_position += Model.Descriptor.head.localPosition - local_head_position;
            Vector3 offset = model_descriptor.head.rotation * model_descriptor.headPositionOffset;
            local_head_position += new Vector3(offset.x / (scaleFactor * 100), offset.y / (scaleFactor * 100), offset.z / (scaleFactor * 100));
            model_descriptor.head.localPosition = local_head_position;
        }

        public void ApplyModel()
        {
            if (ControllingRig.setMatIndex > 0 && model_descriptor.GameModeMaterials)
            {
                var rig_material = ControllingRig.materialsToChangeTo[ControllingRig.setMatIndex];
                rig_material.SetTextureScale("_BaseMap", model_descriptor.GameModeMaterialSize);
                materials[mode_material_index] = rig_material;
                if (mode_material_index == base_material_index) goto SetMaterials; // lazy
            }

            if (base_material_index != mode_material_index)
            {
                materials[mode_material_index] = original_game_material;
            }
            else
            {
                materials[base_material_index] = original_base_material;
            }

            if (Model.Descriptor.CustomColors)
            {
                if (materials[base_material_index].HasColor("_BaseColor")) materials[base_material_index].SetColor("_BaseColor", ControllingRig.playerColor);
                else materials[base_material_index].color = ControllingRig.playerColor;
            }

        SetMaterials:
            model_descriptor.MainMesh.materials = materials;
        }
    }
}
