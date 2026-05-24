using System.Collections.Generic;
using UnityEngine;

namespace WaterSort.Model
{
    /// <summary>
    /// Represents a single flask with a stack of colored liquid layers.
    /// </summary>
    [System.Serializable]
    public class FlaskData
    {
        public int capacity;
        public List<int> layers; // color indices, bottom to top

        public FlaskData(int capacity)
        {
            this.capacity = capacity;
            layers = new List<int>(capacity);
        }

        public FlaskData Clone()
        {
            var clone = new FlaskData(capacity);
            clone.layers.AddRange(layers);
            return clone;
        }

        public bool IsEmpty  => layers.Count == 0;
        public bool IsFull   => layers.Count >= capacity;
        public bool IsSorted => IsEmpty || (layers.Count == capacity && AllSameColor());
        public int  TopColor => IsEmpty ? -1 : layers[layers.Count - 1];
        public int  FreeSpace => capacity - layers.Count;

        private bool AllSameColor()
        {
            int c = layers[0];
            foreach (int l in layers) if (l != c) return false;
            return true;
        }

        /// <summary>How many consecutive identical top layers can be poured.</summary>
        public int TopCount()
        {
            if (IsEmpty) return 0;
            int color = TopColor;
            int count = 0;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i] == color) count++;
                else break;
            }
            return count;
        }
    }
}
