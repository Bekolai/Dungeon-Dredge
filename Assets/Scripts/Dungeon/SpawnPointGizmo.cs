using UnityEngine;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Visual helper for spawn points in the scene view.
    /// Shows colored gizmos to identify different spawn point types.
    /// </summary>
    public class SpawnPointGizmo : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        public Color gizmoColor = Color.yellow;
        public float gizmoSize = 0.5f;
        public bool showLabel = true;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            
            // Draw sphere
            Gizmos.DrawWireSphere(transform.position, gizmoSize);
            
            // Draw directional arrow
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * gizmoSize * 2);
            
            // Draw cross on ground
            Vector3 pos = transform.position;
            Gizmos.DrawLine(pos + Vector3.left * gizmoSize, pos + Vector3.right * gizmoSize);
            Gizmos.DrawLine(pos + Vector3.forward * gizmoSize, pos + Vector3.back * gizmoSize);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            
            // Draw solid sphere when selected
            Gizmos.DrawSphere(transform.position, gizmoSize * 0.8f);
            
            // Draw larger range indicator
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, gizmoSize * 2);
        }
#endif
    }
}
