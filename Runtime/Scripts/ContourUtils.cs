using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MrGVSV.PixelContour
{
    public static class ContourUtils
    {
        /// <summary>
        /// Converts a sequence of points into vertices
        /// </summary>
        /// <remarks>
        /// This method assumes that the points are ordered sequentially in a clockwise manner
        /// </remarks>
        /// <param name="points">The points</param>
        /// <returns>The same points converted to vertex nodes</returns>
        public static List<ContourVertex> PointsToVertices(ICollection<Vector2> points)
        {
            List<ContourVertex> vertices = new List<ContourVertex>( points.Count );
            ForTriad( points, (i, prev, point, next) =>
            {
                if (next == prev)
                {
                    return;
                }

                Vector2 prevOffset = ( prev - point ).normalized;
                Vector2 nextOffset = ( next - point ).normalized;
                Vector2 prevEdgeNormal = Vector2.Perpendicular( point - prev ).normalized;
                Vector2 nextEdgeNormal = Vector2.Perpendicular( next - point ).normalized;

                Vector2 normal = ( prevEdgeNormal + nextEdgeNormal ).normalized;
                // === Handle Collinear === //
                if (Mathf.Approximately( normal.x, 0 ) && Mathf.Approximately( normal.y, 0 ))
                {
                    Vector2 diff = nextOffset - prevOffset;
                    normal = new Vector2( -diff.y, diff.x ).normalized;
                }

                float max = Mathf.Max( Mathf.Abs( normal.x ), Mathf.Abs( normal.y ) );
                normal /= max;

                vertices.Add( new ContourVertex( point, normal ) );
            } );

            return vertices;
        }

        /// <summary>
        /// Simply the given list of points
        /// </summary>
        /// <remarks>
        /// This method simply removes duplicate edge points. Corners and diagonals will not be affected
        /// </remarks>
        /// <param name="points">The points to simplify</param>
        /// <returns>The simplified points</returns>
        public static List<Vector2> Simplify(ICollection<Vector2> points)
        {
            List<Vector2> vertices = new List<Vector2>( points.Count );
            ForTriad( points, (i, prev, point, next) =>
            {
                if (!OnLineSegment( point, prev, next ))
                {
                    vertices.Add( points.ElementAt( i ) );
                }
            } );

            return vertices;
        }

        /// <summary>
        /// Expand the given sequence of vertices
        /// </summary>
        /// <param name="vertices">The vertices to expand</param>
        /// <param name="amount">The amount to expand by (accepts negative numbers as well)</param>
        /// <returns>The expanded vertices</returns>
        public static List<ContourVertex> Expand(ICollection<ContourVertex> vertices, float amount)
        {
            List<Vector2> points = new List<Vector2>( vertices.Count );
            
            foreach (ContourVertex vertex in vertices)
            {
                Vector2 normal = vertex.PixelNormal;
                Vector2 position = vertex.Position + ( amount * normal );
                points.Add( position );
            }

            return PointsToVertices( points );
        }

        /// <summary>
        /// Expand the given sequence of vertices in incremental steps (useful for pixel-sized increments)
        /// </summary>
        /// <param name="vertices">The vertices to expand</param>
        /// <param name="steps">The number of steps to expand by (accepts negative numbers as well)</param>
        /// <returns>The expanded vertices</returns>
        public static List<ContourVertex> StepExpand(IEnumerable<ContourVertex> vertices, int steps)
        {
            List<ContourVertex> expanded = new List<ContourVertex>( vertices );
            List<Vector2> points = new List<Vector2>( expanded.Count );
            var sign = (int) Mathf.Sign( steps );
            for (var step = 0; step < Mathf.Abs( steps ); step++)
            {
                points.Clear();
                for (var i = 0; i < expanded.Count; i++)
                {
                    Vector2 normal = expanded[ i ].PixelNormal;
                    Vector2 position = expanded[ i ].Position + ( sign * normal );
                    points.Add( position );
                }

                expanded = PointsToVertices( RemoveDuplicatePoints( points ) );
            }

            return expanded;
        }

        /// <summary>
        /// Removes any duplicate points in a list
        /// </summary>
        /// <param name="points">The points</param>
        /// <returns>The filtered list of points</returns>
        public static List<Vector2> RemoveDuplicatePoints(IEnumerable<Vector2> points)
        {
            List<Vector2> contour = points.Distinct().ToList();
            return contour;
        }

        /// <summary>
        /// Get the bounding box of the given set of points
        /// </summary>
        /// <param name="points">The points</param>
        /// <returns>The bounds of the points</returns>
        public static Bounds GetBounds(ICollection<Vector2> points)
        {
            Debug.Assert( points.Count > 2, "points.Count > 2" );
            Vector2 min = points.ElementAt( 0 );
            Vector2 max = points.ElementAt( 0 );
            foreach (Vector2 point in points)
            {
                if (point.x < min.x)
                {
                    min.x = point.x;
                }

                if (point.y < min.y)
                {
                    min.y = point.y;
                }

                if (point.x > max.x)
                {
                    max.x = point.x;
                }

                if (point.y > max.y)
                {
                    max.y = point.y;
                }
            }

            Vector2 size = max - min;
            return new Bounds( size / 2, size );
        }

        /// <summary>
        /// Checks if the given point lies on the line segment from <paramref name="start"/> to <paramref name="end"/>
        /// </summary>
        /// <param name="point">The point to check</param>
        /// <param name="start">The line segment start point</param>
        /// <param name="end">The line segment end point</param>
        /// <returns>True, if the point lies on the line segment</returns>
        public static bool OnLineSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 dPoint = point - start;
            Vector2 segment = end - start;

            float cross = Cross( dPoint, segment );
            if (!Mathf.Approximately( cross, 0f ))
            {
                return false;
            }

            if (Mathf.Abs( segment.x ) >= Mathf.Abs( segment.y ))
            {
                return segment.x > 0 ? start.x <= point.x && point.x <= end.x : end.x <= point.x && point.x <= start.x;
            }

            return segment.y > 0 ? start.y <= point.y && point.y <= end.y : end.y <= point.y && point.y <= start.y;
        }
        
        //    __  __       _   _     
        //   |  \/  |     | | | |    
        //   | \  / | __ _| |_| |__  
        //   | |\/| |/ _` | __| '_ \ 
        //   | |  | | (_| | |_| | | |
        //   |_|  |_|\__,_|\__|_| |_|
        //                           
        //                           

        /// <summary>
        /// Contain a value within a range such that extending the value beyond that range
        /// wraps it back within the range 
        /// </summary>
        /// <param name="value">The value to contain</param>
        /// <param name="min">The range minimum (inclusive)</param>
        /// <param name="max">The range maximum (exclusive)</param>
        /// <returns>The wrapped value</returns>
        public static int WrapInt(int value, int min = 0, int max = 1)
        {
            return ( ( value - min ) % ( max - min ) + ( max - min ) ) % ( max - min ) + min;
        }
        
        /// <summary>
        /// Returns the cross product of two 2D vectors
        /// </summary>
        /// <param name="a">Vector a</param>
        /// <param name="b">Vector b</param>
        /// <returns>The cross-product</returns>
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
        
        //    _                       _             
        //   | |                     (_)            
        //   | |     ___   ___  _ __  _ _ __   __ _ 
        //   | |    / _ \ / _ \| '_ \| | '_ \ / _` |
        //   | |___| (_) | (_) | |_) | | | | | (_| |
        //   |______\___/ \___/| .__/|_|_| |_|\__, |
        //                     | |             __/ |
        //                     |_|            |___/ 

        /// <summary>
        /// A helper method that takes in an index and returns the wrapped index
        /// (i.e. the index bound between 0 and a collection's count)
        /// </summary>
        public delegate int WrappedIndexer(int index);

        /// <summary>
        /// A for-loop helper method that makes it easier to get nearby elements
        /// </summary>
        /// <remarks>
        /// The callback receives a <see cref="WrappedIndexer"/> that can be used to retrieve another index
        /// in the collection.
        /// <example>
        /// Assume a collection of 3 objects:
        /// <code>
        /// ForWrap(collection.Count, (idx, indexer) => {
        ///     var prev = collection.ElementAt( indexer( idx - 1 ) );
        ///     var curr = collection.ElementAt( idx );
        ///     var next = collection.ElementAt( indexer( idx + 1 ) );
        /// 
        ///     // Do something...
        /// });
        /// </code>
        /// Retrieving the previous and next elements become much simpler, where the `<c>prev</c>` of the first element
        /// is the last, and the `<c>next</c>` of the last element is the first
        /// </example>
        /// </remarks>
        /// <param name="count">The number of iterations to run</param>
        /// <param name="callback">
        /// The callback to run on each iteration of the loop.
        /// Given the index of the current element and an instance of <see cref="WrappedIndexer"/>
        /// </param>
        public static void ForWrap(int count, Action<int, WrappedIndexer> callback)
        {
            var indexer = new WrappedIndexer( (idx) => WrapInt( idx, 0, count ) );
            for (var i = 0; i < count; i++)
            {
                callback.Invoke( i, indexer );
            }
        }

        /// <summary>
        /// A for-loop helper method that gives the previous, current, and next element at each iteration (wrapped)
        /// </summary>
        /// <param name="collection">The collection to iterate over</param>
        /// <param name="callback">The callback to run on each iteration of the loop.
        /// Given the index of the current element. The remaining arguments are the previous, current, and next
        /// elements, respectively
        /// </param>
        /// <typeparam name="T">The element type in the collection</typeparam>
        public static void ForTriad<T>(ICollection<T> collection, Action<int, T, T, T> callback)
        {
            ForWrap( collection.Count, (i, indexer) =>
            {
                T prev = collection.ElementAt( indexer( i - 1 ) );
                T point = collection.ElementAt( i );
                T next = collection.ElementAt( indexer( i + 1 ) );

                callback.Invoke( i, prev, point, next );
            } );
        }
        
        
        //     _____                              _                 
        //    / ____|                            (_)                
        //   | |     ___  _ ____   _____ _ __ ___ _  ___  _ __  ___ 
        //   | |    / _ \| '_ \ \ / / _ \ '__/ __| |/ _ \| '_ \/ __|
        //   | |___| (_) | | | \ V /  __/ |  \__ \ | (_) | | | \__ \
        //    \_____\___/|_| |_|\_/ \___|_|  |___/_|\___/|_| |_|___/
        //                                                          
        //                                                          

        public static Vector2Int ToVector2Int(Vector2 vector2)
        {
            return new Vector2Int( (int) vector2.x, (int) vector2.y );
        }
        
        internal static IEnumerable<Vector2Int> EdgesToVertices(IEnumerable<ContourEdge> edges)
        {
            List<ContourEdge> uniqueEdges = edges.Distinct().ToList();
            List<Vector2Int> points = new List<Vector2Int>( 2 * uniqueEdges.Count );
            ForTriad( uniqueEdges, (i, prev, curr, next) =>
            {
                points.Add( curr.Start );
            } );
            return points;
        }
    }
}