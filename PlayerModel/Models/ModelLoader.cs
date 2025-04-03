using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PlayerModel.Models
{
    public class ModelLoader
    {
        public List<IModel> Models;

        public ModelLoader()
        {
            Models = GetAllModels();
        }

        public List<IModel> GetAllModels()
        {
            List<IModel> new_player_models = [];

            string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "PlayerModels");
            if (Directory.Exists(path))
            {
                var model_files = Directory.GetFiles(path, "*.gtplayermodel", SearchOption.TopDirectoryOnly);
                new_player_models.AddRange(LoadModels(model_files));
            }

            string legacy_path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "PlayerAssets");
            if (Directory.Exists(legacy_path))
            {
                var legacy_model_files = Directory.GetFiles(legacy_path, "*.gtmodel", SearchOption.TopDirectoryOnly);
                new_player_models.AddRange(LoadLegacyModels(legacy_model_files));
            }

            new_player_models.Sort((x, y) => x.Descriptor.ModelName.CompareTo(y.Descriptor.ModelName));

            return new_player_models;
        }

        public List<ModelAsset> LoadModels(IEnumerable<string> files)
        {
            List<ModelAsset> player_models = [];

            foreach (var file in files)
            {
                try
                {
                    player_models.Add(new ModelAsset(file));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error when adding new PlayerModel: {ex}");
                    File.Move(file, string.Concat(file, ".broken"));
                    Debug.LogWarning($"File {Path.GetFileNameWithoutExtension(file)} is broken");
                }
            }

            return player_models;
        }

        public List<LegacyModelAsset> LoadLegacyModels(IEnumerable<string> files)
        {
            List<LegacyModelAsset> player_models = [];

            foreach (var file in files)
            {
                try
                {
                    player_models.Add(new LegacyModelAsset(file));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error when adding new PlayerModel: {ex}");
                    File.Move(file, string.Concat(file, ".broken"));
                    Debug.LogWarning($"File {Path.GetFileNameWithoutExtension(file)} is broken");
                }
            }

            return player_models;
        }

        public bool GetPlayerModel(string url)
        {
            throw new NotImplementedException();
        }

#if DEBUG

        public void InstallPlayerModels()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "PlayerAssets");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            List<string> embedded_models = Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();

            for (int i = 0; i < embedded_models.Count; i++)
            {
                if (!embedded_models[i].EndsWith(".gtmodel"))
                {
                    embedded_models.Remove(embedded_models[i]);
                }
            }

            foreach (string model in embedded_models)
            {
                string filename = model.Replace("PlayerModel.PlayerAssets.", "");
                MemoryStream ms = new();
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(model);
                stream.CopyTo(ms);   // copy to buffer
                byte[] bb = ms.ToArray();   // need array to save
                File.WriteAllBytes(Path.Combine(path, filename), bb);   // save byte array to file
            }
        }
#endif

    }
}
