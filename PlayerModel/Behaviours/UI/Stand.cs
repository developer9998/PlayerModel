using System;
using System.Collections.Generic;
using GorillaExtensions;
using PlayerModel.Models;
using PlayerModel.Utils;
using UnityEngine;
using Button = PlayerModel.Behaviours.UI.StandButton;
using Model = PlayerModel.Models.IModel;
using Text = UnityEngine.UI.Text;

namespace PlayerModel.Behaviours.UI
{
    [DisallowMultipleComponent]
    public class Stand : Singleton<Stand>
    {
        public override bool SingleInstance => false;

        public bool IsMainStand => Instance == this; // The "Singleton" name is kind of misleading lol

        public List<Button> Buttons = [];

        public List<Renderer> Orbs = [];

        private Renderer custom_material_orb;

        private Material fallback_material;

        public Text Text_Model, Text_Author, Text_Version, Text_CreditPrompt;

        public Transform PreviewPoint;

        public GameObject CreditModel;

        public Model Model;

        public GameObject player_preview;

        private ModelDescriptor preview_descriptor;

        private Material[] preview_materials;

        private GameObject target_left, target_right;

        private Transform rig, head_bone, body_bone, left_hand_bone, right_hand_bone;

        private float scale_factor = 1f;

        public override void Initialize()
        {
            enabled = false;

            if (IsMainStand)
            {
                transform.position = new Vector3(-48.8854f, 16.1596f, -116.6837f);
                transform.eulerAngles = Vector3.up * 247f;
            }

            var display = transform.Find("Display");

            var buttons = display.Find("Buttons");
            foreach (Transform tform in buttons)
            {
                if (tform.name.StartsWith("Button") && Enum.TryParse<EButtonClass>(tform.name[6..], out var button_class))
                {
                    if (!IsMainStand) // TODO: make portable button layouts apart of an animation, and not hardcoded
                    {
                        float x = button_class switch
                        {
                            EButtonClass.Select => 0f,
                            EButtonClass.NavLeft => 0.25f,
                            EButtonClass.NavRight => -0.25f,
                            _ => 0f
                        };
                        tform.localPosition = tform.localPosition.WithX(x);
                    }

                    var button = tform.gameObject.AddComponent<Button>();
                    button.ButtonClass = button_class;

                    Buttons.Add(button);
                }
            }

            var orbs = display.Find("Buttons/Materials");
            foreach (Transform tform in orbs)
            {
                if (Enum.TryParse<EButtonClass>(string.Concat("Material", tform.name), out var button_class))
                {
                    var button = tform.gameObject.AddComponent<Button>();
                    button.ButtonClass = button_class;
                    button.SimpleButton = true;

                    var renderer = tform.GetComponent<Renderer>();

                    Orbs.Add(renderer);

                    if (button_class == EButtonClass.MaterialFur)
                    {
                        custom_material_orb = renderer;
                        fallback_material = renderer.material;
                    }

                    tform.gameObject.SetActive(IsMainStand); // TODO: hide portable orbs through previously mentioned animation
                }
            }

            var local_rig = GorillaTagger.Instance.offlineVRRig;
            preview_materials = [fallback_material, local_rig.materialsToChangeTo[1], local_rig.materialsToChangeTo[2], local_rig.materialsToChangeTo[3]];

            var ui = display.Find("Canvas");
            Text_Model = ui.Find("Model").GetComponent<Text>();
            Text_Author = ui.Find("Author").GetComponent<Text>();
            Text_Version = ui.Find("Version").GetComponent<Text>();
            Text_CreditPrompt = ui.Find("CreditPrompt").GetComponent<Text>();

            Text_Version.text = $"v{Constants.Version}";

            var platform = transform.Find("Platform");

            PreviewPoint = platform.Find("Preview");
            CreditModel = PreviewPoint.Find("CreditText").gameObject;
            rig = platform.Find("Rig");
            head_bone = rig.FindChildRecursive("head");
            body_bone = rig.FindChildRecursive("Torso");
            left_hand_bone = rig.FindChildRecursive("hand.L");
            right_hand_bone = rig.FindChildRecursive("hand.R");

            Vector3 scale = body_bone.localScale;
            Transform parent = body_bone.parent;

            while (parent != null)
            {
                scale.Scale(parent.localScale);
                parent = parent.parent;
            }

            var mean = (scale.x + scale.y + scale.z) / 3f;
            scale_factor = mean / 100f;

            WriteCreditState(IsMainStand || Instance.CreditModel.activeSelf);
            enabled = true;
        }

        public void Update()
        {
            if (!IsMainStand)
            {
                if (Instance.Model != Model)
                {
                    ApplyPlayerModel(Instance.Model);
                }
                if (Instance.CreditModel.activeSelf != CreditModel.activeSelf)
                {
                    WriteCreditState(Instance.CreditModel.activeSelf);
                }
            }

            if (player_preview)
            {
                UpdateRig();
            }
        }

        public void WriteCreditState(bool show_credit)
        {
            Text_Model.enabled = !show_credit;
            Text_Author.enabled = !show_credit;
            Text_Version.enabled = !show_credit;
            Text_CreditPrompt.enabled = show_credit;
            CreditModel.SetActive(show_credit);
        }

