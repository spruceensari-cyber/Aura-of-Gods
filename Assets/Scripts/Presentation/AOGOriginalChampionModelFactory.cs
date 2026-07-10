using System.Collections.Generic;
using UnityEngine;

public static class AOGOriginalChampionModelFactory
{
    public static void BuildChampion(Transform parent, string championId, Color accent)
    {
        if (parent == null || parent.Find("AOG_Original_Champion_Model") != null)
            return;

        GameObject root = new GameObject("AOG_Original_Champion_Model");
        root.transform.SetParent(parent, false);

        Material body = CreateLitMaterial(championId + "_Body", BodyColor(championId, accent), 0.42f, 0.24f);
        Material secondary = CreateLitMaterial(championId + "_Secondary", SecondaryColor(championId, accent), 0.34f, 0.10f);
        Material skin = CreateLitMaterial(championId + "_Skin", new Color(0.78f, 0.56f, 0.48f), 0.46f, 0.02f);
        Material energy = CreateEmissionMaterial(championId + "_Energy", accent, 4.2f);
        Material dark = CreateLitMaterial(championId + "_Dark", new Color(0.018f, 0.026f, 0.045f), 0.48f, 0.32f);

        bool feminineMage = championId == "nyra" || championId == "pyrelle" || championId == "selene" || championId == "lyra";
        float bodyHeight = feminineMage ? 1.55f : 1.75f;
        float bottomRadius = feminineMage ? 0.54f : 0.66f;
        float topRadius = feminineMage ? 0.34f : 0.48f;

        CreateMeshObject("Torso", root.transform, BuildTaperedBodyMesh(10, bodyHeight, bottomRadius, topRadius), body, new Vector3(0f, 0.85f, 0f));
        CreateMeshObject("Head", root.transform, BuildUvSphereMesh(14, 10, 0.38f), skin, new Vector3(0f, 2.20f, 0f));
        CreateMeshObject("Hair", root.transform, BuildUvSphereMesh(14, 10, 0.43f), secondary, new Vector3(0f, 2.28f, -0.05f), new Vector3(1.05f, 1.08f, 0.88f));

        BuildShoulders(root.transform, championId, body, secondary, energy);
        BuildRoleIdentity(root.transform, championId, accent, body, secondary, energy, dark);
        BuildEnergyCore(root.transform, energy, championId);
    }

    private static void BuildShoulders(Transform root, string id, Material body, Material secondary, Material energy)
    {
        float width = id == "auron" || id == "kaelith" ? 0.92f : 0.72f;
        Mesh pauldron = BuildWedgeMesh(0.42f, 0.22f, 0.52f);
        GameObject left = CreateMeshObject("Shoulder_L", root, pauldron, secondary, new Vector3(-width, 1.72f, 0f));
        left.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
        GameObject right = CreateMeshObject("Shoulder_R", root, pauldron, secondary, new Vector3(width, 1.72f, 0f));
        right.transform.localScale = new Vector3(-1f, 1f, 1f);
        right.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);

