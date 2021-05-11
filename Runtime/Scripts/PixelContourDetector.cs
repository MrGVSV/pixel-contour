using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MrGVSV.PixelContour
{
    /// <summary>
    /// Class used to detect the contour of pixel-art
    /// </summary>
    /// <remarks>
    /// It is not recommended to use this class with:
    /// <list type="bullet">
    /// <item>
    /// <description>Large textures</description>
    /// </item>
    /// <item>
    /// <description>Textures with jagged edges</description>
    /// </item>
    /// <item>
    /// <description>Textures with multiple islands</description>
    /// </item>
    /// </list>
    ///
    /// Note that this class will work on textures with "holes" but the holes themselves will not be
    /// individually contoured
    /// </remarks>
    public sealed class PixelContourDetector
    {
        /// <summary>
        /// The threshold for which alpha values less-than-or-equal-to are deemed "transparent"
        /// </summary>
        public float AlphaThresh { get; set; } = Constants.ALPHA_THRESH;

        private Contour? m_Contour;
        private readonly Vector2Int m_Size;
        private readonly string m_DebugName;

        /// <summary>
        /// The pixels in the texture
        /// </summary>
        /// <remarks>
        /// REMEMBER: Texture2D coords go from bottom-left to top-right!
        /// </remarks>
        private readonly Color[ , ] m_Pixels;

        /// <summary>
        /// Detect the contour of a sprite
        /// </summary>
        /// <param name="sprite">The sprite to contour</param>
        public PixelContourDetector(Sprite sprite)
            : this( sprite.texture, ContourUtils.ToVector2Int( sprite.textureRect.size ),
                ContourUtils.ToVector2Int( sprite.textureRect.position ) )
        {
            m_DebugName = sprite.name;
        }

        /// <summary>
        /// Detect the contour of a texture
        /// </summary>
        /// <param name="texture2D">The texture to contour</param>
        public PixelContourDetector(Texture2D texture2D)
            : this( texture2D, new Vector2Int( texture2D.width, texture2D.height ), Vector2Int.zero )
        {
        }

        /// <summary>
        /// Detect the contour of a texture
        /// </summary>
        /// <param name="texture2D">The texture to contour</param>
        /// <param name="size">The sub-texture size</param>
        public PixelContourDetector(Texture2D texture2D, Vector2Int size)
            : this( texture2D, size, Vector2Int.zero )
        {
        }

        /// <summary>
        /// Detect the contour of a texture
        /// </summary>
        /// <param name="texture2D">The texture to contour</param>
        /// <param name="size">The sub-texture size</param>
        /// <param name="pos">The sub-texture position</param>
        public PixelContourDetector(Texture2D texture2D, Vector2Int size, Vector2Int pos)
        {
            m_Size = size;
            m_DebugName = texture2D.name;

            m_Pixels = new Color[ m_Size.x, m_Size.y ];
            Color[] pixels = texture2D.GetPixels( pos.x, pos.y, size.x, size.y );
            for (var i = 0; i < pixels.Length; i++)
            {
                var coord = new Vector2Int(
                    i % m_Size.x,
                    i / m_Size.x
                );
                m_Pixels[ coord.x, coord.y ] = pixels[ i ];
            }
        }

        //    _____       _     _ _      
        //   |  __ \     | |   | (_)     
        //   | |__) |   _| |__ | |_  ___ 
        //   |  ___/ | | | '_ \| | |/ __|
        //   | |   | |_| | |_) | | | (__ 
        //   |_|    \__,_|_.__/|_|_|\___|
        //                               
        //                               

        /// <summary>
        /// Process the given sprite and find the contour
        /// </summary>
        /// <remarks>
        /// This method is distinct from the <see cref="GetContour"/> method so that finding a contour can be performed
        /// separately from when it is needed. This can be useful for optimizing data.
        /// </remarks>
        /// <param name="autoSimplify">If true, the contour will be automatically simplified</param>
        public void FindContour(bool autoSimplify = true)
        {
            List<ContourEdge> edges = new List<ContourEdge>();

            Vector2Int start = FindStartPixel();
            Vector2Int entry = start + Constants.StartOffset;

            if (TryGetEdge( start, Constants.StartOffset, out ContourEdge startEdge ))
            {
                edges.Add( startEdge );
            }

            edges.AddRange( FindNext( start, entry, start ) );
            
            List<Vector2> points = ContourUtils.EdgesToVertices( edges )
                .Select( pt => (Vector2) pt )
                .ToList();
            m_Contour = autoSimplify ? new Contour( points ).Simplified() : new Contour( points );
        }

        /// <summary>
        /// Get the contour
        /// </summary>
        /// <remarks>
        /// Will call <see cref="FindContour"/> if the contour is null
        /// </remarks>
        /// <returns>The contour shape</returns>
        public Contour GetContour()
        {
            if (m_Contour == null)
            {
                FindContour();
            }

            Debug.Assert( m_Contour != null, nameof( m_Contour ) + " != null" );
            return (Contour) m_Contour;
        }


        //    _____      _            _       
        //   |  __ \    (_)          | |      
        //   | |__) | __ ___   ____ _| |_ ___ 
        //   |  ___/ '__| \ \ / / _` | __/ _ \
        //   | |   | |  | |\ V / (_| | ||  __/
        //   |_|   |_|  |_| \_/ \__,_|\__\___|
        //                                    
        //                                    

        /// <summary>
        /// Recursively find the next edge pixel until back at the start
        /// </summary>
        /// <remarks>
        /// Uses the Moore-Neighborhood algorithm
        /// (See: http://www.imageprocessingplace.com/downloads_V3/root_downloads/tutorials/contour_tracing_Abeer_George_Ghuneim/moore.html)
        ///
        /// ---
        /// 
        /// The <paramref name="entry"/> argument is simply the last pixel checked. This should always be
        /// a transparent pixel's coordinates
        /// </remarks>
        /// <param name="curr">The current pixel coordinates</param>
        /// <param name="entry">The entry pixel coordinates</param>
        /// <param name="start">The start pixel coordinates</param>
        /// <returns>A list of pixel edges found, in clockwise order</returns>
        private IEnumerable<ContourEdge> FindNext(Vector2Int curr, Vector2Int entry, Vector2Int start)
        {
            List<ContourEdge> found = new List<ContourEdge>( 4 );

            // The offset from the current non-transparent pixel
            Vector2Int offset = GetNextOffset( entry - curr );
            // The current entry alpha pixel
            Vector2Int currEntry = entry;
            // The next pixel to check (looking for non-transparent)
            Vector2Int next = curr + offset;
            for (var i = 0; i < Constants.OrderedOffsets.Length; i++)
            {
                if (!IsAlpha( next ))
                {
                    // Found next
                    break;
                }
                
                if (TryGetEdge( curr, offset, out ContourEdge edge ))
                {
                    found.Add( edge );
                }

                // Update coordinates
                currEntry = next;
                offset = GetNextOffset( offset );
                next = curr + offset;
            }
            
            // Add next edge
            if (TryGetEdge( next, currEntry - next, out ContourEdge nextEdge ))
            {
                found.Add( nextEdge );
            }

            // Check for end condition
            if (next == start && currEntry == start + Constants.StartOffset)
            {
                // Back to start AND from starting direction
                return found;
            }

            found.AddRange( FindNext( next, currEntry, start ) );
            return found;
        }

        private Vector2Int FindStartPixel()
        {
            for (var x = 0; x < m_Size.x; x++)
            {
                for (var y = 0; y < m_Size.y; y++)
                {
                    var coord = new Vector2Int( x, y );
                    if (!IsAlpha( coord ))
                    {
                        return coord;
                    }
                }
            }

            throw new InvalidOperationException( $"No non-transparent pixels found in texture {m_DebugName}" );
        }

        private bool IsAlpha(Vector2Int pos)
        {
            if (!Within( pos, m_Size ))
            {
                return true;
            }

            return m_Pixels[ pos.x, pos.y ].a <= AlphaThresh;
        }

        /// <summary>
        /// Get the edge of a pixel given its position and offset
        /// </summary>
        /// <remarks>
        /// An edge can only be found for adjacent offsets. A diagonal offset will result in this method
        /// returning false
        /// </remarks>
        /// <param name="curr">The current pixel coordinate</param>
        /// <param name="offset">The offset</param>
        /// <param name="edge">The edge, if found</param>
        /// <returns>Tru if an edge was found</returns>
        private static bool TryGetEdge(Vector2Int curr, Vector2Int offset, out ContourEdge edge)
        {
            if (offset == Constants.West)
            {
                edge = new ContourEdge (
                    curr + Vector2Int.zero,
                    curr + Constants.North
                );
                return true;
            }

            if (offset == Constants.North)
            {
                edge = new ContourEdge (
                    curr + Constants.North,
                    curr + Vector2Int.one
                );
                return true;
            }

            if (offset == Constants.East)
            {
                edge = new ContourEdge (
                    curr + Vector2Int.one,
                    curr + Constants.East
                );
                return true;
            }

            if (offset == Constants.South)
            {
                edge = new ContourEdge (
                    curr + Constants.East,
                    curr + Vector2Int.zero
                );
                return true;
            }

            edge = new ContourEdge();
            return false;
        }

        /// <summary>
        /// Get the next offset to check according to <see cref="Constants.OrderedOffsets"/>
        /// </summary>
        /// <param name="curr">The current offset</param>
        /// <returns>The next offset to check</returns>
        private static Vector2Int GetNextOffset(Vector2Int curr)
        {
            int idx = Array.IndexOf( Constants.OrderedOffsets, curr );
            return Constants.OrderedOffsets[ ( idx + 1 ) % Constants.OrderedOffsets.Length ];
        }

        /// <summary>
        /// Check if a point lies within the given bounds (assumes an origin of (0,0))
        /// </summary>
        /// <param name="pos">The position to check</param>
        /// <param name="size">The size of the bounding box</param>
        /// <param name="inclusiveMax">Set to true to allow for points to lie on the maximum edges</param>
        /// <returns>True, if the point is within the bounds</returns>
        private static bool Within(Vector2 pos, Vector2 size, bool inclusiveMax = false)
        {
            bool minSuccess = pos.x >= 0 && pos.y >= 0;
            bool maxSuccess = inclusiveMax ? pos.x <= size.x && pos.y <= size.y : pos.x < size.x && pos.y < size.y;
            return minSuccess && maxSuccess;
        }

        private static class Constants
        {
            public const float ALPHA_THRESH = 0.1f;
            public static readonly Vector2Int StartOffset = new Vector2Int( 0, -1 );

            public static readonly Vector2Int North = new Vector2Int( 0, 1 );
            public static readonly Vector2Int East = new Vector2Int( 1, 0 );
            public static readonly Vector2Int South = new Vector2Int( 0, -1 );
            public static readonly Vector2Int West = new Vector2Int( -1, 0 );

            public static readonly Vector2Int[] OrderedOffsets =
            {
                new Vector2Int( -1, 1 ),
                North,
                new Vector2Int( 1, 1 ),
                East,
                new Vector2Int( 1, -1 ),
                South,
                new Vector2Int( -1, -1 ),
                West,
            };
        }
    }
}