        public void SetCustomMaterial(Material material, Material displayMaterial)
        {
            preview_materials[0] = material;
            custom_material_orb.material = displayMaterial;
        }

        public void SetPreviewModelMaterial(EButtonClass button_class)
        {
            int index = (int)button_class - (int)EButtonClass.MaterialFur;
            if (index > -1 && index < preview_materials.Length)
            {
                var descriptor = player_preview.GetComponent<ModelDescriptor>();
                int mesh_material_index = Model.Descriptor.GameMaterialIndex ?? 0;
                var materials = descriptor.MainMesh.materials;
                materials[mesh_material_index] = preview_materials[index];
                if (index != 0)
                {
                    materials[mesh_material_index] = new(materials[mesh_material_index]);
                    materials[mesh_material_index].SetTextureScale("_BaseMap", descriptor.GameModeMaterialSize);
                }
                descriptor.MainMesh.materials = materials;
            }
        }

        public void ApplyPlayerModel(Model model)
        {
            Model = model;

            if (CreditModel.activeSelf)
            {
                WriteCreditState(false);
            }

            if (player_preview)
            {
                Destroy(player_preview.GetComponent<ModelDescriptor>().head.gameObject);
                Destroy(player_preview.GetComponent<ModelDescriptor>().body.gameObject);
                Destroy(player_preview);
            }

            player_preview = Instantiate(model.Template);
            player_preview.SetActive(true);

            preview_descriptor = player_preview.GetComponent<ModelDescriptor>();

            Text_Model.text = $"MODEL: {preview_descriptor.ModelName.ToUpper()}";
            Text_Author.text = $"AUTHOR: {preview_descriptor.Author.ToUpper()}";

            int mesh_material_index = preview_descriptor.BaseMaterialIndex ?? 0;
            var material = preview_descriptor.MainMesh.materials[mesh_material_index];
            SetCustomMaterial(material, preview_descriptor.DisplayMaterial ?? fallback_material);

            bool lip_sync = preview_descriptor.LipSync;
            if (lip_sync)
            {
                AudioSource audio_device = preview_descriptor.head.gameObject.AddComponent<AudioSource>();
                audio_device.playOnAwake = false;
                audio_device.spatialBlend = 1f;
                audio_device.minDistance = 7f;
                audio_device.maxDistance = 12.5f;
                audio_device.volume = 0.15f;
                audio_device.rolloffMode = AudioRolloffMode.Linear;
                audio_device.clip = GorillaTagger.Instance.offlineVRRig.myReplacementVoice.replacementVoiceClipsLoud[3];
                audio_device.loop = true;
                //audio_device.Play();
                int blend_shape = (!string.IsNullOrEmpty(preview_descriptor.LipShapeName) && !string.IsNullOrWhiteSpace(preview_descriptor.LipShapeName)) ? preview_descriptor.MainMesh.sharedMesh.GetBlendShapeIndex(preview_descriptor.LipShapeName) : -1;
                blend_shape = blend_shape > -1 ? blend_shape : 0;
                var mouth = preview_descriptor.MainMesh.gameObject.GetOrAddComponent<Mouth>();
                mouth.BlendShape = blend_shape;
                mouth.AudioDevice = audio_device;
            }

            target_left = new GameObject("Left Hand Target");
            target_left.transform.SetParent(player_preview.transform);

            target_right = new GameObject("Right Hand Target");
            target_right.transform.SetParent(player_preview.transform);

            ModelUtils.SetupIK(preview_descriptor, target_left.transform, target_right.transform);
            UpdateRig();
        }

        public void UpdateRig()
        {
            player_preview.transform.SetPositionAndRotation(rig.position, rig.rotation);
            player_preview.transform.localScale = Vector3.one * scale_factor;

            target_left.transform.rotation = left_hand_bone.rotation * Quaternion.Euler(preview_descriptor.leftHandRotationOffset);
            target_left.transform.position = left_hand_bone.position + target_left.transform.rotation * (preview_descriptor.leftHandPositionOffset * scale_factor);
            target_right.transform.rotation = right_hand_bone.rotation * Quaternion.Euler(preview_descriptor.rightHandRotationOffset);
            target_right.transform.position = right_hand_bone.position + target_right.transform.rotation * (preview_descriptor.rightHandPositionOffset * scale_factor);
            
            preview_descriptor.body.SetPositionAndRotation(body_bone.position, body_bone.rotation);
            preview_descriptor.head.rotation = head_bone.rotation * Quaternion.Euler(preview_descriptor.headRotationOffset);
            var local_head_position = preview_descriptor.head.parent.InverseTransformPoint(head_bone.position);
            local_head_position += Model.Descriptor.head.localPosition - local_head_position;
            Vector3 offset = preview_descriptor.head.rotation * preview_descriptor.headPositionOffset;
            local_head_position += new Vector3(offset.x / (scale_factor * 100), offset.y / (scale_factor * 100), offset.z / (scale_factor * 100));
            preview_descriptor.head.localPosition = local_head_position;
        }
    }
}
