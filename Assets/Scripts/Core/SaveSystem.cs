using System;
using System.IO;
using UnityEngine;

namespace DigiPurity.Core
{
    [Serializable]
    public class GameSaveData
    {
        public string playerName = "Cole";
        public int purityLevel = 1;
        public int maxHp = 20;
        public int currentHp = 20;
        public int purifiedCount = 0;
        public int purgedCount = 0;
        public string currentSector = "Sector_01_AI_Wasteland";
    }

    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "digipurity_save.json");

        public static void Save(GameSaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static GameSaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                return new GameSaveData();
            }
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
    }
}
