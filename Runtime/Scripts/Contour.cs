using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace MrGVSV.PixelContour
{
    public class Contour
    {
        /// <summary>
        /// The vertices in this contour
        /// </summary>
        public ReadOnlyCollection<ContourVertex> Vertices { get; }

        /// <summary>
        /// The number of vertices in this contour
        /// </summary>
        public int VertexCount => Vertices.Count;

        /// <summary>
        /// Helper property for retrieving the position data from each vertex in this contour
        /// </summary>
        public IEnumerable<Vector2> Points => Vertices.Select( v => v.Position );

        /// <summary>
        /// The bounds of this contour
        /// </summary>
        public readonly Bounds Bounds;

        /// <summary>
        /// Create a new <see cref="Contour"/> instance
        /// </summary>
        /// <param name="vertices">The vertices in clockwise order</param>
        public Contour(IEnumerable<ContourVertex> vertices)
        {
            Vertices = Array.AsReadOnly( vertices.ToArray() );
            Bounds = ContourUtils.GetBounds( Points.ToList() );
        }

        /// <summary>
        /// Create a new <see cref="Contour"/> instance
        /// </summary>
        /// <param name="points">The vertices in clockwise order</param>
        public Contour(ICollection<Vector2> points)
            : this(
                ContourUtils.PointsToVertices( points )
            )
        {
        }

        /// <summary>
        /// Return an expanded (or shrunk) version of this contour
        /// </summary>
        /// <remarks>
        /// Expanding/Shrinking can cause edges to overlap if they are too close together
        /// </remarks>
        /// <param name="amount">The amount to expand by (or shrink if negative)</param>
        /// <returns>The expanded/shrunk contour</returns>
        public Contour Expanded(float amount)
        {
            return new Contour( ContourUtils.Expand( Vertices.ToList(), amount ) );
        }

        /// <summary>
        /// Return an expanded (or shrunk) version of this contour, using whole-pixel steps
        /// </summary>
        /// <remarks>
        /// Stepping is sometimes better because it allows nearby vertices to collapse into a single vertex,
        /// preventing some cases of overlapping edges
        /// 
        /// Expanding/Shrinking can cause edges to overlap if they are too close together
        /// </remarks>
        /// <param name="steps">The number of steps to perform</param>
        /// <returns>The expanded/shrunk contour</returns>
        public Contour StepExpanded(int steps)
        {
            return new Contour( ContourUtils.StepExpand( Vertices.ToList(), steps ) );
        }

        /// <summary>
        /// Return a simplified version of this contour
        /// </summary>
        /// <remarks>
        /// This only affects collinear vertices. Corners and diagonals will not be simplified.
        /// </remarks>
        /// <returns>The simplified contour</returns>
        public Contour Simplified()
        {
            return new Contour( ContourUtils.Simplify( Points.ToList() ) );
        }
    }
}