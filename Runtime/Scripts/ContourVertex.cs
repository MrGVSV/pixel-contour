using System;
using UnityEngine;

namespace MrGVSV.PixelContour
{
    public readonly struct ContourVertex : IEquatable<ContourVertex>
    {
        /// <summary>
        /// The position of this vertex
        /// </summary>
        public readonly Vector2 Position;
        /// <summary>
        /// The "pixel-normal" of this vertex, where "pixel-normal" is simply the normal of a vertex rounded
        /// to the best-fit pixel (ex. a normal of <c>( 0.7f, 0.7f )</c> will become <c>( 1f, 1f )</c>,
        /// whereas <c>( 1f, 0f )</c> will stay the same)
        /// </summary>
        /// <remarks>
        /// Rounding it to the best-fit pixel allows expand and shrink operations to be performed much easier.
        /// For example, a normal of 45 degrees will point have a PixelNormal of ( 1f, 1f ), meaning an expansion
        /// of 1 will place the vertex up one pixel and to the right one pixel
        /// </remarks>
        public readonly Vector2 PixelNormal;

        public ContourVertex(Vector2 position, Vector2 pixelNormal)
        {
            Position = position;
            PixelNormal = pixelNormal;
        }

        public bool Equals(ContourVertex other)
        {
            return Position.Equals( other.Position ) && PixelNormal.Equals( other.PixelNormal );
        }

        public override bool Equals(object obj)
        {
            return obj is ContourVertex other && Equals( other );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ( Position.GetHashCode() * 397 ) ^ PixelNormal.GetHashCode();
            }
        }
    }
}