using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum ShapeGrid
{
    Square,
    Triangle,
    Hexagon,
}

public enum MatGrid
{
    Standard,
    HDRP,
}

public class GridSpawner : EditorWindow
{
    private string baseNameCell; //Every cell object will be named with this string input
    private MatGrid matShaderSelected; //Stores which option is selected for the material
    private ShapeGrid shapeSelected; //Stores which option is selected for the shape of every cell
    private float sizeCell; //Every cell will base its length and width on this value
    private float heightCell; //Every cell will take this value as its height, unless randomized (in case, this value will be the average height of all cells)
    private int rowsGrid; //Amount of rows present in the grid
    private int columnsGrid; //Amount of columns present in the grid
    private float gutterGrid; //Size of the gutter between the cells
    private Object prefabSource; //The prefab that will be placed randomly on the grid, unless amountRandom is set to 0
    private float sizePrefab; //Every prefab will scale its size based on this value
    private int amountRandom; //Percentage of prefabs placed relative to the amount of cells present in the grid
    private bool prefabOptions; //Controls whether prefab spawning is enabled

    private GameObject parentGrid; //The parent object that holds all the cells and prefabs
    private float xPos; //x position used to place current cell
    private float zPos; //z position used to place current cell
    private bool triangleUp; //Decides whether a cell with a triangle shape is facing upwards or not to fit a triangle grid
    private bool rowEven; //Decides whether the current row with cells with a hexagon shape is an even number (2nd, 4th, 6th...row)

    private int numCell; //Number of cell used to decide whether current cell needs a prefab
    private int numPrefab; //Number used to name all prefabs ("Prefab" + numPrefab)

    private float cellAmount; //Determines how many cells need to have a prefab placed on them
    private int currentRandomCell; //Determines how many prefabs have already been placed and how many more need to be placed
    private float placePrefab; //Random number that decides which cell the tool will try to place a prefab
    private bool[] cellPrefab; //Stores which cells have a prefab on them already

    [MenuItem("Window/Grid Spawner")]
    static void OpenWindow()
    {
        // Setting up editor window
        GridSpawner window = (GridSpawner)GetWindow(typeof(GridSpawner), false, "Grid Spawner");
        window.minSize = new Vector2(150, 150);
        window.Show();
    }

    void OnGUI()
    {
        // Creating fields for the grid settings
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        baseNameCell = EditorGUILayout.TextField("Base Cell Name", baseNameCell);
        matShaderSelected = (MatGrid)EditorGUILayout.EnumPopup("Base Material Shader", matShaderSelected);
        shapeSelected = (ShapeGrid)EditorGUILayout.EnumPopup("Shape Cell ", shapeSelected);
        sizeCell = Mathf.Max(0, EditorGUILayout.FloatField("Size Cell", sizeCell));
        heightCell = Mathf.Max(0, EditorGUILayout.FloatField("Height Cell", heightCell));
        rowsGrid = Mathf.Max(0, EditorGUILayout.IntField("Rows", rowsGrid));
        columnsGrid = Mathf.Max(0, EditorGUILayout.IntField("Colums", columnsGrid));
        gutterGrid = Mathf.Max(0, EditorGUILayout.FloatField("Size Gutter", gutterGrid));

        EditorGUILayout.Space();

        prefabOptions = GUILayout.Toggle(prefabOptions, "Enable Prefab Spawning");
        GUI.enabled = prefabOptions;
        // Creating fields for the prefab settings
        GUILayout.Label("Prefab settings", EditorStyles.boldLabel);
        prefabSource = EditorGUILayout.ObjectField("Prefab", prefabSource, typeof(Object), true);
        sizePrefab = Mathf.Max(0, EditorGUILayout.FloatField("Size", sizePrefab));

        EditorGUILayout.Space();

        // Creating fields for the prefab spawning settings
        GUILayout.Label("Prefab Spawning settings", EditorStyles.boldLabel);
        amountRandom = EditorGUILayout.IntSlider("Amount", amountRandom, 1, 100);

        EditorGUILayout.Space();

        //Disable "Replace Grid" button if no previous grid exists
        GUI.enabled = false;

        if (parentGrid != null)
        {
            GUI.enabled = true;
        }

        // Creating replace button
        if (GUILayout.Button("Replace Grid"))
        {
            DestroyImmediate(parentGrid);
            CreateGrid();
        }

        GUI.enabled = true;

        // Creating create button
        if (GUILayout.Button("Generate New Grid"))
        {
            CreateGrid();
        }
    }

