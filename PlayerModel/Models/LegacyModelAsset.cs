using System;
using System.Linq;
using PlayerModel.Behaviours;
using UnityEngine;
using Text = UnityEngine.UI.Text;

namespace PlayerModel.Models
{
    public class LegacyModelAsset : IModel
    {
        public string FilePath { get; }
        public ModelDescriptor Descriptor { get; }
        public GameObject Template { get; }

        public LegacyModelAsset(string path)
        {
            try
            {
                FilePath = path;

                var assetBundle = AssetBundle.LoadFromFile(path);
                Template = assetBundle.LoadAsset<GameObject>("playermodel.ParentObject");

                var main_mesh_object = Template.transform.FindChildRecursive(Constants.BoneMainMesh);
                Singleton<Main>.Instance.UpdateModelMaterials(main_mesh_object.gameObject);

                foreach (Collider collider in Template.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }

                if (Template.TryGetComponent(out Text text))
                {
                    text.enabled = false;
                    var properties = text.text.Split('$');

                    Descriptor = Template.AddComponent<ModelDescriptor>();

                    Descriptor.head = Template.transform.FindChildRecursive(Constants.BoneHead);
                    Descriptor.body = Template.transform.FindChildRecursive(Constants.BoneBody);
                    Descriptor.leftHand = Template.transform.FindChildRecursive(Constants.BoneLeftHand);
                    Descriptor.rightHand = Template.transform.FindChildRecursive(Constants.BoneRightHand);

                    Descriptor.ModelName = properties.ElementAtOrDefault((int)EModelProperty.ModelName) is string model_name
                        ? model_name
                        : "N/A";
                    Descriptor.Author = properties.ElementAtOrDefault((int)EModelProperty.ModelAuthor) is string author
                        ? author
                        : "N/A";

                    Descriptor.Version = EModelVersion.Legacy1;

                    Descriptor.BaseMaterialIndex = 0;

                    Descriptor.GameMaterialIndex = 0;

                    var model_renderer = main_mesh_object.GetComponent<SkinnedMeshRenderer>();

                    if (properties.Length >= (int)EModelProperty.UseCustomColours)
                    {
                        Descriptor.Version = EModelVersion.Legacy2;

                        Descriptor.CustomColors = properties.ElementAtOrDefault((int)EModelProperty.UseCustomColours) is string custom_colour_string
                            && bool.TryParse(custom_colour_string, out bool custom_colour)
                            && custom_colour;

                        Descriptor.GameModeMaterials = properties.ElementAtOrDefault((int)EModelProperty.UseGameModeMaterials) is string mode_texture_string
                            && bool.TryParse(mode_texture_string, out bool mode_textures)
                            && mode_textures;

                        string colour_material_name = properties.ElementAtOrDefault((int)EModelProperty.ColourMaterial) is string cm ? cm : string.Empty;
                        string mode_material_name = properties.ElementAtOrDefault((int)EModelProperty.GameModeMaterial) is string gmm ? gmm : string.Empty;

                        var material_array = model_renderer.sharedMaterials;

                        for (int i = 0; i < material_array.Length; i++)
                        {
                            var material = material_array[i];
                            if (colour_material_name != string.Empty && colour_material_name == material.name || string.Concat(colour_material_name, " (Instance)") == material.name)
                            {
                                Descriptor.BaseMaterial = material;
                                Descriptor.BaseMaterialIndex = i;
                                continue;
                            }
                            if (mode_material_name != string.Empty && mode_material_name == material.name || string.Concat(mode_material_name, " (Instance)") == material.name)
                            {
                                Descriptor.GameMaterial = material;
                                Descriptor.GameMaterialIndex = i;
                                continue;
                            }
                        }

                        if (properties.Length >= (int)EModelProperty.UseLipSync)
                        {
                            Descriptor.Version = EModelVersion.Legacy3;

                            Descriptor.LipSync = properties.ElementAtOrDefault((int)EModelProperty.UseLipSync) is string lip_sync_string
                                && bool.TryParse(lip_sync_string, out bool lip_sync)
                                && lip_sync;
                        }
                    }
                }

                Template.SetActive(false);

                assetBundle.Unload(false);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public override string ToString()
        {
            return $"{Descriptor.Author}: {Descriptor.ModelName} ({Descriptor.Version})";
        }
    }
}
