using UnityEngine;

namespace MrGVSV.PixelContour
{
    /// <summary>
    /// A helper class for displaying vertices of a Sprite
    /// </summary>
    [ExecuteAlways, RequireComponent( typeof( SpriteRenderer ) )]
    public class SpriteContourVisualizer : MonoBehaviour
    {
        [Tooltip( "Use this to expand or shrink the contour. This may not work well for all Sprites." )]
        [SerializeField, Range( -10f, 10f )]
        private float m_Expansion;

        [Tooltip( "Set this to match the Pixels Per Unit of the Sprite" )] [SerializeField, Min( 1 )]
        private int m_PixelsPerUnit = 32;

        private Sprite m_Sprite;
        private PixelContourDetector m_Detector;
        private Contour m_Contour;

        private void OnEnable()
        {
            m_Sprite = GetComponent<SpriteRenderer>().sprite;

            m_Detector = new PixelContourDetector( m_Sprite );
            m_Detector.FindContour();
            m_Contour = m_Detector.GetContour();
        }

        private void OnValidate()
        {
            m_Contour = m_Detector?.GetContour().Expanded( m_Expansion );
        }

        private void OnDrawGizmos()
        {
            if (m_Contour == null || m_Contour?.VertexCount <= 2)
            {
                return;
            }

            Vector2 pos = transform.position;
            ContourUtils.ForTriad( m_Contour.Vertices, (i, prev, curr, next) =>
            {
                Vector2 a = (Vector2) curr.Position / m_PixelsPerUnit;
                Vector2 b = (Vector2) next.Position / m_PixelsPerUnit;

                a += pos;
                b += pos;

                Gizmos.color = Color.green;
                Gizmos.DrawLine( a, b );
                Gizmos.color = Color.red;
                Gizmos.DrawSphere( a, Constants.GizmoCircleRadius / m_PixelsPerUnit );
                Gizmos.color = Color.blue;
                Gizmos.DrawRay( a, curr.PixelNormal / m_PixelsPerUnit );
                Gizmos.DrawRay( b, next.PixelNormal / m_PixelsPerUnit );
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere( b, Constants.GizmoCircleRadius / m_PixelsPerUnit );
            } );
        }

        private static class Constants
        {
            public const float GizmoCircleRadius = 0.15f;
        }
    }
}