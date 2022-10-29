///-----------------------------------------------------------------
///   Name:           TerrainPathPainter
///   Description:    This asset use can paint the terrain as path method.
///   Author:         ATILGAN SAK
///   Contact:        sak.atilgan@gmail.com
///   Date:           11.25.2019 - Monday
///   Note:           There is a important point in this asset. If there are small dots when you press the PAINT button, the Texture Resolution of your terrain is larger than its size. 
///   In this case, you can change the paint frequency from the "General Settings" section or change your terrain settings.
///-----------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TerrainPathPainter
{
    [DisallowMultipleComponent]
    public class TerrainPathPainter : MonoBehaviour
    {
        public int m_TargetLayer;
        public int m_Offset = 1;
        public int m_FlatLayer;
        public float m_Alpha = 1;
        public float m_IP_Frequency = 1;
        public float m_OP_Frequency = 1;
        public float m_NewIPPosOffset = 1;
        public List<IntersectPoint> interPoints = new List<IntersectPoint>();
        public List<Vector3> offsetPoints = new List<Vector3>();
        public bool m_TerrainIsNull = true;
        public bool m_DrawOffsetPoint = false;
        public bool m_DrawReferencePoint = true;
        public bool m_DrawPointDistance = false;
        public bool m_FocusObjectCreated;
        public bool m_SetParent;
        public bool m_CleanScene;
        public bool m_UseGeneralSettings = true;
        public bool m_IsActiveCreativeMode = false;
        public bool m_ChangedTerrain;
        public TPP_Data dataBase { get => db; set { db = value; } }
        TPP_Data db;
        public TerrainData terrainData;
        public Terrain terrain;
        float[,,] map;
        List<float[,,]> alphamapHistory = new List<float[,,]>();
        int m_CurrentHistoryPoint;
        int m_HistoryStoreLimit = 20;

        void OnValidate()
        {
            GetTerrain();
        }
        void OnDrawGizmos()
        {
            Lebug.Log("History Member Count: ",alphamapHistory.Count,"TPP");
            Lebug.Log("Current History Point: ", m_CurrentHistoryPoint, "TPP");
            if(m_DrawOffsetPoint)
            {
                if(offsetPoints != null)
                {
                    Gizmos.color = Color.green;
                    for(int i = 0; i < offsetPoints.Count; i++)
                    {
                        Gizmos.DrawSphere(offsetPoints[i], 0.25f);
                    }
                }
            }
        }
        void SyncIntersectPoints()
        {
            if(interPoints != null)
            {
                for(int i = 0; i < interPoints.Count; i++)
                {
                    interPoints[i].ID = i;
                    //interPoints[i].name = Regex.Replace(interPoints[i].name, "[^a-zA-Z]", "") + "-" + i;
                }
                interPoints = interPoints.OrderBy(x => x.ID).ToList();
            }
        }
        void LayerMapProcess(bool _createOP = false)
        {
            map = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            Transform tmpObject = new GameObject("TmpObject").transform;
            if(interPoints[0].gameObject.activeInHierarchy)
                tmpObject.SetPositionAndRotation(interPoints[0].transform.position, interPoints[0].transform.rotation);
            else
                return;
            int border = terrainData.alphamapResolution - 1;
            for(int r = 0; r < interPoints.Count; r++)
            {
                if(!interPoints[r].gameObject.activeInHierarchy || !interPoints[r].IsConnected)
                {
                    continue;
                }
                tmpObject.transform.position = interPoints[r].transform.position;
                Vector3 cord1 = Vector3.zero;
                Vector3 cord2 = Vector3.zero;
                cord1 = interPoints[r].transform.position;
                IntersectPoint nIp = interPoints[r].TargetIP;
                if(nIp != null)
                {
                    cord2 = nIp.transform.position;
                }
                else
                    break;
                tmpObject.transform.LookAt(cord2, Vector3.up);
                while(tmpObject.transform.position != cord2)
                {
                    Vector3 _leftMax = (tmpObject.transform.right * -m_Offset) + tmpObject.transform.position;
                    Vector3 _rightMax = (tmpObject.transform.right * m_Offset) + tmpObject.transform.position;
                    Vector3 _currentPoint = _leftMax;
                    while(_currentPoint != _rightMax)
                    {
                        _currentPoint = Vector3.MoveTowards(_currentPoint, _rightMax, m_OP_Frequency);
                        Vector2Int _mapPos = TerrainHelper.WorldPositionToTerrainMap(_currentPoint, terrain);
                        if(_mapPos.y > border)
                            _mapPos.y = border;
                        else if(_mapPos.y < 0)
                            _mapPos.y = 0;
                        if(_mapPos.x > border)
                            _mapPos.x = border;
                        else if(_mapPos.x < 0)
                            _mapPos.x = 0;
                        if(_createOP)
                            offsetPoints.Add(_currentPoint);
                        for(int l = 0; l < terrainData.alphamapLayers; l++)
                        {
                            map[_mapPos.y, _mapPos.x, l] = 0;
                        }
                        if(m_UseGeneralSettings)
                        {
                            map[_mapPos.y, _mapPos.x, m_TargetLayer] = m_Alpha;
                        }
                        else
                        {
                            map[_mapPos.y, _mapPos.x, interPoints[r].TargetLayer.m_Id] = interPoints[r].TargetLayer.m_Alpha;
                        }
                    }
                    tmpObject.transform.position = Vector3.MoveTowards(tmpObject.transform.position, cord2, m_IP_Frequency);
                }
                terrainData.SetAlphamaps(0, 0, map);
                m_ChangedTerrain = true;
                SubscribeHistory(map);
            }
            DestroyImmediate(tmpObject.gameObject);
        }
        [ContextMenu("Create RSP File")]
        public void CreateRSP_File()
        {
            //First try read.
            if(File.Exists("Assets\\csc.rsp"))
            {
                string[] lines = File.ReadAllLines("Assets\\csc.rsp");
                if(!lines.Contains("-define:TERRAIN_PATH_PAINTER"))
                {
                    StreamWriter writer = File.AppendText("Assets\\csc.rsp");
                    writer.WriteLine("-define:TERRAIN_PATH_PAINTER");
                    writer.Close();
                }
            }
            else
            {
                FileStream body = new FileStream("Assets/csc.rsp", FileMode.Create);
                StreamWriter w = new StreamWriter(body, Encoding.UTF8);
                w.Write("-define:TERRAIN_PATH_PAINTER");
                w.Close();
                body.Close();
            }
            AssetDatabase.Refresh();
        }
        /// <summary>
        /// Paint the terrain during the intersect points way.
        /// </summary>
        public void PaintMap()
        {
            if(interPoints == null) return;
            if(interPoints.Count == 0) return;

            if(alphamapHistory.Count == 0)
                SubscribeHistory(terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight));
            if(dataBase != null)
            {
                //Set previous alphamap to terrain.
                if(dataBase.PreviousMap == null)
                {
                    dataBase.PreviousMap = terrainData.GetAlphamaps(0,0,terrainData.alphamapWidth, terrainData.alphamapHeight);
                }
                else
                {
                    terrainData.SetAlphamaps(0, 0, dataBase.PreviousMap);
                    dataBase.PreviousMap = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
                }
            }
            LayerMapProcess(_createOP: true);
        }
        /// <summary>
        /// Set default alphamap layer of current alphamap.
        /// </summary>
        public void ApplyMap()
        {
            dataBase.PreviousMap = null;
            alphamapHistory.Clear();
            m_CurrentHistoryPoint = 0;
        }
        /// <summary>
        /// Revert terrain alphamap layer to state of first state.
        /// </summary>
        public void RevertMap()
        {
            terrainData.SetAlphamaps(0, 0, dataBase.PreviousMap);
        }
        /// <summary>
        /// Revoke the alphamap changes.
        /// </summary>
        public void UndoAlphamap()
        {
            if(alphamapHistory.Count != 0)
            {
                if((m_CurrentHistoryPoint - 1) >= 0)
                {
                    m_CurrentHistoryPoint--;
                    terrainData.SetAlphamaps(0, 0, alphamapHistory[m_CurrentHistoryPoint]);
                }
            }
        }
        /// <summary>
        /// Get forward the alphamp changes.
        /// </summary>
        public void ReDoAlphamap()
        {
            if(alphamapHistory.Count != 0)
            {
                if((m_CurrentHistoryPoint + 1) <= alphamapHistory.Count - 1)
                {
                    m_CurrentHistoryPoint++;
                    terrainData.SetAlphamaps(0, 0, alphamapHistory[m_CurrentHistoryPoint]);
                }
            }
        }
        /// <summary>
        /// Add new event to History.
        /// </summary>
        /// <param name="_obj"></param>
        public void SubscribeHistory(float[,,] _obj)
        {
            if(_obj != null)
            {
                if(alphamapHistory.Count != m_HistoryStoreLimit)
                {
                    if(!alphamapHistory.Contains(_obj))
                    {
                        if(m_CurrentHistoryPoint != alphamapHistory.Count - 1)
                        {
                            for(int i = m_CurrentHistoryPoint + 1; i <= alphamapHistory.Count - 1; i++)
                            {
                                alphamapHistory.RemoveAt(i);
                            }
                        }
                        alphamapHistory.Add(_obj);
                        m_CurrentHistoryPoint = alphamapHistory.Count - 1;
                    }
                }
            }
        }
        /// <summary>
        /// Remove Intersect Point
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_onLayer">Is delete the layer?</param>
        /// <param name="_onScene">Is delete IP object on the scene?</param>
        public void RemoveIP(int _id,bool _onLayer = false, bool _onScene = false)
        {
            int _listCount = interPoints.Count;
            if(_onLayer)
            {
                if(dataBase.PreviousMap != null)
                {
                    int startID = 0;
                    int finishID = 0;
                    if(interPoints.Count > 1)
                    {
                        if(interPoints[_id - 1].TargetIP != null)
                        {
                            startID = _id - 1;
                        }
                        else
                        {
                            startID = _id;
                        }
                    }
                    if(interPoints[_id].TargetIP != null)
                    {
                        finishID = _id + 1;
                    }
                    else
                    {
                        finishID = _id;
                    }
                    for(int s = startID; s < finishID; s++)
                    {
                        Transform tmpObject = new GameObject("TmpObject").transform;
                        tmpObject.transform.position = interPoints[s].transform.position;
                        Vector3 cord1 = Vector3.zero;
                        Vector3 cord2 = Vector3.zero;
                        cord1 = interPoints[s].transform.position;
                        cord2 = interPoints[s].TargetIP.transform.position;
                        map = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
                        tmpObject.transform.LookAt(cord2, Vector3.up);
                        while(tmpObject.transform.position != cord2)
                        {
                            Vector3 _leftMax = (tmpObject.transform.right * -m_Offset) + tmpObject.transform.position;
                            Vector3 _rightMax = (tmpObject.transform.right * m_Offset) + tmpObject.transform.position;
                            Vector3 _currentPoint = _leftMax;
                            while(_currentPoint != _rightMax)
                            {
                                _currentPoint = Vector3.MoveTowards(_currentPoint, _rightMax, m_OP_Frequency);
                                Vector2Int _mapPos = TerrainHelper.WorldPositionToTerrainMap(_currentPoint, terrain);
                                for(int l = 0; l < terrainData.alphamapLayers; l++)
                                {
                                    map[_mapPos.y, _mapPos.x, l] = 0;
                                    if(dataBase.PreviousMap.Length == map.Length)
                                    {
                                        map[_mapPos.y, _mapPos.x, l] = dataBase.PreviousMap[_mapPos.y, _mapPos.x, l];
                                    }
                                }
                            }
                            tmpObject.transform.position = Vector3.MoveTowards(tmpObject.transform.position, cord2, m_IP_Frequency);
                        }
                        terrainData.SetAlphamaps(0, 0, map);
                        m_ChangedTerrain = true;
                        DestroyImmediate(tmpObject.gameObject);
                    }
                }
            }
            if(_onScene)
            {
                GameObject go = interPoints[_id].gameObject;
                if(interPoints.Count > 1)
                {
                    interPoints[_id - 1].TargetIP = null;
                    interPoints[_id - 1].IsConnected = false;
                }
                interPoints.RemoveAt(_id);
                DestroyImmediate(go);
            }
            else
            {
                interPoints.RemoveAt(_id);
            }
            if(interPoints.Count != _listCount)
                SyncIntersectPoints();
        }
        /// <summary>
        /// Create Intersect Point
        /// </summary>
        /// <returns></returns>
        public IntersectPoint CreateIP(Vector3? newPos = null)
        {
            GameObject newIPObject = new GameObject("IntersectPoint-" + interPoints.Count);
            Undo.RegisterCreatedObjectUndo(newIPObject, "Created go");
            if(m_SetParent)
                newIPObject.transform.SetParent(transform);
            IntersectPoint ip = newIPObject.AddComponent<IntersectPoint>();
            if(newPos == null)
            {
                if(interPoints.Count > 0)
                    ip.transform.position = interPoints[interPoints.Count - 1].transform.position;
                else
                {
#if UNITY_EDITOR
                    //Set IP position according to SceneView center point/
                    Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
                    RaycastHit hitInfo;
                    if(Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
                    {
                        Vector3 hitPoint = hitInfo.point;
                        hitPoint.y += m_NewIPPosOffset;
                        ip.transform.position = hitPoint;
                    }
#endif
                }
            }
            else
            {
                ip.transform.position = (Vector3)newPos;
            }
            ip.ID = interPoints.Count;
            ip.BaseClass = this;
            if(interPoints.Count > 0)
            {
                interPoints[interPoints.Count - 1].TargetIP = ip;
                interPoints[interPoints.Count - 1].IsConnected = true;
            }
            interPoints.Add(ip);
            return ip;
        }
        /// <summary>
        /// Connect of selected ip to first ip.
        /// </summary>
        public void ConnectToFirstIP(IntersectPoint _selectedIP)
        {
            if(_selectedIP != null)
            {
                _selectedIP.IsConnected = true;
                _selectedIP.TargetIP = interPoints[0];
            }
        }
        /// <summary>
        /// Set terrain layer to 0.
        /// </summary>
        public void ResetTerrainLayer()
        {
            float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
            for(int y = 0; y < terrainData.alphamapHeight; y++)
            {
                for(int x = 0; x < terrainData.alphamapWidth; x++)
                {
                    for(int i = 0; i < terrainData.alphamapLayers; i++)
                    {
                        //Set alpha 0 other layers.
                        if(i != m_TargetLayer)
                        {
                            map[x, y, i] = 0;
                        }
                    }
                    map[x, y, m_FlatLayer] = 1;
                }
            }
            terrainData.SetAlphamaps(0, 0, map);
            m_ChangedTerrain = true;
            alphamapHistory.Clear();
            m_CurrentHistoryPoint = 0;
            dataBase.PreviousMap = null;
        }
        /// <summary>
        /// Get Terrain component in the object.
        /// </summary>
        public void GetTerrain()
        {
            if(m_TerrainIsNull)
            {
                terrain = GetComponent<Terrain>();
                if(terrain != null)
                {
                    m_TerrainIsNull = false;
                    terrainData = terrain.terrainData;
                }
                else
                {
                    m_TerrainIsNull = true;
                }
            }
        }
        /// <summary>
        /// Remove offset point in the scene and list.
        /// </summary>
        public void RemoveOffsetPoints()
        {
            if(offsetPoints != null)
                if(offsetPoints.Count > 0)
                    offsetPoints.Clear();
        }
        /// <summary>
        /// Enable or disable offset point visibility
        /// </summary>
        public void ShowOrHideOffsetPoint() => m_DrawOffsetPoint = !m_DrawOffsetPoint;
        /// <summary>
        /// Enable or disable reference point visibility
        /// </summary>
        public void ShowOrHideReferencePoint() => m_DrawReferencePoint = !m_DrawReferencePoint;
        /// <summary>
        /// Enable or disable point distance label visibility.
        /// </summary>
        public void ShowOrHidePointDistances() => m_DrawPointDistance = !m_DrawPointDistance;
        /// <summary>
        /// Enable or disable Creative Mode.
        /// </summary>
        public void OpenOrCloseCreativeMode() => m_IsActiveCreativeMode = !m_IsActiveCreativeMode;
    }
    [System.Serializable]
    public class TPP_Data
    {
        [SerializeField]
        public float ID = 111;
        [SerializeField]
        public float[,,] PreviousMap;
    }
    [System.Serializable]
    public struct LayerMesh
    {
        public int m_Id;
        public string m_Name;
        public float m_Alpha;
        public Texture2D m_DiffuseTexture;
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(TerrainPathPainter))]
    public class TerrainPathPainterEditor : Editor
    {
        TerrainPathPainter script;
        Vector2 ipHandlerScroll;

        private void OnEnable()
        {
            script = (TerrainPathPainter)target;
            EditorApplication.hierarchyChanged += UpdatedHierarchy;
            ControlDatabase();
        }
        private void OnDisable()
        {
            SaveChanges();
        }
        private void OnSceneGUI()
        {
            if(script.m_IsActiveCreativeMode)
            {
                if(Event.current.shift)
                {
                    if(Event.current.type == EventType.MouseDown &&
                    Event.current.clickCount == 1 &&
                    Event.current.button == 0)
                    {
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        RaycastHit hit;
                        if(Physics.Raycast(ray, out hit))
                        {
                            Vector3 hitPoint = hit.point;
                            hitPoint.y += script.m_NewIPPosOffset;
                            script.CreateIP(hitPoint);
                        }
                        Event.current.Use();
                    }
                }
            }
        }
        void UpdatedHierarchy()
        {
            if(script != null)
                script.GetTerrain();
            script.interPoints.RemoveAll(x => x == null);
        }
        public override void OnInspectorGUI()
        {
            Lebug.Log("Terrain Is Null: ", script.m_TerrainIsNull);
            if(script.m_TerrainIsNull)
            {
                script.terrain = (Terrain)EditorGUILayout.ObjectField(new GUIContent("Target Terrain: "), script.terrain, typeof(Terrain),true);
                if(script.terrain != null)
                {
                    script.terrainData = script.terrain.terrainData;
                    script.m_TerrainIsNull = false;
                }
                else
                    script.m_TerrainIsNull = true;
                EditorGUILayout.HelpBox("Attach or AddComponent the Terrain!", MessageType.Error);
            }
            else
            {
                EditorGUILayout.BeginVertical("Box",GUILayout.Width(457));//VER7

                #region HEADER
                GUI.color = new Color(1, 0.7861286f, 0.1367925f,1);
                GUI.DrawTexture(new Rect(15.04f, 4.7f, 464.3f, 39.7f), new Texture2D(330,35));
                GUILayout.BeginArea(new Rect(75, 8, 400, 35));
                GUIStyle headerStyle = new GUIStyle();
                headerStyle.fontSize = 25;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.normal.textColor = new Color(0.6037736f, 0.1063234f,0,1);
                EditorGUILayout.LabelField("TERRAIN PATH PAINTER",headerStyle);
                GUILayout.EndArea();
                GUI.color = Color.white;
                #endregion

                GUILayout.Space(40);

                #region GENERAL SETTINGS HANDLER
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Dispose Terrain"))
                {
                    script.terrain = null;
                    script.m_TerrainIsNull = true;
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Box("", GUILayout.Height(0.3f), GUILayout.Width(457));
                script.m_TargetLayer = EditorGUILayout.IntField("Target Layer", script.m_TargetLayer, GUILayout.Width(456));
                script.m_Alpha = EditorGUILayout.FloatField("Layer Alpha", script.m_Alpha, GUILayout.Width(456));
                script.m_Offset = EditorGUILayout.IntField("Offset",script.m_Offset, GUILayout.Width(456));
                script.m_IP_Frequency = EditorGUILayout.FloatField("Inter Points Freq", Mathf.Clamp(script.m_IP_Frequency,0.03f,10f), GUILayout.Width(456));
                script.m_OP_Frequency = EditorGUILayout.FloatField("Offset Points Freq", Mathf.Clamp(script.m_OP_Frequency, 0.03f, 5f), GUILayout.Width(456));
                script.m_UseGeneralSettings = EditorGUILayout.Toggle("Use Genereal Settings", script.m_UseGeneralSettings, GUILayout.Width(250));
                if(EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(script);
                }
                #endregion

                GUILayout.Space(10);

                #region INTERSECT POINTS HANDLER
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();//HOR5
                EditorGUILayout.LabelField("Intersect Points Handler", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();//HOR5
                GUILayout.Box("", GUILayout.Height(0.3f), GUILayout.Width(458));
                if(script.interPoints != null)
                {
                    EditorGUILayout.BeginVertical("Box");//VER1
                    if(script.interPoints.Count== 0)
                    {
                        EditorGUILayout.LabelField("Not Found!", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        ipHandlerScroll = EditorGUILayout.BeginScrollView(ipHandlerScroll,GUILayout.Height(150));//SV1
                        for(int i = 0; i < script.interPoints.Count; i++)
                        {
                            if(script.interPoints[i] != null)
                            {
                                EditorGUILayout.BeginHorizontal();//HOR1
                                if(GUILayout.Button(script.interPoints[i].name, GUILayout.Width(130)))
                                {
                                    EditorGUIUtility.PingObject(script.interPoints[i].gameObject);
                                    Selection.activeGameObject = script.interPoints[i].gameObject;
                                }
                                if(!script.m_UseGeneralSettings)
                                {
                                    if(i != script.interPoints.Count - 1)
                                    {
                                        EditorGUIUtility.labelWidth = 40;
                                        script.interPoints[i].TargetLayer.m_Id = EditorGUILayout.IntField("Layer", script.interPoints[i].TargetLayer.m_Id, GUILayout.Width(70));
                                        script.interPoints[i].TargetLayer.m_Alpha = EditorGUILayout.FloatField("Alpha", script.interPoints[i].TargetLayer.m_Alpha, GUILayout.Width(70));
                                        EditorGUIUtility.labelWidth = 167;
                                    }
                                }
                                GUILayout.FlexibleSpace();
                                if(script.interPoints[i].IsConnected)
                                    GUI.backgroundColor = new Color(0.8098273f, 1f, 0.2705883f, 1);
                                else
                                    GUI.backgroundColor = new Color(0.5598365f, 0.5660378f, 0.5420079f, 1);
                                if(GUILayout.Button("~", GUILayout.Width(20)))
                                {
                                    if(script.interPoints[i].TargetIP != null)
                                        script.interPoints[i].IsConnected = !script.interPoints[i].IsConnected;
                                    else
                                        Debug.LogError("Cannot connected because its don't have TargetIP!");
                                }
                                GUI.backgroundColor = Color.white;
                                GUI.backgroundColor = new Color(0.9150943f, 0.5407583f, 0.2978373f,1);
                                if(GUILayout.Button("XO", GUILayout.Width(27)))
                                {
                                    script.RemoveIP(script.interPoints[i].ID,true,true);
                                }
                                GUI.backgroundColor = Color.white;
                                GUI.backgroundColor = Color.red;
                                if(GUILayout.Button("X", GUILayout.Width(20)))
                                {
                                    script.RemoveIP(script.interPoints[i].ID);
                                }
                                GUI.backgroundColor = Color.white;
                                EditorGUILayout.EndHorizontal();//HOR1
                            }
                        }
                        EditorGUILayout.EndScrollView();//SV1
                    }
                    EditorGUILayout.EndVertical();//VER1
                }
                EditorGUILayout.BeginHorizontal();//HOR4
                EditorGUIUtility.labelWidth = 50;
                script.m_FocusObjectCreated = EditorGUILayout.Toggle("Focus", script.m_FocusObjectCreated,GUILayout.Width(90));
                EditorGUIUtility.labelWidth = 75;
                script.m_SetParent = EditorGUILayout.Toggle("Set Parent", script.m_SetParent, GUILayout.Width(120));
                //Clean Scene Toggle
                if(script.interPoints != null)
                {
                    if(script.interPoints.Count != 0)
                    {
                        EditorGUIUtility.labelWidth = 80;
                        script.m_CleanScene = EditorGUILayout.Toggle("Clean Scene", script.m_CleanScene, GUILayout.Width(90));
                        EditorGUIUtility.labelWidth = 167;
                    }
                }
                EditorGUILayout.EndHorizontal();//HOR4
                GameObject[] selectedObjects = Selection.gameObjects;
                EditorGUILayout.BeginHorizontal();//HOR2
                if(GUILayout.Button("Create IP"))
                {
                    GameObject go = script.CreateIP().gameObject;
                    if(script.m_FocusObjectCreated)
                    {
                        EditorGUIUtility.PingObject(go);
                        Selection.activeGameObject = go;
                    }
                    Repaint();
                }
                //Fetch Button
                if(selectedObjects.Length != 0)
                {
                    if(GUILayout.Button("Fetch IP"))
                    {
                        if(selectedObjects != null)
                        {
                            for(int i = 0; i < selectedObjects.Length; i++)
                            {
                                if(selectedObjects[i].GetComponent<IntersectPoint>())
                                {
                                    if(!script.interPoints.Contains(selectedObjects[i].GetComponent<IntersectPoint>()))
                                    {
                                        script.interPoints.Add(selectedObjects[i].GetComponent<IntersectPoint>());
                                    }
                                }
                            }
                            if(script.interPoints.Count > 0)
                                script.interPoints = script.interPoints.OrderBy(x => x.ID).ToList();
                            Repaint();
                        }
                    }
                }
                //ClearAll Button
                if(script.interPoints != null)
                {
                    if(script.interPoints.Count != 0)
                    {
                        if(GUILayout.Button("ClearAll"))
                        {
                            if(script.m_CleanScene)
                            {
                                for(int i = 0; i < script.interPoints.Count; i++)
                                {
                                    DestroyImmediate(script.interPoints[i].gameObject);
                                }
                            }
                            script.interPoints.Clear();
                            script.offsetPoints.Clear();
                            script.ResetTerrainLayer();
                            Repaint();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();//HOR2
                //Connect First
                if(selectedObjects.Length != 0)
                {
                    GameObject go = Selection.activeGameObject;
                    if(go != null)
                    {
                        IntersectPoint ip = go.GetComponent<IntersectPoint>();
                        if(ip != null)
                        {
                            if(script.interPoints.Count != 1)
                            {
                                if(GUILayout.Button("Connect First"))
                                {
                                    script.ConnectToFirstIP(ip);
                                }
                            }
                        }
                    }
                }
                if(script.m_IsActiveCreativeMode)
                    GUI.backgroundColor = new Color(0.577826f, 0.8962264f, 0.1564169f, 1);
                else
                    GUI.backgroundColor = new Color(0.8980392f, 0.3620706f, 0.1568627f, 1);
                GUIStyle acmButtonStyle = new GUIStyle("button");
                acmButtonStyle.fontSize = 23;
                acmButtonStyle.normal.textColor = Color.white;
                if(GUILayout.Button("Activate Creative Mode", acmButtonStyle,GUILayout.Height(60)))
                {
                    script.OpenOrCloseCreativeMode();
                }
                GUI.backgroundColor = Color.white;
                if(EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(script);
                }
                #endregion

                GUILayout.Space(10);

                #region VISUAL PROCESS
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Visual Process", EditorStyles.boldLabel);
                GUILayout.Box("", GUILayout.Height(0.3f), GUILayout.Width(458));
                EditorGUILayout.BeginHorizontal();//HOR6
                EditorGUIUtility.labelWidth = 80;
                script.m_FlatLayer = EditorGUILayout.IntField("Flat Layer", Mathf.Clamp(script.m_FlatLayer,0,script.terrainData.alphamapLayers - 1), GUILayout.Width(120));
                script.m_NewIPPosOffset = EditorGUILayout.FloatField("Pos Offset", script.m_NewIPPosOffset, GUILayout.Width(120));
                EditorGUIUtility.labelWidth = 167;
                EditorGUILayout.EndHorizontal();//HOR6
                EditorGUILayout.BeginVertical("Box");//VER2
                EditorGUILayout.BeginHorizontal();//HOR3
                if(GUILayout.Button("Reset Mesh"))
                {
                    script.ResetTerrainLayer();
                }
                if(script.m_DrawOffsetPoint)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;
                if(GUILayout.Button("ShowOrHide OP"))
                {
                    script.ShowOrHideOffsetPoint();
                }
                GUI.backgroundColor = Color.white;
                if(script.m_DrawReferencePoint)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;
                if(GUILayout.Button("ShowOrHide RP"))
                {
                    script.ShowOrHideReferencePoint();
                }
                GUI.backgroundColor = Color.white;
                if(script.m_DrawPointDistance)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;
                if(GUILayout.Button("ShowOrHide PD"))
                {
                    script.ShowOrHidePointDistances();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();//HOR3
                GUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();//HOR7
                GUI.backgroundColor = new Color(0.1568627f, 0.5008639f, 8980392f, 1);
                GUIStyle paintButtonStyle = new GUIStyle("button");
                paintButtonStyle.fontSize = 25;
                paintButtonStyle.normal.textColor = Color.white;
                if(GUILayout.Button("PAINT", paintButtonStyle, GUILayout.Width(220),GUILayout.Height(60)))
                {
                    script.RemoveOffsetPoints();
                    script.PaintMap();
                }
                EditorGUILayout.BeginVertical();//VER3
                GUI.backgroundColor = new Color(0.5348434f, 0.9528302f, 0.7950762f, 1);
                if(GUILayout.Button("APPLY",GUILayout.Height(30)))
                {
                    script.ApplyMap();
                }
                GUI.backgroundColor = new Color(0.745283f, 0.4255339f, 0.2355375f, 1);
                if(GUILayout.Button("REVERT", GUILayout.Height(30)))
                {
                    script.RevertMap();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();//VER3
                EditorGUILayout.BeginVertical();//VER4
                GUI.backgroundColor = new Color(0.5348434f, 0.9528302f, 0.7950762f, 1);
                if(GUILayout.Button("Undo", GUILayout.Height(30)))
                {
                    script.UndoAlphamap();
                }
                GUI.backgroundColor = new Color(0.745283f, 0.4255339f, 0.2355375f, 1);
                if(GUILayout.Button("Redo", GUILayout.Height(30)))
                {
                    script.ReDoAlphamap();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();//VER4
                EditorGUILayout.EndHorizontal();//HOR7
                EditorGUILayout.EndVertical();//VER2
                if(EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(script);
                }
                #endregion

                EditorGUILayout.EndVertical();//VER7
            }
        }
        void ControlDatabase()
        {
            if(script.dataBase == null)
            {
                TPP_Data data = TPP_DB.Load();
                if(data != null)
                    script.dataBase = data;
                else
                    script.dataBase = new TPP_Data();
            }
        }
        void SaveChanges()
        {
            if(script.dataBase != null)
            {
                if(script.m_ChangedTerrain)
                {
                    TPP_DB.Save(script.dataBase);
                    script.m_ChangedTerrain = false;
                }
            }
        }
    }
#endif
    public class TPP_DB
    {
        static string dataPath;

        public static void Save(System.Object _saveObject)
        {
            if(_saveObject == null) return;

            BinaryFormatter bf = new BinaryFormatter();
            if(dataPath == null)
            {
                GetDirectory();
            }
            FileStream file = null;
            if(!File.Exists(dataPath + "/Save/db.sav"))
            {
                file = File.Create(dataPath + "/Save/db.sav");
            }
            else
            {
                file = File.Open(dataPath + "/Save/db.sav", FileMode.Open);
            }
            if(file != null)
            {
                bf.Serialize(file, _saveObject);
                file.Close();
            }
        }
        public static TPP_Data Load()
        {
            if(dataPath == null)
            {
                GetDirectory();
            }
            if(dataPath != null)
            {
                if(File.Exists(dataPath + "/Save/db.sav"))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(dataPath + "/Save/db.sav", FileMode.Open);
                    TPP_Data loadedData = (TPP_Data)bf.Deserialize(file);
                    file.Close();
                    return loadedData;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        static void GetDirectory()
        {
            string[] paths = Directory.GetDirectories(Application.dataPath, "TerrainPathPainter", System.IO.SearchOption.TopDirectoryOnly);
            if(paths.Length > 0)
            {
                dataPath = paths[0];
                dataPath = dataPath.Replace('\\', '/');
                dataPath = dataPath.Substring(dataPath.IndexOf("Assets"));
            }
        }
    }
}