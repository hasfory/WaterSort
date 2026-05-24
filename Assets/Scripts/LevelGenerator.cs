using System.Collections.Generic;
using UnityEngine;
using WaterSort.Model;

namespace WaterSort
{
    /// Generates level data both from hand-authored definitions and procedurally.
    /// Procedural algorithm: fill → shuffle → distribute → validate solvability.
    public static class LevelGenerator
    {
        // ── Progression table from design document ────────────────────────────────

        private static readonly (int colors, int depth, int empty)[] ProgressionTable =
        {
            (3, 4, 2),   // levels  1–5
            (4, 4, 2),   // levels  6–10
            (5, 4, 2),   // levels 11–15
            (6, 5, 2),   // levels 16–20
            (7, 5, 1),   // levels 21–30
            (8, 5, 1),   // levels 31–40
            (10, 6, 1),  // levels 41–50
        };

        // ── Hand-authored starter levels (levels 1–3) ─────────────────────────────

        private static readonly List<List<List<int>>> HandCraftedLevels = new()
        {
            // Level 1 – very easy intro (3 colors, depth 4, 2 empty)
            new() {
                new() { 0, 1, 2, 0 },
                new() { 1, 0, 2, 1 },
                new() { 2, 2, 0, 1 },
                new() { },
                new() { },
            },
            // Level 2 – slightly harder
            new() {
                new() { 0, 1, 2, 1 },
                new() { 2, 0, 1, 0 },
                new() { 1, 2, 0, 2 },
                new() { },
                new() { },
            },
            // Level 3 – 4 colors appear
            new() {
                new() { 0, 1, 2, 3 },
                new() { 3, 0, 1, 2 },
                new() { 2, 3, 0, 1 },
                new() { 1, 2, 3, 0 },
                new() { },
                new() { },
            },
        };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns a GameState for the requested 1-based level index.</summary>
        public static GameState Generate(int levelIndex)
        {
            // Use hand-crafted levels for the first few
            if (levelIndex <= HandCraftedLevels.Count)
                return BuildFromDefinition(HandCraftedLevels[levelIndex - 1], 4);

            var (colors, depth, empty) = GetProgression(levelIndex);
            return GenerateProcedural(colors, depth, empty);
        }

        // ── Procedural generation ─────────────────────────────────────────────────

        public static GameState GenerateProcedural(int numColors, int depth, int emptyFlasks, int maxAttempts = 50)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var flasks = TryGenerate(numColors, depth, emptyFlasks);
                if (flasks != null)
                    return new GameState(flasks);
            }

            // Fallback: generate without solvability check
            Debug.LogWarning($"[LevelGenerator] Could not verify solvability after {maxAttempts} attempts. Using last result.");
            return new GameState(TryGenerate(numColors, depth, emptyFlasks, skipCheck: true));
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private static List<FlaskData> TryGenerate(int numColors, int depth, int emptyFlasks, bool skipCheck = false)
        {
            // Step 1: fill array with exactly (numColors * depth) colored layers
            var pool = new List<int>(numColors * depth);
            for (int c = 0; c < numColors; c++)
                for (int d = 0; d < depth; d++)
                    pool.Add(c);

            // Step 2: Fisher-Yates shuffle
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            // Step 3: distribute into flasks
            int totalFlasks = numColors + emptyFlasks;
            var flasks = new List<FlaskData>(totalFlasks);
            for (int f = 0; f < numColors; f++)
            {
                var flask = new FlaskData(depth);
                for (int d = 0; d < depth; d++)
                    flask.layers.Add(pool[f * depth + d]);
                flasks.Add(flask);
            }
            for (int e = 0; e < emptyFlasks; e++)
                flasks.Add(new FlaskData(depth));

            if (skipCheck) return flasks;

            // Step 4: quick solvability check via simulation (BFS-lite, limited depth)
            return IsProbablySolvable(flasks, depth) ? flasks : null;
        }

        private static bool IsProbablySolvable(List<FlaskData> flasks, int depth)
        {
            // Clone state and run a greedy solver for up to 500 iterations
            var state = new GameState(new List<FlaskData>(flasks.Count));
            // Re-clone manually
            var clones = new List<FlaskData>(flasks.Count);
            foreach (var f in flasks) clones.Add(f.Clone());
            state = new GameState(clones);

            for (int iter = 0; iter < 500; iter++)
            {
                if (state.IsWon) return true;
                bool moved = false;
                for (int i = 0; i < state.Flasks.Count && !moved; i++)
                    for (int j = 0; j < state.Flasks.Count && !moved; j++)
                        if (state.TryPour(i, j)) moved = true;
                if (!moved) break;
            }
            return state.IsWon;
        }

        private static GameState BuildFromDefinition(List<List<int>> def, int depth)
        {
            var flasks = new List<FlaskData>(def.Count);
            foreach (var layerList in def)
            {
                var f = new FlaskData(depth);
                f.layers.AddRange(layerList);
                flasks.Add(f);
            }
            return new GameState(flasks);
        }

        private static (int colors, int depth, int empty) GetProgression(int levelIndex)
        {
            if (levelIndex <= 5)   return ProgressionTable[0];
            if (levelIndex <= 10)  return ProgressionTable[1];
            if (levelIndex <= 15)  return ProgressionTable[2];
            if (levelIndex <= 20)  return ProgressionTable[3];
            if (levelIndex <= 30)  return ProgressionTable[4];
            if (levelIndex <= 40)  return ProgressionTable[5];
            return ProgressionTable[6];
        }
    }
}
