using System.Collections.Generic;
using UnityEngine;
using WaterSort.Model;

namespace WaterSort.Model
{
    /// <summary>
    /// Pure game-logic model. No Unity dependencies.
    /// Manages flask states, move validation, undo history, and win/lose detection.
    /// </summary>
    public class GameState
    {
        public List<FlaskData> Flasks { get; private set; }
        private Stack<List<FlaskData>> _undoStack = new Stack<List<FlaskData>>();

        public int MoveCount { get; private set; }
        public bool IsWon  => CheckWin();
        public bool IsLost => !IsWon && !HasAnyValidMove();

        // ── Construction ──────────────────────────────────────────────────────────

        public GameState(List<FlaskData> flasks)
        {
            Flasks = flasks;
            MoveCount = 0;
        }

        // ── Move API ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true and executes the pour if the move is legal.
        /// </summary>
        public bool TryPour(int fromIndex, int toIndex)
        {
            if (!CanPour(fromIndex, toIndex)) return false;

            SaveSnapshot();

            FlaskData src = Flasks[fromIndex];
            FlaskData dst = Flasks[toIndex];

            int color      = src.TopColor;
            int topCount   = src.TopCount();
            int transferable = Mathf.Min(topCount, dst.FreeSpace);

            for (int i = 0; i < transferable; i++)
            {
                src.layers.RemoveAt(src.layers.Count - 1);
                dst.layers.Add(color);
            }

            MoveCount++;
            return true;
        }

        /// <summary>Checks all four validity criteria from the design document.</summary>
        public bool CanPour(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return false;

            FlaskData src = Flasks[fromIndex];
            FlaskData dst = Flasks[toIndex];

            // (1) Source must not be empty
            if (src.IsEmpty) return false;

            // (2) Destination must not be full
            if (dst.IsFull) return false;

            // (3) Colors must match or destination is empty
            if (!dst.IsEmpty && dst.TopColor != src.TopColor) return false;

            // (4) Avoid no-op: moving a fully-sorted single-color flask into an empty flask
            if (dst.IsEmpty && src.IsSorted) return false;

            return true;
        }

        // ── Undo ──────────────────────────────────────────────────────────────────

        public bool CanUndo => _undoStack.Count > 0;

        public void Undo()
        {
            if (!CanUndo) return;
            Flasks = _undoStack.Pop();
            if (MoveCount > 0) MoveCount--;
        }

        // ── Win / Lose ────────────────────────────────────────────────────────────

        private bool CheckWin()
        {
            foreach (var f in Flasks)
                if (!f.IsEmpty && !f.IsSorted) return false;
            return true;
        }

        private bool HasAnyValidMove()
        {
            for (int i = 0; i < Flasks.Count; i++)
                for (int j = 0; j < Flasks.Count; j++)
                    if (i != j && CanPour(i, j)) return true;
            return false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SaveSnapshot()
        {
            var snapshot = new List<FlaskData>(Flasks.Count);
            foreach (var f in Flasks) snapshot.Add(f.Clone());
            _undoStack.Push(snapshot);
        }

        public List<FlaskData> CloneFlasks()
        {
            var result = new List<FlaskData>(Flasks.Count);
            foreach (var f in Flasks) result.Add(f.Clone());
            return result;
        }

        public void Reset(List<FlaskData> initialState)
        {
            Flasks = initialState;
            _undoStack.Clear();
            MoveCount = 0;
        }
    }
}
