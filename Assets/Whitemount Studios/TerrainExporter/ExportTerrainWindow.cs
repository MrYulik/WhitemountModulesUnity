using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ExportTerrainWindow : EditorWindow
{
    private SaveFormat _saveFormat = SaveFormat.Triangles;
    private SaveResolution _saveResolution = SaveResolution.Half;
    private SaveResolutionTexture _saveResolutionTexture = SaveResolutionTexture.Texture512x512;
    private int _count;
    private int _counter;
    private int _totalCount;
    private int _progressUpdateInterval = 10000;

    private static TerrainData _terrain;
    private static Vector3 _terrainPosition;

    [MenuItem("Whitemount/ExportTerrain/Export To Object")]
    private static void Init()
    {
        _terrain = null;
        Terrain terrainObject = Selection.activeObject as Terrain;
        if (!terrainObject)
            terrainObject = Terrain.activeTerrain;

        if (terrainObject)
        {
            _terrain = terrainObject.terrainData;
            _terrainPosition = terrainObject.transform.position;
        }

        EditorWindow.GetWindow<ExportTerrainWindow>().Show();
    }

    private void OnGUI()
    {
        if (!_terrain)
        {
            GUILayout.Label("Ландшафт не удалось найти.");
            if (GUILayout.Button("Отмена"))
                EditorWindow.GetWindow<ExportTerrainWindow>().Close();

            return;
        }
        EditorGUILayout.LabelField("Экспорт ландшафта | Whitemount Studios");
        _saveFormat = (SaveFormat)EditorGUILayout.EnumPopup("Формат", _saveFormat);
        _saveResolution = (SaveResolution)EditorGUILayout.EnumPopup("Разрешение", _saveResolution);
        _saveResolutionTexture = (SaveResolutionTexture)EditorGUILayout.EnumPopup("Разрешение текстур", _saveResolutionTexture);

        if (GUILayout.Button("Экспортировать"))
            ExportTerrain();

        if (GUILayout.Button("Отмена"))
            EditorWindow.GetWindow<ExportTerrainWindow>().Close();
    }

    private void ExportTerrain()
    {
        string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", "Terrain", "obj");
        int resolution = _terrain.heightmapResolution;
        Vector3 meshScale = _terrain.size;
        int tRes = (int)Mathf.Pow(2, (int)_saveResolution);
        meshScale = new Vector3(meshScale.x / (resolution - 1) * tRes, meshScale.y, meshScale.z / (resolution - 1) * tRes);
        Vector2 uvScale = new Vector2(1.0f / (resolution - 1), 1.0f / (resolution - 1));
        float[,] tData = _terrain.GetHeights(0, 0, resolution, resolution);

        resolution = (resolution - 1) / tRes + 1;
        resolution = (resolution - 1) / tRes + 1;
        Vector3[] tVertices = new Vector3[resolution * resolution];
        Vector2[] tUV = new Vector2[resolution * resolution];

        int[] tPolys;

        if (_saveFormat == SaveFormat.Triangles)
            tPolys = new int[(resolution - 1) * (resolution - 1) * 6];
        else
            tPolys = new int[(resolution - 1) * (resolution - 1) * 4];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                tVertices[y * resolution + x] = Vector3.Scale(meshScale, new Vector3(-y, tData[x * tRes, y * tRes], x)) + _terrainPosition;
                tUV[y * resolution + x] = Vector2.Scale(new Vector2(x * tRes, y * tRes), uvScale);
            }
        }

        int index = 0;
        if (_saveFormat == SaveFormat.Triangles)
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    tPolys[index++] = (y * resolution) + x;
                    tPolys[index++] = ((y + 1) * resolution) + x;
                    tPolys[index++] = (y * resolution) + x + 1;

                    tPolys[index++] = ((y + 1) * resolution) + x;
                    tPolys[index++] = ((y + 1) * resolution) + x + 1;
                    tPolys[index++] = (y * resolution) + x + 1;
                }
            }
        }
        else
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    tPolys[index++] = (y * resolution) + x;
                    tPolys[index++] = ((y + 1) * resolution) + x;
                    tPolys[index++] = ((y + 1) * resolution) + x + 1;
                    tPolys[index++] = (y * resolution) + x + 1;
                }
            }
        }

        StreamWriter streamWriter = new StreamWriter(fileName);
        try
        {
            streamWriter.WriteLine("# Unity terrain OBJ File");
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            _counter = _count = 0;
            _totalCount = (tVertices.Length * 2 + (_saveFormat == SaveFormat.Triangles ? tPolys.Length / 3 : tPolys.Length / 4)) / _progressUpdateInterval;
            for (int i = 0; i < tVertices.Length; i++)
            {
                UpdateProgress();
                StringBuilder sb = new StringBuilder("v ", 20);
                sb.Append(tVertices[i].x.ToString()).Append(" ").
                   Append(tVertices[i].y.ToString()).Append(" ").
                   Append(tVertices[i].z.ToString());
                streamWriter.WriteLine(sb);
            }
            for (int i = 0; i < tUV.Length; i++)
            {
                UpdateProgress();
                StringBuilder sb = new StringBuilder("vt ", 22);
                sb.Append(tUV[i].x.ToString()).Append(" ").
                   Append(tUV[i].y.ToString());
                streamWriter.WriteLine(sb);
            }
            if (_saveFormat == SaveFormat.Triangles)
            {
                for (int i = 0; i < tPolys.Length; i += 3)
                {
                    UpdateProgress();
                    StringBuilder sb = new StringBuilder("f ", 43);
                    sb.Append(tPolys[i] + 1).Append("/").Append(tPolys[i] + 1).Append(" ").
                       Append(tPolys[i + 1] + 1).Append("/").Append(tPolys[i + 1] + 1).Append(" ").
                       Append(tPolys[i + 2] + 1).Append("/").Append(tPolys[i + 2] + 1);
                    streamWriter.WriteLine(sb);
                }
            }
            else
            {
                for (int i = 0; i < tPolys.Length; i += 4)
                {
                    UpdateProgress();
                    StringBuilder sb = new StringBuilder("f ", 57);
                    sb.Append(tPolys[i] + 1).Append("/").Append(tPolys[i] + 1).Append(" ").
                       Append(tPolys[i + 1] + 1).Append("/").Append(tPolys[i + 1] + 1).Append(" ").
                       Append(tPolys[i + 2] + 1).Append("/").Append(tPolys[i + 2] + 1).Append(" ").
                       Append(tPolys[i + 3] + 1).Append("/").Append(tPolys[i + 3] + 1);
                    streamWriter.WriteLine(sb);
                }
            }
        }
        catch (Exception err)
        {
            Debug.Log("Ошибка сохранения, сообщение ошибки: " + err.Message);
        }
        streamWriter.Close();

        _terrain = null;
        EditorUtility.DisplayProgressBar("Сохранение ландшафта на диск", "Это может занять некоторое время...", 1f);
        EditorWindow.GetWindow<ExportTerrainWindow>().Close();
        EditorUtility.ClearProgressBar();
    }

    private void UpdateProgress()
    {
        if (_counter++ == _progressUpdateInterval)
        {
            _count = 0;
            EditorUtility.DisplayProgressBar("Сохранение...", "", Mathf.InverseLerp(0, _totalCount, ++_count));
        }
    }
}
