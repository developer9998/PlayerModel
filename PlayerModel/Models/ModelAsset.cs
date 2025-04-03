using System;
using System.Linq;
using PlayerModel.Behaviours;
using UnityEngine;

namespace PlayerModel.Models
{
    public class ModelAsset : IModel
    {
        public string FilePath { get; }
        public ModelDescriptor Descriptor { get; }
        public GameObject Template { get; }

        public ModelAsset(string path)
        {
            try
            {
                FilePath = path;

                var assetBundle = AssetBundle.LoadFromFile(path);
                Template = assetBundle.LoadAsset<GameObject>("PlayerModelAsset");

                foreach (Collider collider in Template.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }

                if (Template.TryGetComponent(out ModelDescriptor descriptor))
                {
                    Descriptor = descriptor;
                    Descriptor.Version = EModelVersion.Current;
                    var material_array = Descriptor.MainMesh.sharedMaterials.ToList();
                    if (descriptor.CustomColors && descriptor.BaseMaterial)
                    {
                        int primative_base_index = material_array.IndexOf(descriptor.BaseMaterial);
                        descriptor.BaseMaterialIndex = primative_base_index > -1 ? primative_base_index : null;
                    }
                    if (descriptor.GameModeMaterials && descriptor.GameMaterial)
                    {
                        int primative_game_index = material_array.IndexOf(descriptor.GameMaterial);
                        descriptor.GameMaterialIndex = primative_game_index > -1 ? primative_game_index : null;
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
            return $"{Descriptor.Author}: {Descriptor.ModelName}";
        }
    }
}