    void CreateGrid()
    {
        // Creating parent objects
        parentGrid = new GameObject("Grid");

        GameObject parentPrefab = null;
        if (prefabOptions && prefabSource != null)
        {
            parentPrefab = new GameObject("Prefabs");
            parentPrefab.transform.parent = parentGrid.transform;
        }

        GameObject parentCell = new GameObject("Cells");
        parentCell.transform.parent = parentGrid.transform;

        // Calculates the amount of cells that need to have a prefab present
        cellAmount = Mathf.Round((float)columnsGrid * (float)rowsGrid / 100 * (float)amountRandom);

        cellPrefab = new bool[columnsGrid * rowsGrid];

        RandomPick();

        // Checks which base shape has been selected, and creates a grid
        int row;
        int column;
        switch (shapeSelected)
        {
            // Square selected
            case ShapeGrid.Square:
                row = 0;
                while (row < rowsGrid)
                {
                    column = 0;
                    while (column < columnsGrid)
                    {
                        if (prefabOptions && prefabSource != null && cellPrefab[numCell])
                        {
                            numPrefab++;
                            Vector3 prefabPos = new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0.5f);
                            InstantiatePrefab(prefabPos, parentPrefab);
                        }
                        numCell++;

                        //Creating triangle information for mesh
                        int[] triangles = {
                            // Bottom triangles
                            2, 1, 0,
                            3, 2, 0,
                            // Back triangles
                            6, 5, 4,
                            7, 6, 4,
                            // Right triangles
                            10, 9, 8,
                            11, 10, 8,
                            // Left triangles
                            14, 13, 12,
                            15, 14, 12,
                            // Top triangles
                            18, 17, 16,
                            19, 18, 16,
                            // Front triangles
                            22, 21, 20,
                            23, 22, 20,
                        };

                        Square(parentCell, triangles);

                        xPos += sizeCell + gutterGrid;
                        column++;
                    }
                    xPos = 0f;
                    zPos += sizeCell + gutterGrid;
                    row++;
                }
                ResetValues();
                break;

            // Triangle selected
            case ShapeGrid.Triangle:
                row = 0;
                while (row < rowsGrid)
                {
                    column = 0;
                    while (column < columnsGrid)
                    {
                        if (prefabOptions && prefabSource != null && cellPrefab[numCell])
                        {
                            if (triangleUp)
                            {
                                numPrefab++;
                                Vector3 prefabPos = new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0.33f);
                                InstantiatePrefab(prefabPos, parentPrefab);
                            }
                            else
                            {
                                numPrefab++;
                                Vector3 prefabPos = new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0.67f);
                                GameObject prefabSpawned = (GameObject)Instantiate(prefabSource, prefabPos, Quaternion.identity);
                                InstantiatePrefab(prefabPos, parentPrefab);
                            }
                        }
                        numCell++;

                        //Creating triangle information for mesh
                        if (triangleUp)
                        {
                            int[] triangles = {
                                // Bottom triangles
                                2, 1, 0,
                                // Right triangles
                                5, 4, 3,
                                6, 5, 3,
                                // Left triangles
                                9, 8, 7,
                                10, 9, 7,
                                // Top triangles
                                13, 12, 11,
                                // Front triangles
                                16, 15, 14,
                                17, 16, 14,
                            };

                            Triangle(parentCell, triangles);
                        }
                        else
                        {
                            int[] triangles = {
                                // Bottom triangles
                                2, 1, 0,
                                // Back triangles
                                5, 4, 3,
                                6, 4, 5,
                                // Right triangles
                                9, 8, 7,
                                10, 9, 7,
                                // Left triangles
                                13, 12, 11,
                                14, 13, 11,
                                // Top triangles
                                17, 16, 15,
                            };

                            Triangle(parentCell, triangles);
                        }

                        xPos += sizeCell / 2 + gutterGrid;
                        column++;
                    }
                    triangleUp = !triangleUp;
                    xPos = 0f;
                    zPos += sizeCell + gutterGrid;
                    row++;
                }
                ResetValues();
                triangleUp = true;
                break;

            // Hexagon selected
            case ShapeGrid.Hexagon:
                xPos = -sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f);
                row = 0;
                while (row < rowsGrid)
                {
                    column = 0;
                    while (column < columnsGrid)
                    {
                        if (prefabOptions && prefabSource != null && cellPrefab[numCell])
                        {
                            numPrefab++;
                            Vector3 prefabPos = new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0.5f);
                            InstantiatePrefab(prefabPos, parentPrefab);
                        }
                        numCell++;

                        //Creating triangle information for mesh
                        int[] triangles = {
                            // Bottom triangles
                            1, 0, 6,
                            2, 1, 6,
                            3, 2, 6,
                            4, 3, 6,
                            5, 4, 6,
                            0, 5, 6,
                            // Back right triangles
                            7, 9, 8,
                            7, 10, 9,
                            // Right triangles
                            11, 13, 12,
                            11, 14, 13,
                            // Front right triangles
                            15, 17, 16,
                            15, 18, 17,
                            // Front left triangles
                            19, 21, 20,
                            19, 22, 21,
                            // Left triangles
                            23, 25, 24,
                            23, 26, 25,
                            // Back left triangles
                            27, 29, 28,
                            27, 30, 29,
                            // Top triangles
                            37, 31, 32,
                            37, 32, 33,
                            37, 33, 34,
                            37, 34, 35,
                            37, 35, 36,
                            37, 36, 31,
                        };

                        Hexagon(parentCell, triangles);

                        xPos += sizeCell * (Mathf.Sqrt(3f) / 2f) + gutterGrid;
                        column++;
                    }
                    if (rowEven)
                    {
                        xPos = -sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f);
                        zPos += sizeCell * 0.75f + gutterGrid;
                        rowEven = false;
                    }
                    else
                    {
                        xPos = sizeCell * (Mathf.Sqrt(3f) / 4f) - sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f) + gutterGrid / 2;
                        zPos += sizeCell * 0.75f + gutterGrid;
                        rowEven = true;
                    }
                    row++;
                }
                ResetValues();
                break;
        }
    }

    // Creates a square mesh with a height (also considered a cube)
    void Square(GameObject parent, int[] triangles)
    {
        // Creating arrays for mesh information
        Vector3[] vertices = {
            // Bottom vertices
            new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 0f), //0
            new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 0f), //1
            new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 1f), //2
            new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 1f), //3

            // Back vertices
            new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 1f), //4
            new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 1f), //5
            new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 1f), //6
            new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 1f), //7

            // Right vertices
            new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 1f), //8
            new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 0f), //9
            new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 0f), //10
            new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 1f), //11

            // Left vertices
            new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 0f), //12
            new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 1f), //13
            new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 1f), //14
            new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 0f), //15

            // Top vertices
            new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 0f), //16
            new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 0f), //17
            new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 1f), //18
            new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 1f), //19

            // Front vertices
            new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 0f), //20
            new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 0f), //21
            new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 0f), //22
            new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 0f), //23
        };
        CreateMesh(vertices, triangles, parent);
    }

    // Creates a triangle mesh with a height
    void Triangle(GameObject parent, int[] triangles)
    {
        // Checking which way the triangle is facing
        if (triangleUp)
        {
            // Creating arrays for mesh information
            Vector3[] vertices = {
                // Bottom vertices
                new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 0f), //0
                new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 0f), //1
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 1f), //2

                // Right vertices
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 1f), //3
                new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 0f), //4
                new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 0f), //5
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 1f), //6

                // Left vertices
                new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 0f), //7
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 1f), //8
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 1f), //9
                new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 0f), //10

                // Top vertices
                new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 0f), //11
                new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 0f), //12
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 1f), //13

                // Front vertices
                new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 0f), //14
                new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 0f), //15
                new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 0f), //16
                new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 0f), //17
            };
            CreateMesh(vertices, triangles, parent);
        }
        else
        {
            // Creating arrays for mesh information
            Vector3[] vertices = {
                // Bottom vertices
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0f), //0
                new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 1f), //1
                new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 1f), //2

                // Back vertices
                new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 1f), //3
                new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 1f), //4
                new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 1f), //5
                new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 1f), //6

                // Right vertices
                new Vector3(xPos + sizeCell * 0f, heightCell * 0f, zPos + sizeCell * 1f), //7
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0f), //8
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0f), //9
                new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 1f), //10

                // Left vertices
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0f), //11
                new Vector3(xPos + sizeCell * 1f, heightCell * 0f, zPos + sizeCell * 1f), //12
                new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 1f), //13
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0f), //14

                // Top vertices
                new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0f), //15
                new Vector3(xPos + sizeCell * 1f, heightCell * 1f, zPos + sizeCell * 1f), //16
                new Vector3(xPos + sizeCell * 0f, heightCell * 1f, zPos + sizeCell * 1f), //17
            };
            CreateMesh(vertices, triangles, parent);
        }
    }

    // Creates a hexagon mesh with a height
    void Hexagon(GameObject parent, int[] triangles)
    {
        // Creating arrays for mesh information
        Vector3[] vertices = {
        // Bottom vertices
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 1f), //0
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.75f), //1
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.25f), //2
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0f), //3
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.25f), //4
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.75f), //5
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0.5f), //6
        
        // Back right vertices
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 1f), //7
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.75f), //8
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.75f), //9
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 1f), //10

        // Right vertices
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.75f), //11
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.25f), //12
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.25f), //13
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.75f), //14

        // Front right vertices
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.25f), //15
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0f), //16
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0f), //17
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.25f), //18

        // Front left vertices
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 0f), //19
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.25f), //20
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.25f), //21
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0f), //22

        // Left vertices
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.25f), //23
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.75f), //24
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.75f), //25
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.25f), //26

        // Back left vertices
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 0f, zPos + sizeCell * 0.75f), //27
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 0f, zPos + sizeCell * 1f), //28
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 1f), //29
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.75f), //30

        // Bottom vertices
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 1f), //31
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.75f), //32
        new Vector3(xPos + sizeCell * (0.5f + Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.25f), //33
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0f), //34
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.25f), //35
        new Vector3(xPos + sizeCell * (0.5f - Mathf.Sqrt(3f) / 4f), heightCell * 1f, zPos + sizeCell * 0.75f), //36
        new Vector3(xPos + sizeCell * 0.5f, heightCell * 1f, zPos + sizeCell * 0.5f), //37
        };
        CreateMesh(vertices, triangles, parent);
    }

    void CreateMesh(Vector3[] vertices, int[] triangles, GameObject parent)
    {
        // Creating mesh
        Mesh cell = new Mesh();

        // Appointing mesh information to mesh
        cell.vertices = vertices;
        cell.triangles = triangles;
        cell.RecalculateNormals();
        cell.name = baseNameCell;

        // Creating game object
        GameObject cellObject = new GameObject(baseNameCell + numCell, typeof(MeshFilter), typeof(MeshRenderer));

        cellObject.GetComponent<MeshFilter>().mesh = cell;

        CreateMaterial(cellObject);
        cellObject.transform.parent = parent.transform;
    }

    void CreateMaterial(GameObject cellObject)
    {
        // Checks which shader is selected, and then creates a material
        switch (matShaderSelected)
        {
            case MatGrid.Standard:
                cellObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                break;

            case MatGrid.HDRP:
                cellObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("HDRP/Lit"));
                break;
        }
    }

    void ResetValues()
    {
        xPos = 0f;
        zPos = 0f;
        numCell = 0;
        numPrefab = 0;
        cellAmount = 0f;
        currentRandomCell = 0;

        int cell = 0;
        while (cell < cellAmount)
        {
            cellPrefab[cell] = false;
            cell++;
        }
    }

    void InstantiatePrefab(Vector3 prefabPos, GameObject parentPrefab)
    {
        GameObject prefabSpawned = (GameObject)Instantiate(prefabSource, prefabPos, Quaternion.identity);
        prefabSpawned.name = "Prefab" + numPrefab;
        prefabSpawned.transform.parent = parentPrefab.transform;
        prefabSpawned.transform.localScale = new Vector3(sizePrefab, sizePrefab, sizePrefab);
    }

    void RandomPick()
    {
        int randomAmount = 0;
        while (randomAmount < columnsGrid * rowsGrid)
        {
            if (currentRandomCell < cellAmount)
            {
                placePrefab = Mathf.Min(Mathf.Round(Random.value * ((float)columnsGrid * (float)rowsGrid)), columnsGrid * rowsGrid - 1);
                if (!cellPrefab[(int)placePrefab])
                {
                    cellPrefab[(int)placePrefab] = true;
                    currentRandomCell++;
                }
                else
                {
                    randomAmount--;
                }
            }
            randomAmount++;
        }
    }
}