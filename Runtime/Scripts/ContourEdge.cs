using System;
using UnityEngine;

namespace MrGVSV.PixelContour
{
    internal readonly struct ContourEdge : IEquatable<ContourEdge>
    {
        /// <summary>
        /// The starting corner of a pixel
        /// </summary>
        public readonly Vector2Int Start;
        /// <summary>
        /// The ending corner of a pixel
        /// </summary>
        public readonly Vector2Int End;

        public ContourEdge(Vector2Int start, Vector2Int end)
        {
            Start = start;
            End = end;
        }

        public bool Equals(ContourEdge other)
        {
            return Start.Equals( other.Start ) && End.Equals( other.End );
        }

        public override bool Equals(object obj)
        {
            return obj is ContourEdge other && Equals( other );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ( Start.GetHashCode() * 397 ) ^ End.GetHashCode();
            }
        }
    }
}