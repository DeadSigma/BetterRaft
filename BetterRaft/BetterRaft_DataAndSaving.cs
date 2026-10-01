using System.IO;
using UnityEngine;
using HMLLibrary;
using System.Text;

namespace BetterRaft
{
    [System.Serializable]
    public class LungProgressionData {
        public int LungLevel;
        public int SignificantDivesCounter;
    }

    [System.Serializable]
    public class FatigueProgressionData {
        public int FatigueLevel;
        public int FatigueActivityCounter;
    }

    [System.Serializable]
    public class FishingRodProgressionData {
        public int FishingLevel;
        public int FishingExperience;
    }

    [System.Serializable]
    public class PlayerModSaveData {
        public LungProgressionData LungData = new LungProgressionData();
        public FatigueProgressionData FatigueData = new FatigueProgressionData();
        public FishingRodProgressionData FishingRodData = new FishingRodProgressionData();
    }

    public static class ModDataManager {
        public const string MOD_SAVE_FOLDER_NAME = "BetterRaft";
        private static readonly string ModDataRootPath;

        static ModDataManager() {
            string modsRoot = Path.Combine(Application.dataPath, "..", "Mods");
            try {
                if (typeof(HLib) != null && !string.IsNullOrEmpty(HLib.path_modsFolder)) {
                    modsRoot = HLib.path_modsFolder;
                } else {
                    
                }
            } catch (System.Exception) {
                
            }
            ModDataRootPath = Path.Combine(modsRoot, "ModData", MOD_SAVE_FOLDER_NAME);
            try {
                Directory.CreateDirectory(ModDataRootPath);
            }
            catch (System.Exception) {
               
            }
        }

        private static string GetSaveFilePath(string worldIdentifier) {
            string sanitizedIdentifier = worldIdentifier;
            if (string.IsNullOrEmpty(sanitizedIdentifier)) {
                sanitizedIdentifier = "unknown_world";
            }
            foreach (char c in Path.GetInvalidFileNameChars()) {
                sanitizedIdentifier = sanitizedIdentifier.Replace(c, '_');
            }
            return Path.Combine(ModDataRootPath, $"{sanitizedIdentifier}_progress.json");
        }

        public static void SaveCombinedProgression(string worldIdentifier, PlayerModSaveData data) {
            if (string.IsNullOrEmpty(worldIdentifier)) {
               
                return;
            }
            if (data == null)
            {
               
                return;
            }
            try {
                string filePath = GetSaveFilePath(worldIdentifier);
                string directoryPath = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directoryPath)) {
                    try {
                        Directory.CreateDirectory(directoryPath);
                       
                    } catch (System.Exception) {
                        
                        return;
                    }
                }

                if (data.LungData == null) data.LungData = new LungProgressionData();
                if (data.FatigueData == null) data.FatigueData = new FatigueProgressionData();
                if (data.FishingRodData == null) data.FishingRodData = new FishingRodProgressionData();

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"LungData\":{");
                sb.Append($"\"LungLevel\":{data.LungData.LungLevel},");
                sb.Append($"\"SignificantDivesCounter\":{data.LungData.SignificantDivesCounter}");
                sb.Append("},");
                sb.Append("\"FatigueData\":{");
                sb.Append($"\"FatigueLevel\":{data.FatigueData.FatigueLevel},");
                sb.Append($"\"FatigueActivityCounter\":{data.FatigueData.FatigueActivityCounter}");
                sb.Append("},");
                sb.Append("\"FishingRodData\":{");
                sb.Append($"\"FishingLevel\":{data.FishingRodData.FishingLevel},");
                sb.Append($"\"FishingExperience\":{data.FishingRodData.FishingExperience}");
                sb.Append("}");
                sb.Append("}");
                string json = sb.ToString();

                File.WriteAllText(filePath, json);
                
            }
            catch (System.Exception) {
               
            }
        }

        private static int ParseIntValueManually(string json, string key, string sectionKey = null)
        {
            string originalJsonForLogging = json; 
            string searchJson = json;
            if (!string.IsNullOrEmpty(sectionKey))
            {
                string sectionStartTag = $"\"{sectionKey}\":{{";
                int sectionStartIndex = json.IndexOf(sectionStartTag);
                if (sectionStartIndex == -1) {
                    
                    return 0;
                }

                int sectionContentStartIndex = sectionStartIndex + sectionStartTag.Length;
                int braceCounter = 1;
                int sectionEndIndex = -1;
                for(int i = sectionContentStartIndex; i < json.Length; i++)
                {
                    if(json[i] == '{') braceCounter++;
                    else if(json[i] == '}') braceCounter--;
                    if (braceCounter == 0)
                    {
                        sectionEndIndex = i;
                        break;
                    }
                }

                if (sectionEndIndex == -1) {
                   
                    return 0;
                }
                searchJson = json.Substring(sectionContentStartIndex, sectionEndIndex - sectionContentStartIndex);
                
            }

            string tag = $"\"{key}\":";
            int tagIndex = searchJson.IndexOf(tag);
            if (tagIndex == -1) {
                
                return 0;
            }

            int valueStartIndex = tagIndex + tag.Length;
            int valueEndIndex = searchJson.IndexOfAny(new char[] { ',', '}' }, valueStartIndex);

            if (valueEndIndex == -1) {
               
                valueEndIndex = searchJson.Length;
            }

            string valueStr = searchJson.Substring(valueStartIndex, valueEndIndex - valueStartIndex).Trim();
            if (int.TryParse(valueStr, out int result))
            {
               
                return result;
            }
            else
            {
                
                return 0;
            }
        }

        public static PlayerModSaveData LoadCombinedProgression(string worldIdentifier) {
            if (string.IsNullOrEmpty(worldIdentifier)) {
               
                return new PlayerModSaveData();
            }
            string filePath = GetSaveFilePath(worldIdentifier);
            PlayerModSaveData data = new PlayerModSaveData();

            if (File.Exists(filePath)) {
                try {
                    string json = File.ReadAllText(filePath);
                   
                    if (string.IsNullOrWhiteSpace(json) || json == "{}") {
                        
                        return data;
                    }

                    data.LungData.LungLevel = ParseIntValueManually(json, "LungLevel", "LungData");
                    data.LungData.SignificantDivesCounter = ParseIntValueManually(json, "SignificantDivesCounter", "LungData");

                    data.FatigueData.FatigueLevel = ParseIntValueManually(json, "FatigueLevel", "FatigueData");
                    data.FatigueData.FatigueActivityCounter = ParseIntValueManually(json, "FatigueActivityCounter", "FatigueData");

                    data.FishingRodData.FishingLevel = ParseIntValueManually(json, "FishingLevel", "FishingRodData");
                    data.FishingRodData.FishingExperience = ParseIntValueManually(json, "FishingExperience", "FishingRodData");

                    
                    return data;
                }
                catch (System.Exception) {
                  
                    return new PlayerModSaveData();
                }
            }
            else
            {
                
            }
            return data;
        }
    }
}