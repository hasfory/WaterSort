using System.Collections.Generic;
using UnityEngine;

namespace WaterSort
{
    /// Persists player progress using Unity's PlayerPrefs.
    /// Stores: current level, best move counts per level.
    public static class SaveSystem
    {
        private const string KEY_CURRENT_LEVEL  = "ws_currentLevel";
        private const string KEY_BEST_MOVES_FMT = "ws_best_{0}";   // {0} = level index

        // ── Level progress ────────────────────────────────────────────────────────

        public static int LoadCurrentLevel()
        {
            return PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 1);
        }

        public static void SaveCurrentLevel(int level)
        {
            PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, level);
            PlayerPrefs.Save();
        }

        // ── Best move records ─────────────────────────────────────────────────────

        public static int LoadBestMoves(int levelIndex)
        {
            return PlayerPrefs.GetInt(string.Format(KEY_BEST_MOVES_FMT, levelIndex), int.MaxValue);
        }

        public static bool TrySaveBestMoves(int levelIndex, int moves)
        {
            int current = LoadBestMoves(levelIndex);
            if (moves < current)
            {
                PlayerPrefs.SetInt(string.Format(KEY_BEST_MOVES_FMT, levelIndex), moves);
                PlayerPrefs.Save();
                return true; // new record!
            }
            return false;
        }

        // ── Reset ─────────────────────────────────────────────────────────────────

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