        if (id == "kaelith" || id == "auron")
        {
            CreateMeshObject("Shoulder_Core_L", left.transform, BuildCrystalMesh(0.22f, 0.55f), energy, new Vector3(0f, 0.28f, 0f));
            CreateMeshObject("Shoulder_Core_R", right.transform, BuildCrystalMesh(0.22f, 0.55f), energy, new Vector3(0f, 0.28f, 0f));
        }
    }

    private static void BuildRoleIdentity(Transform root, string id, Color accent, Material body, Material secondary, Material energy, Material dark)
    {
        if (id == "auron")
        {
            GameObject blade = CreateMeshObject("Solar_Blade", root, BuildBladeMesh(0.22f, 1.85f, 0.12f), energy, new Vector3(0.92f, 1.25f, 0.18f));
            blade.transform.localRotation = Quaternion.Euler(8f, 0f, -16f);
            GameObject shield = CreateMeshObject("Solar_Shield", root, BuildDiscMesh(20, 0.70f, 0.12f), secondary, new Vector3(-0.82f, 1.25f, 0.18f));
            shield.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else if (id == "vesper")
        {
            GameObject bow = CreateMeshObject("Void_Bow", root, BuildArcRibbonMesh(20, 1.35f, 0.10f, 150f), energy, new Vector3(0.72f, 1.35f, 0.10f));
            bow.transform.localRotation = Quaternion.Euler(0f, 90f, -12f);
            CreateMeshObject("Quiver", root, BuildTaperedBodyMesh(8, 0.75f, 0.18f, 0.24f), dark, new Vector3(-0.48f, 1.28f, -0.34f));
        }
        else if (id == "nyra")
        {
            for (int i = 0; i < 5; i++)
            {
                float spread = Mathf.Lerp(-56f, 56f, i / 4f);
                GameObject tail = CreateMeshObject("Spirit_Tail_" + i, root, BuildCurvedTailMesh(8, 1.55f, 0.22f), energy, new Vector3(0f, 0.95f, -0.36f));
                tail.transform.localRotation = Quaternion.Euler(38f, spread, spread * 0.22f);
            }
            BuildEarPair(root, energy, 0.48f);
        }
        else if (id == "pyrelle")
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 360f / 7f;
                Vector3 pos = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.34f);
                GameObject flame = CreateMeshObject("Flame_Crown_" + i, root, BuildCrystalMesh(0.12f, 0.55f), energy, new Vector3(pos.x, 2.72f, pos.z));
                flame.transform.localRotation = Quaternion.Euler(-10f, angle, 0f);
            }
            CreateMeshObject("Phoenix_Mantle", root, BuildCapeMesh(1.25f, 1.55f, 0.32f), secondary, new Vector3(0f, 1.30f, -0.25f));
        }
        else if (id == "selene")
        {
            CreateMeshObject("Astral_Halo", root, BuildTorusMesh(28, 8, 0.64f, 0.055f), energy, new Vector3(0f, 2.82f, 0f));
            CreateMeshObject("Astral_Staff", root, BuildBladeMesh(0.12f, 1.65f, 0.08f), dark, new Vector3(0.72f, 1.30f, 0.10f));
            CreateMeshObject("Star_Focus", root, BuildCrystalMesh(0.26f, 0.54f), energy, new Vector3(0.72f, 2.20f, 0.10f));
        }
        else if (id == "kaelith")
        {
            GameObject blade = CreateMeshObject("Eclipse_Blade", root, BuildBladeMesh(0.30f, 1.95f, 0.14f), energy, new Vector3(0.96f, 1.20f, 0.16f));
            blade.transform.localRotation = Quaternion.Euler(10f, 0f, -20f);
            CreateMeshObject("Back_Shard_L", root, BuildCrystalMesh(0.20f, 1.15f), dark, new Vector3(-0.36f, 1.82f, -0.36f)).transform.localRotation = Quaternion.Euler(-25f, 0f, -20f);
            CreateMeshObject("Back_Shard_R", root, BuildCrystalMesh(0.20f, 1.15f), dark, new Vector3(0.36f, 1.82f, -0.36f)).transform.localRotation = Quaternion.Euler(-25f, 0f, 20f);
        }
        else
        {
            CreateMeshObject("Mage_Focus", root, BuildCrystalMesh(0.24f, 0.58f), energy, new Vector3(0.72f, 1.58f, 0.30f));
            CreateMeshObject("Moon_Mantle", root, BuildCapeMesh(1.10f, 1.45f, 0.28f), secondary, new Vector3(0f, 1.25f, -0.24f));
        }
    }

    private static void BuildEnergyCore(Transform root, Material energy, string id)
    {
        float height = id == "auron" || id == "kaelith" ? 1.72f : 1.62f;
        CreateMeshObject("Energy_Core", root, BuildUvSphereMesh(10, 8, 0.16f), energy, new Vector3(0f, height, 0.40f));
    }

    private static void BuildEarPair(Transform root, Material material, float height)
    {
        GameObject left = CreateMeshObject("Spirit_Ear_L", root, BuildCrystalMesh(0.16f, height), material, new Vector3(-0.22f, 2.72f, 0f));
        left.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
        GameObject right = CreateMeshObject("Spirit_Ear_R", root, BuildCrystalMesh(0.16f, height), material, new Vector3(0.22f, 2.72f, 0f));
        right.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
    }

    private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition, Vector3? localScale = null)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale ?? Vector3.one;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static Mesh BuildTaperedBodyMesh(int sides, float height, float bottomRadius, float topRadius)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int y = 0; y < 2; y++)
        {
            float radius = y == 0 ? bottomRadius : topRadius;
            float py = y == 0 ? -height * 0.5f : height * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, py, Mathf.Sin(a) * radius));
            }
        }
        for (int i = 0; i < sides; i++)
        {
            int n = (i + 1) % sides;
            triangles.Add(i); triangles.Add(sides + i); triangles.Add(sides + n);
            triangles.Add(i); triangles.Add(sides + n); triangles.Add(n);
        }
        return FinalizeMesh("TaperedBody", vertices, triangles);
    }

    private static Mesh BuildUvSphereMesh(int segments, int rings, float radius)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int r = 0; r <= rings; r++)
        {
            float v = r / (float)rings;
            float phi = v * Mathf.PI;
            for (int s = 0; s <= segments; s++)
            {
                float u = s / (float)segments;
                float theta = u * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * radius);
            }
        }
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int a = r * (segments + 1) + s;
                int b = a + segments + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
        }
        return FinalizeMesh("UvSphere", vertices, triangles);
    }

    private static Mesh BuildBladeMesh(float width, float length, float thickness)
    {
        Vector3[] v =
        {
            new Vector3(-width,-length*0.5f,-thickness), new Vector3(width,-length*0.5f,-thickness),
            new Vector3(-width,length*0.35f,-thickness), new Vector3(0f,length*0.5f,-thickness), new Vector3(width,length*0.35f,-thickness),
            new Vector3(-width,-length*0.5f,thickness), new Vector3(width,-length*0.5f,thickness),
            new Vector3(-width,length*0.35f,thickness), new Vector3(0f,length*0.5f,thickness), new Vector3(width,length*0.35f,thickness)
        };
        int[] t =
        {
            0,2,1, 1,2,4, 2,3,4,
            5,6,7, 6,9,7, 7,9,8,
            0,5,2, 2,5,7, 1,4,6, 4,9,6,
            2,7,3, 3,7,8, 3,8,4, 4,8,9, 0,1,5, 1,6,5
        };
        Mesh mesh = new Mesh { name = "Blade" }; mesh.vertices = v; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh BuildCrystalMesh(float radius, float height)
    {
        List<Vector3> v = new List<Vector3>();
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            v.Add(new Vector3(Mathf.Cos(a) * radius, -height * 0.28f, Mathf.Sin(a) * radius));
            v.Add(new Vector3(Mathf.Cos(a) * radius * 0.72f, height * 0.20f, Mathf.Sin(a) * radius * 0.72f));
        }
        v.Add(new Vector3(0f, height * 0.5f, 0f));
        v.Add(new Vector3(0f, -height * 0.5f, 0f));
        List<int> t = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            int n = (i + 1) % 6;
            t.Add(i*2); t.Add(n*2); t.Add(i*2+1);
            t.Add(i*2+1); t.Add(n*2); t.Add(n*2+1);
            t.Add(i*2+1); t.Add(n*2+1); t.Add(12);
            t.Add(n*2); t.Add(i*2); t.Add(13);
        }
        return FinalizeMesh("Crystal", v, t);
    }

    private static Mesh BuildDiscMesh(int sides, float radius, float halfThickness)
    {
        List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
        for (int z = 0; z < 2; z++)
        {
            float pz = z == 0 ? -halfThickness : halfThickness;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                v.Add(new Vector3(Mathf.Cos(a)*radius, Mathf.Sin(a)*radius, pz));
            }
        }
        for (int i = 0; i < sides; i++)
        {
            int n=(i+1)%sides; t.Add(i);t.Add(sides+i);t.Add(sides+n); t.Add(i);t.Add(sides+n);t.Add(n);
        }
        return FinalizeMesh("Disc",v,t);
    }

    private static Mesh BuildArcRibbonMesh(int segments, float radius, float width, float degrees)
    {
        List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
        float start = -degrees * 0.5f * Mathf.Deg2Rad;
        float end = degrees * 0.5f * Mathf.Deg2Rad;
        for(int i=0;i<=segments;i++)
        {
            float a=Mathf.Lerp(start,end,i/(float)segments);
            Vector3 dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0f);
            v.Add(dir*(radius-width)); v.Add(dir*(radius+width));
        }
        for(int i=0;i<segments;i++)
        {
            int b=i*2; t.Add(b);t.Add(b+2);t.Add(b+1); t.Add(b+1);t.Add(b+2);t.Add(b+3);
        }
        return FinalizeMesh("ArcRibbon",v,t);
    }

    private static Mesh BuildCurvedTailMesh(int segments, float length, float radius)
    {
        List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
        for(int i=0;i<=segments;i++)
        {
            float u=i/(float)segments;
            Vector3 center=new Vector3(Mathf.Sin(u*Mathf.PI)*0.35f,u*length,-u*u*0.45f);
            float r=Mathf.Lerp(radius,0.025f,u);
            v.Add(center+Vector3.right*r); v.Add(center-Vector3.right*r); v.Add(center+Vector3.forward*r); v.Add(center-Vector3.forward*r);
        }
        for(int i=0;i<segments;i++)
        {
            int a=i*4,b=(i+1)*4;
            for(int s=0;s<4;s++){int n=(s+1)%4;t.Add(a+s);t.Add(b+s);t.Add(a+n);t.Add(a+n);t.Add(b+s);t.Add(b+n);} 
        }
        return FinalizeMesh("CurvedTail",v,t);
    }

    private static Mesh BuildCapeMesh(float width, float height, float depth)
    {
        Vector3[] v={new Vector3(-width*0.5f,height*0.5f,0f),new Vector3(width*0.5f,height*0.5f,0f),new Vector3(width*0.36f,-height*0.5f,-depth),new Vector3(-width*0.36f,-height*0.5f,-depth)};
        int[] t={0,1,2,0,2,3,2,1,0,3,2,0}; Mesh m=new Mesh{name="Cape"};m.vertices=v;m.triangles=t;m.RecalculateNormals();m.RecalculateBounds();return m;
    }

    private static Mesh BuildWedgeMesh(float width, float height, float depth)
    {
        Vector3[] v={new Vector3(-width,0f,-depth),new Vector3(width,0f,-depth),new Vector3(-width,0f,depth),new Vector3(width,0f,depth),new Vector3(0f,height,0f)};
        int[] t={0,1,4,1,3,4,3,2,4,2,0,4,0,2,1,1,2,3};Mesh m=new Mesh{name="Wedge"};m.vertices=v;m.triangles=t;m.RecalculateNormals();m.RecalculateBounds();return m;
    }

    private static Mesh BuildTorusMesh(int majorSegments, int minorSegments, float majorRadius, float minorRadius)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();
        for(int i=0;i<=majorSegments;i++)
        {
            float a=i*Mathf.PI*2f/majorSegments;
            Vector3 center=new Vector3(Mathf.Cos(a)*majorRadius,0f,Mathf.Sin(a)*majorRadius);
            for(int j=0;j<=minorSegments;j++)
            {
                float b=j*Mathf.PI*2f/minorSegments;
                v.Add(center+new Vector3(Mathf.Cos(a)*Mathf.Cos(b)*minorRadius,Mathf.Sin(b)*minorRadius,Mathf.Sin(a)*Mathf.Cos(b)*minorRadius));
            }
        }
        int row=minorSegments+1;
        for(int i=0;i<majorSegments;i++)for(int j=0;j<minorSegments;j++){int a=i*row+j,b=(i+1)*row+j;t.Add(a);t.Add(b);t.Add(a+1);t.Add(a+1);t.Add(b);t.Add(b+1);} 
        return FinalizeMesh("Torus",v,t);
    }

    private static Mesh FinalizeMesh(string name, List<Vector3> vertices, List<int> triangles)
    {
        Mesh mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }

    private static Material CreateLitMaterial(string name, Color color, float smoothness, float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard");
        Material mat=new Material(shader){name=name,color=color};
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);
        if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);
        if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);
        return mat;
    }

    private static Material CreateEmissionMaterial(string name, Color color, float intensity)
    {
        Material mat=CreateLitMaterial(name,color,0.48f,0.15f);
        if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*intensity);}return mat;
    }

    private static Color BodyColor(string id, Color accent)
    {
        if(id=="auron")return new Color(0.16f,0.10f,0.04f);
        if(id=="pyrelle")return new Color(0.20f,0.025f,0.012f);
        if(id=="selene")return new Color(0.025f,0.065f,0.16f);
        if(id=="nyra")return new Color(0.16f,0.025f,0.13f);
        if(id=="vesper")return new Color(0.018f,0.085f,0.12f);
        if(id=="kaelith")return new Color(0.055f,0.028f,0.10f);
        return new Color(0.06f,0.035f,0.10f);
    }

    private static Color SecondaryColor(string id, Color accent)
    {
        return Color.Lerp(BodyColor(id,accent),accent,0.42f);
    }
}
