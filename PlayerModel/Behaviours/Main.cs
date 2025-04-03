using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PlayerModel.Behaviours.UI;
using PlayerModel.Extensions;
using PlayerModel.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using PlayerModel.Behaviours.Networking;
using Rig = PlayerModel.Behaviours.PlayerRig;
using Button = PlayerModel.Behaviours.UI.StandButton;
using Descriptor = PlayerModel.Behaviours.ModelDescriptor;
using Model = PlayerModel.Models.IModel;

namespace PlayerModel.Behaviours
{
    public class Main : Singleton<Main>
    {
        public Stand Stand;

        public ModelLoader ModelLoader;

        public Stack<Action<EButtonClass>> ButtonActionStack = new();

        public AssetBundle Bundle;

        public GameObject CallButtonAsset, StandAsset;

        public Rig FirstPersonRig, ThirdPersonRig;

        private readonly Shader uber_shader = UberShader.GetShader();

        private int model_selection_index;

        private bool city_active;

        private TransformFollow left_trigger, right_trigger;

        private Transform base_left_index, base_right_index;

        public override void Initialize()
        {
            ModelLoader = new();

            ButtonActionStack.Push(ProcessButton);
            ButtonActionStack.Push(ProcessButtonWithCredit);

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayerModel.Content.playermodelassets");
            Bundle = AssetBundle.LoadFromStream(stream);
            StandAsset = Bundle.LoadAsset<GameObject>("Stand");
            CallButtonAsset = Bundle.LoadAsset<GameObject>("CallButton");

            Stand = Instantiate(StandAsset).AddComponent<Stand>();

#if !DEBUG
            Stand.gameObject.SetActive(city_active);
            ZoneManagement.OnZoneChange += OnZoneChanged;
#endif

            var gorilla_tagger = GorillaTagger.Instance;

            if (gorilla_tagger.leftHandTriggerCollider.TryGetComponent(out left_trigger)) base_left_index = left_trigger.transformToFollow;
            if (gorilla_tagger.rightHandTriggerCollider.TryGetComponent(out right_trigger)) base_right_index = right_trigger.transformToFollow;

            var local_rig = gorilla_tagger.offlineVRRig;

            FirstPersonRig = local_rig.gameObject.AddComponent<Rig>();
            FirstPersonRig.ControllingRig = local_rig;
            FirstPersonRig.Layer = UnityLayer.FirstPersonOnly;
            FirstPersonRig.PlayerModelAlias = "PlayerModel FirstPerson";
            FirstPersonRig.OnModelLoaded += AssignTransformFollow;
            FirstPersonRig.OnModelUnloaded += ResetTransformFollow;
            FirstPersonRig.OnModelLoaded += SendModel;
            FirstPersonRig.OnModelUnloaded += RetractModel;

            ThirdPersonRig = local_rig.gameObject.AddComponent<Rig>();
            ThirdPersonRig.ControllingRig = local_rig;
            ThirdPersonRig.Layer = UnityLayer.MirrorOnly;
            ThirdPersonRig.PlayerModelAlias = "PlayerModel ThirdPerson";
        }

        private void AssignTransformFollow(Descriptor descriptor)
        {
            ResetTransformFollow();
            if (descriptor.Digits != null && descriptor.Digits.Count > 0)
            {
                if (descriptor.Digits.Find(finger => finger.Node == XRNode.LeftHand && finger.Type == EDigitType.Index) is Finger left_index_finger)
                {
                    var left_fingertip = left_index_finger.Bones.Last();
                    if (left_fingertip.childCount > 0) left_fingertip = left_fingertip.GetChild(0);
                    left_trigger.transformToFollow = left_fingertip;
                }
                if (descriptor.Digits.Find(finger => finger.Node == XRNode.RightHand && finger.Type == EDigitType.Index) is Finger right_index_finger)
                {
                    var right_fingertip = right_index_finger.Bones.Last();
                    if (right_fingertip.childCount > 0) right_fingertip = right_fingertip.GetChild(0);
                    right_trigger.transformToFollow = right_fingertip;
                }
            }
        }

        private void ResetTransformFollow()
        {
            left_trigger.transformToFollow = base_left_index;
            right_trigger.transformToFollow = base_right_index;
        }

        private void SendModel(Descriptor descriptor)
        {
            NetworkHandler.Instance.SetProperty("Model", string.Concat(descriptor.ModelName, descriptor.Author));
        }

