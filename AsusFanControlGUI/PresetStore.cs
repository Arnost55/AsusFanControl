using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace AsusFanControlGUI
{
    internal sealed class PresetStore
    {
        private readonly string filePath;

        public PresetStore()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appData, "AsusFanControl");
            filePath = Path.Combine(directory, "presets.json");
        }

        public string FilePath
        {
            get { return filePath; }
        }

        public List<Preset> LoadPresets()
        {
            if (!File.Exists(filePath))
            {
                var defaults = CreateDefaultPresets();
                SavePresets(defaults);
                return defaults;
            }

            try
            {
                var serializer = new JavaScriptSerializer();
                var json = File.ReadAllText(filePath);
                var document = serializer.Deserialize<PresetFile>(json);
                var loadedPresets = document == null ? null : document.Presets;
                var normalized = NormalizePresets(loadedPresets);
                SavePresets(normalized);
                return normalized;
            }
            catch
            {
                BackupCorruptFile();
                var defaults = CreateDefaultPresets();
                SavePresets(defaults);
                return defaults;
            }
        }

        public void SavePresets(IList<Preset> presets)
        {
            var safePresets = presets == null
                ? new List<Preset>()
                : presets.Where(p => p != null).ToList();

            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = new JavaScriptSerializer();
            var document = new PresetFile
            {
                Version = 1,
                Presets = safePresets
            };

            File.WriteAllText(filePath, serializer.Serialize(document));
        }

        private void BackupCorruptFile()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupPath = Path.Combine(Path.GetDirectoryName(filePath), "presets.corrupt-" + stamp + ".json");
                File.Copy(filePath, backupPath, true);
            }
            catch
            {
                // Corrupt-file backup is best effort only.
            }
        }

        private static List<Preset> NormalizePresets(IEnumerable<Preset> loadedPresets)
        {
            var defaults = CreateDefaultPresets();
            var result = new List<Preset>(defaults);
            var existingIds = new HashSet<string>(defaults.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
            var existingNames = new HashSet<string>(defaults.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            if (loadedPresets == null)
            {
                return result;
            }

            foreach (var preset in loadedPresets)
            {
                if (preset == null || preset.IsBuiltIn)
                {
                    continue;
                }

                var id = string.IsNullOrWhiteSpace(preset.Id)
                    ? Guid.NewGuid().ToString("N")
                    : preset.Id.Trim();

                if (existingIds.Contains(id))
                {
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(preset.Name) ? "Custom preset" : preset.Name.Trim();
                name = MakeUniqueName(name, existingNames);

                result.Add(new Preset
                {
                    Id = id,
                    Name = name,
                    Speed = ClampSpeed(preset.Speed),
                    IsBuiltIn = false
                });

                existingIds.Add(id);
                existingNames.Add(name);
            }

            return result;
        }

        private static string MakeUniqueName(string desiredName, ISet<string> existingNames)
        {
            var candidate = desiredName;
            var suffix = 2;

            while (existingNames.Contains(candidate))
            {
                candidate = desiredName + " (" + suffix + ")";
                suffix++;
            }

            return candidate;
        }

        private static int ClampSpeed(int speed)
        {
            if (speed < 0)
            {
                return 0;
            }

            if (speed > 100)
            {
                return 100;
            }

            return speed;
        }

        private static List<Preset> CreateDefaultPresets()
        {
            return new List<Preset>
            {
                new Preset
                {
                    Id = "silent",
                    Name = "Silent",
                    Speed = 40,
                    IsBuiltIn = true
                },
                new Preset
                {
                    Id = "balanced",
                    Name = "Balanced",
                    Speed = 55,
                    IsBuiltIn = true
                },
                new Preset
                {
                    Id = "performance",
                    Name = "Performance",
                    Speed = 75,
                    IsBuiltIn = true
                },
                new Preset
                {
                    Id = "turbo",
                    Name = "Turbo",
                    Speed = 99,
                    IsBuiltIn = true
                }
            };
        }

        private sealed class PresetFile
        {
            public int Version { get; set; }

            public List<Preset> Presets { get; set; }
        }
    }
}
