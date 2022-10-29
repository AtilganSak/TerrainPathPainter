using UnityEditor;
using UnityEngine;

namespace TerrainPathPainter
{
    public class IntersectPoint : MonoBehaviour
    {
        [HideInInspector]public int ID;
        [HideInInspector]public LayerMesh TargetLayer;
        [HideInInspector]public TerrainPathPainter BaseClass;
        [HideInInspector]public bool IsConnected = false;
        [HideInInspector]public IntersectPoint TargetIP;
        private void OnDrawGizmos()
        {
            if(BaseClass == null) return;
            if(BaseClass.m_DrawReferencePoint)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, 0.5f);
                if(TargetIP != null)
                {
                    if(BaseClass.m_IsActiveCreativeMode)
                        Gizmos.color = Color.green;
                    else
                        Gizmos.color = Color.red;
                    if(IsConnected)
                        Gizmos.DrawLine(transform.position, TargetIP.transform.position);
                }
            }
#if UNITY_EDITOR
            if(BaseClass.m_DrawPointDistance)
            {
                if(IsConnected && TargetIP != null)
                {
                    Vector3 center = transform.position + TargetIP.transform.position;
                    float distance = Vector3.Distance(transform.position, TargetIP.transform.position);
                    Handles.Label(center / 2, Mathf.FloorToInt(distance).ToString());
                }

                RaycastHit hit;
                Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity);
                Debug.DrawRay(transform.position, -transform.up * hit.distance);
                Vector3 center2 = transform.position + hit.point;
                float distance2 = Vector3.Distance(transform.position, hit.point);
                Handles.Label(center2 / 2, Mathf.FloorToInt(distance2).ToString());
            }
#endif
        }
    }
    [CustomEditor(typeof(IntersectPoint))]
    public class IntersectPointEditor : Editor
    {
        IntersectPoint script;
        Vector2 ipListScroll;
        bool showIPList;

        private void OnEnable()
        {
            script = (IntersectPoint)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            showIPList = EditorGUILayout.Foldout(showIPList, "Connects",true,EditorStyles.foldoutHeader);
            if(showIPList)
            {
                if(script.BaseClass != null)
                {
                    ipListScroll = EditorGUILayout.BeginScrollView(ipListScroll, "Box");
                    for(int i = 0; i < script.BaseClass.interPoints.Count; i++)
                    {
                        if(script.BaseClass.interPoints[i].ID == script.ID) continue;
                        if(script.TargetIP != null)
                            if(script.BaseClass.interPoints[i].ID == script.TargetIP.ID) continue;
                        if(script.BaseClass.interPoints.Count > 1)
                        {
                            if((script.ID - 1) >= 0)
                            {
                                if(script.BaseClass.interPoints[i].ID == script.BaseClass.interPoints[script.ID - 1].ID) continue;
                            }
                        }
                        EditorGUILayout.BeginHorizontal();
                        if(GUILayout.Button(script.BaseClass.interPoints[i].name, EditorStyles.miniButton))
                        {
                            script.TargetIP = script.BaseClass.interPoints[i];
                            script.IsConnected = true;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }
    }
}
