using UnityEngine;

[RequireComponent(typeof(MeshFilter))] //Script requires a Mesh Filter Component
public class MeshGenerator : MonoBehaviour
{
    //Size of your mesh
    public int xSize;
    public int zSize;

    //Noise Amplitudes
    public float amp1;
    public float amp2;
    public float amp3;

    //Noise Scales
    public float scale1;
    public float scale2;
    public float scale3;

    private Mesh mesh;

    private Vector3[] vertices;
    private int[] triangles;

    //uvs are needed when you load a texture on your terrain
    //private Vector2[] uvs;

    //Otherwise use colors with a gradient
    public Color[] colors;
    public Gradient gradient;

    //min and max height are needed for the gradient
    private float minTerrainHeight;
    private float maxTerrainHeight;

    // Start is called before the first frame update
    void Start()
    {
        //Make a new mesh and set the mesh to the mesh of the component
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
    }

    void Update()
    {
        //These methods can be placed in Start(), but placing them here updates the colors by the gradient in Play Mode
        UpdateColor();
        UpdateMesh();
    }

    void CreateShape()
    {
        //vert keeps track of which vertix you are at
        int vert = 0;
        //tris keeps track of which triangle you are at
        int tris = 0;
        //vertices are points of your terrain. This is (size + 1)^2. Because a line is connected by two points (x-x), and two lines are connected by three points (x-x-x)
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        //a mesh is made of triangles, so there are 3 three side and you need three vertices to create a triangle
        //the generated mesh is a rectangle, thus the required amount of triangles is the size times three times two
        triangles = new int[xSize * zSize * 6];

        //uvs are needed when you load a texture on your terrain
        //uvs = new Vector2[vertices.Length];

        //The colors are determined for each vertix, thus the colors array is the same length as the vertices array
        colors = new Color[vertices.Length];

        //A nested loop to set all the vertices by z-axis, then by x-axis
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = CalculateNoise(x, z);
                vertices[i] = new Vector3(x, y, z);

                //Keep track of the min and max terrain height for the gradient
                if (y > maxTerrainHeight)
                {
                    maxTerrainHeight = y;
                }

                if (y < minTerrainHeight)
                {
                    minTerrainHeight = y;
                }

                i++;
            }
        }

        //a nested loop to set the triangles for the mesh by by z-axis, then by x-axis
        //Keep the same order as the previous loop, otherwise you will mess up the triangles. In this first by z, then x
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                //the order of vertices matters. Triangles are made clockwise. If you do it counter clockwise, the triangle will face the opposite direction
                // 1 - - 2
                // |   /
                // | /
                // 0
                //Correct: 0, 1, 2 or 1, 2, 0
                //Incorrect: 1, 0, 2 or 2, 1, 0

                //First triangle
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                //Second triangle
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + zSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                //How the triangles are made is difficult to explain. Please refer to https://youtu.be/64NblGkAabk?t=316
                //Brackeys explains it with well with visual references

                //Go to the next vertix
                vert++;
                //You add six to tris, because six vertices are signed for two triangles (you keep track of the triangles by three points)
                tris += 6;
            }
            //This is needed, otherwise the next triangle in the following row will use the vertix on the previous row
            vert++;
        }

        //You need this when you use uvs. For textures,the uvs values need to be between 0 and 1. So, you need the relative position of vertices between 0 and 1
        //for (int i = 0, z = 0; z <= zSize; z++)
        //{
        //    for (int x = 0; x <= xSize; x++)
        //    {
        //        uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
        //        i++;
        //    }
        //}
       
        UpdateColor();
    }

    //Update the color of your terrain
    private void UpdateColor()
    {
        //a nested loop to set the color at each vertix by z-axis, then by x-axis
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                //for the gradient you need a value between 0 and 1. Using InverseLerp, you get the relative value of your current height between min and max height
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }
    }

    void UpdateMesh()
    {
        //Clear your mesh first!
        mesh.Clear();

        //Set the vertices of your mesh
        mesh.vertices = vertices;
        //Set the triangles of your mesh
        mesh.triangles = triangles;

        //Set uv of your mesh (if you use a texture)
        //mesh.uv = uvs;

        //Set the colors of your mesh
        mesh.colors = colors;

        //Recalculate Normals of your mesh. It fixes the lighting
        mesh.RecalculateNormals();
    }

    //To make interesting generated terrain you stack noises on each other
    //This is called Octaves.
    //You get noisier values with a higher amplitude. Less smooth
    //You get greater values with a larger scale. Bigger delta in height
    private float CalculateNoise(float x, float z)
    {

        float noise;
        noise = Mathf.PerlinNoise(x, z) * 5;
        noise += Mathf.PerlinNoise(x * amp1, z * amp1) * scale1;
        noise -= Mathf.PerlinNoise(x * amp2, z * amp2) * scale2;
        noise += Mathf.PerlinNoise(x * amp3, z * amp3) * scale3 * 2;
        return noise;
    }

    /*
     * To use the gradient, you will need the Lightweight Render Pipeline aka Lightweight RP from the Package Manager
     * Create a Pipeline Asset by going to your Project Window > Right-Click to open the menu > Create > Rendering > Universal Render Pipeline > Pipeline Asset (Forward Rendering)
     * Then go to Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings. Select the UniversalRenderPipelineAsset
     * Create a Shader by Project Window > Right-Click to open the menu > Create > Shader > PBR Shader
     * Modify the Shader by double clicking on it to open it. Then in the Shader Window, press Space, search for Vertex Color, add the Vertex Color to the Shader. 
     * Connect the Vertex Color to the Albedo of PBR Master.
     * Save Asset. 
     */
}