        private void RetractModel()
        {
            NetworkHandler.Instance.SetProperty("Model", string.Empty);
        }

        public void OnZoneChanged(ZoneData[] zones)
        {
            ZoneData[] city_zones = Array.FindAll(zones, zone => zone.zone == GTZone.city || zone.zone == GTZone.cityNoBuildings || zone.zone == GTZone.cityWithSkyJungle);
            city_active = city_zones.Any(zone => zone.active);
            Stand.gameObject.SetActive(city_active);
        }

        public void ButtonPress(EButtonClass button)
        {
            if (ButtonActionStack.Count == 0)
            {
                ButtonActionStack.Push(ProcessButton);
            }
            ButtonActionStack.Peek().Invoke(button);
        }

        public void ProcessButton(EButtonClass button)
        {
            switch (button)
            {
                case EButtonClass.Select:
                    Button.GlobalButtonCoolown = Time.realtimeSinceStartup + 0.75f;
                    Model model = ModelLoader.Models[model_selection_index];
                    if (model != FirstPersonRig.Model)
                    {
                        FirstPersonRig.LoadModel(model);
                        ThirdPersonRig.LoadModel(model);
                    }
                    else
                    {
                        FirstPersonRig.UnloadModel();
                        ThirdPersonRig.UnloadModel();
                    }
                    break;
                case EButtonClass.NavLeft:
                    model_selection_index = MathEx.Wrap(model_selection_index - 1, 0, ModelLoader.Models.Count);
                    Stand.ApplyPlayerModel(ModelLoader.Models[model_selection_index]);
                    break;
                case EButtonClass.NavRight:
                    model_selection_index = MathEx.Wrap(model_selection_index + 1, 0, ModelLoader.Models.Count);
                    Stand.ApplyPlayerModel(ModelLoader.Models[model_selection_index]);
                    break;
                case EButtonClass.MaterialFur:
                case EButtonClass.MaterialRock:
                case EButtonClass.MaterialLava:
                case EButtonClass.MaterialIce:
                    Stand.SetPreviewModelMaterial(button);
                    break;
            }
        }

        public void ProcessButtonWithCredit(EButtonClass button)
        {
            if (button >= 0 && button < (EButtonClass)3)
            {
                ButtonActionStack.Pop();
                Stand.ApplyPlayerModel(ModelLoader.Models[model_selection_index]);
            }
        }

        public Material CreateUpdatedMaterial(Material old_material)
        {
            if (old_material.shader == uber_shader)
            {
                return old_material;
            }
            var newmat = new Material(uber_shader)
            {
                shaderKeywords =
                [
                    "_USE_TEXTURE",
                    "_WATER_EFFECT",
                    "_REFLECTIONS_MATCAP_PERSP_AWARE",
                    "_HEIGHT_BASED_WATER_EFFECT",
                    "_GT_RIM_LIGHT_USE_ALPHA",
                    "_GT_RIM_LIGHT",
                    "_EMISSION"
                ],
                name = old_material.name
            };
            newmat.enabledKeywords = [.. newmat.shaderKeywords.Select(kword => new LocalKeyword(uber_shader, kword))];
            newmat.SetFloat("_Cull", 2f);
            if (newmat.HasFloat("_Cull"))
            {
                newmat.SetFloat("_Cull", 2f);
            }
            string color_ = "_Color";
            string tex_ = "_MainTex";
            if (old_material.shader.name == "Custom/playermodel_new" || old_material.shader.name == "Custom/playermodel")
            {
                tex_ = "_texture";
            }
            if (old_material.HasProperty(color_))
            {
                newmat.color = old_material.GetColor(color_);
            }
            if (old_material.HasProperty(tex_))
            {
                Texture texture = old_material.GetTexture(tex_);
                if (texture != null)
                {
                    newmat.mainTexture = texture;
                }
            }
            return newmat;
        }

        public void UpdateModelMaterials(GameObject obj)
        {
            if (obj.TryGetComponent(out Renderer rend))
            {
                Material[] updatedMaterials = new Material[rend.materials.Length];
                for (int j = 0; j < updatedMaterials.Length; j++)
                {
                    updatedMaterials[j] = CreateUpdatedMaterial(rend.materials[j]);
                }
                rend.materials = updatedMaterials;
                for (int i = 0; i < updatedMaterials.Length; i++)
                {
                    rend.materials[i].name = updatedMaterials[i].name;
                }
            }
        }
    }
}
