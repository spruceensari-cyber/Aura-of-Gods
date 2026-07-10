using System.Collections.Generic;
using UnityEngine;

public static class AOGNeutralCreatureModelFactory
{
    public static Color AccentFor(AOGNeutralMonsterType type)
    {
        return type switch
        {
            AOGNeutralMonsterType.SpiritWolf => new Color(0.20f,0.72f,1f),
            AOGNeutralMonsterType.EmberRaptor => new Color(1f,0.25f,0.04f),
            AOGNeutralMonsterType.StoneGuardian => new Color(0.72f,0.62f,0.42f),
            AOGNeutralMonsterType.AetherSentinel => new Color(0.10f,0.82f,1f),
            _ => new Color(1f,0.10f,0.03f)
        };
    }

    public static void BuildMonster(Transform parent, AOGNeutralMonsterType type, Color accent, bool leader)
    {
        if (parent == null)
            return;

        GameObject root = new GameObject("NeutralCreatureVisual");
        root.transform.SetParent(parent, false);
        float scale = leader ? 1.18f : 0.86f;
        root.transform.localScale = Vector3.one * scale;

        Material body = Lit("NeutralBody_"+type, BodyColor(type), 0.30f, type == AOGNeutralMonsterType.StoneGuardian ? 0.25f : 0.06f);
        Material dark = Lit("NeutralDark_"+type, new Color(0.02f,0.025f,0.032f),0.35f,0.16f);
        Material energy = Emission("NeutralEnergy_"+type,accent,4.0f);

        switch (type)
        {
            case AOGNeutralMonsterType.SpiritWolf:
                BuildWolf(root.transform,body,dark,energy);
                break;
            case AOGNeutralMonsterType.EmberRaptor:
                BuildRaptor(root.transform,body,dark,energy);
                break;
            case AOGNeutralMonsterType.StoneGuardian:
                BuildStone(root.transform,body,dark,energy);
                break;
            case AOGNeutralMonsterType.AetherSentinel:
                BuildSentinel(root.transform,body,dark,energy);
                break;
            default:
                BuildBrute(root.transform,body,dark,energy);
                break;
        }
    }

    private static void BuildWolf(Transform root, Material body, Material dark, Material energy)
    {
        Create("Wolf_Body",root,BuildEllipsoid(14,8,new Vector3(0.78f,0.48f,1.12f)),body,new Vector3(0f,1.05f,0f));
        Create("Wolf_Chest",root,BuildEllipsoid(12,7,new Vector3(0.58f,0.64f,0.56f)),body,new Vector3(0f,1.25f,0.48f));
        Create("Wolf_Head",root,BuildWedgeHead(0.46f,0.40f,0.68f),dark,new Vector3(0f,1.48f,0.98f));
        Create("Wolf_Ear_L",root,BuildSpike(0.12f,0.44f),energy,new Vector3(-0.22f,1.88f,0.88f)).transform.localRotation=Quaternion.Euler(-8f,0f,-12f);
        Create("Wolf_Ear_R",root,BuildSpike(0.12f,0.44f),energy,new Vector3(0.22f,1.88f,0.88f)).transform.localRotation=Quaternion.Euler(-8f,0f,12f);
        Create("Wolf_Tail",root,BuildCurvedTube(9,1.25f,0.16f,0.48f),energy,new Vector3(0f,1.16f,-0.95f)).transform.localRotation=Quaternion.Euler(-22f,0f,0f);
        for(int s=-1;s<=1;s+=2)
        {
            Create("ForeLeg_"+s,root,BuildTapered(7,0.86f,0.16f,0.10f),body,new Vector3(s*0.38f,0.55f,0.55f));
            Create("HindLeg_"+s,root,BuildTapered(7,0.92f,0.19f,0.11f),body,new Vector3(s*0.44f,0.55f,-0.55f));
        }
    }

    private static void BuildRaptor(Transform root, Material body, Material dark, Material energy)
    {
        Create("Raptor_Body",root,BuildEllipsoid(14,8,new Vector3(0.62f,0.70f,0.92f)),body,new Vector3(0f,1.20f,0f));
        GameObject neck=Create("Raptor_Neck",root,BuildTapered(8,1.10f,0.25f,0.16f),body,new Vector3(0f,1.72f,0.52f)); neck.transform.localRotation=Quaternion.Euler(-26f,0f,0f);
        Create("Raptor_Head",root,BuildWedgeHead(0.38f,0.34f,0.72f),dark,new Vector3(0f,2.18f,0.98f));
        Create("Raptor_Beak",root,BuildSpike(0.18f,0.62f),energy,new Vector3(0f,2.12f,1.40f)).transform.localRotation=Quaternion.Euler(90f,0f,0f);
        for(int i=0;i<5;i++)
        {
            GameObject feather=Create("Flame_Feather_"+i,root,BuildSpike(0.10f,0.54f+i*0.06f),energy,new Vector3((i-2)*0.14f,2.45f,0.70f));
            feather.transform.localRotation=Quaternion.Euler(-20f,0f,(i-2)*8f);
        }
        for(int s=-1;s<=1;s+=2)
        {
            Create("Raptor_Leg_"+s,root,BuildTapered(7,1.0f,0.16f,0.08f),body,new Vector3(s*0.30f,0.52f,-0.05f));
            GameObject claw=Create("Raptor_Claw_"+s,root,BuildSpike(0.11f,0.48f),energy,new Vector3(s*0.30f,0.10f,0.30f));claw.transform.localRotation=Quaternion.Euler(72f,0f,0f);
        }
        Create("Raptor_Tail",root,BuildCurvedTube(10,1.5f,0.14f,0.34f),body,new Vector3(0f,1.24f,-0.75f)).transform.localRotation=Quaternion.Euler(-10f,180f,0f);
    }

    private static void BuildStone(Transform root, Material body, Material dark, Material energy)
    {
        Create("Stone_Core",root,BuildRockMesh(0.82f,1.0f),body,new Vector3(0f,1.15f,0f));
        Create("Stone_Head",root,BuildRockMesh(0.48f,0.62f),dark,new Vector3(0f,2.12f,0.18f));
        for(int s=-1;s<=1;s+=2)
        {
            GameObject shoulder=Create("Stone_Shoulder_"+s,root,BuildRockMesh(0.48f,0.56f),body,new Vector3(s*0.90f,1.55f,0f));
            shoulder.transform.localRotation=Quaternion.Euler(0f,0f,s*12f);
            Create("Stone_Arm_"+s,root,BuildTapered(8,1.1f,0.28f,0.22f),dark,new Vector3(s*1.05f,0.95f,0f)).transform.localRotation=Quaternion.Euler(0f,0f,s*12f);
        }
        Create("Stone_Crystal",root,BuildSpike(0.26f,0.86f),energy,new Vector3(0f,1.70f,-0.52f)).transform.localRotation=Quaternion.Euler(-24f,0f,0f);
        Create("Stone_Leg_L",root,BuildTapered(8,0.82f,0.30f,0.24f),body,new Vector3(-0.38f,0.40f,0f));
        Create("Stone_Leg_R",root,BuildTapered(8,0.82f,0.30f,0.24f),body,new Vector3(0.38f,0.40f,0f));
    }

    private static void BuildSentinel(Transform root, Material body, Material dark, Material energy)
    {
        Create("Sentinel_Root",root,BuildTapered(10,1.65f,0.72f,0.34f),body,new Vector3(0f,1.12f,0f));
        Create("Sentinel_Head",root,BuildCrystalCluster(0.62f,0.92f),energy,new Vector3(0f,2.28f,0f));
        Create("Sentinel_Halo",root,BuildTorus(28,8,0.80f,0.06f),energy,new Vector3(0f,2.42f,0f)).transform.localRotation=Quaternion.Euler(90f,0f,0f);
        for(int i=0;i<4;i++)
        {
            float a=i*90f;
            Vector3 p=Quaternion.Euler(0f,a,0f)*new Vector3(0f,1.48f,0.72f);
            GameObject wing=Create("Sentinel_Wing_"+i,root,BuildSpike(0.18f,1.10f),energy,p);wing.transform.localRotation=Quaternion.Euler(58f,a,0f);
        }
        Create("Sentinel_Core",root,BuildEllipsoid(12,8,new Vector3(0.28f,0.38f,0.28f)),energy,new Vector3(0f,1.48f,0.42f));
    }

    private static void BuildBrute(Transform root, Material body, Material dark, Material energy)
    {
        Create("Brute_Torso",root,BuildTapered(10,1.65f,0.84f,0.68f),body,new Vector3(0f,1.25f,0f));
        Create("Brute_Head",root,BuildRockMesh(0.50f,0.60f),dark,new Vector3(0f,2.30f,0.18f));
        for(int s=-1;s<=1;s+=2)
        {
            Create("Brute_Horn_"+s,root,BuildSpike(0.18f,0.78f),energy,new Vector3(s*0.34f,2.66f,0.08f)).transform.localRotation=Quaternion.Euler(0f,0f,s*42f);
            Create("Brute_Arm_"+s,root,BuildTapered(8,1.35f,0.34f,0.24f),body,new Vector3(s*1.05f,1.10f,0f)).transform.localRotation=Quaternion.Euler(0f,0f,s*10f);
            Create("Brute_Fist_"+s,root,BuildRockMesh(0.40f,0.50f),dark,new Vector3(s*1.14f,0.40f,0.06f));
        }
        Create("Brute_Core",root,BuildCrystalCluster(0.34f,0.62f),energy,new Vector3(0f,1.60f,0.52f));
        Create("Brute_Leg_L",root,BuildTapered(8,0.92f,0.34f,0.25f),body,new Vector3(-0.42f,0.46f,0f));
        Create("Brute_Leg_R",root,BuildTapered(8,0.92f,0.34f,0.25f),body,new Vector3(0.42f,0.46f,0f));
    }

    private static GameObject Create(string name, Transform parent, Mesh mesh, Material material, Vector3 pos)
    {
        GameObject go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
    }

    private static Mesh BuildTapered(int sides,float height,float bottom,float top)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();
        for(int y=0;y<2;y++){float r=y==0?bottom:top;float py=(y-0.5f)*height;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2f/sides;v.Add(new Vector3(Mathf.Cos(a)*r,py,Mathf.Sin(a)*r));}}
        for(int i=0;i<sides;i++){int n=(i+1)%sides;t.Add(i);t.Add(sides+i);t.Add(sides+n);t.Add(i);t.Add(sides+n);t.Add(n);}return Finish("Tapered",v,t);
    }

    private static Mesh BuildEllipsoid(int segments,int rings,Vector3 radii)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();
        for(int r=0;r<=rings;r++){float phi=r/(float)rings*Mathf.PI;for(int s=0;s<=segments;s++){float th=s/(float)segments*Mathf.PI*2f;v.Add(new Vector3(Mathf.Sin(phi)*Mathf.Cos(th)*radii.x,Mathf.Cos(phi)*radii.y,Mathf.Sin(phi)*Mathf.Sin(th)*radii.z));}}
        for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){int a=r*(segments+1)+s,b=a+segments+1;t.Add(a);t.Add(b);t.Add(a+1);t.Add(a+1);t.Add(b);t.Add(b+1);}return Finish("Ellipsoid",v,t);
    }

    private static Mesh BuildWedgeHead(float width,float height,float length)
    {
        Vector3[] v={new Vector3(-width,-height,-length*0.5f),new Vector3(width,-height,-length*0.5f),new Vector3(-width,height,-length*0.4f),new Vector3(width,height,-length*0.4f),new Vector3(0f,-height*0.3f,length*0.5f),new Vector3(0f,height*0.25f,length*0.45f)};
        int[] t={0,1,4,2,5,3,0,4,2,2,4,5,1,3,4,3,5,4,0,2,1,1,2,3};Mesh m=new Mesh{name="WedgeHead"};m.vertices=v;m.triangles=t;m.RecalculateNormals();m.RecalculateBounds();return m;
    }

    private static Mesh BuildSpike(float radius,float height)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();int sides=7;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2f/sides;v.Add(new Vector3(Mathf.Cos(a)*radius,-height*0.5f,Mathf.Sin(a)*radius));}v.Add(new Vector3(0f,height*0.5f,0f));int tip=sides;for(int i=0;i<sides;i++){int n=(i+1)%sides;t.Add(i);t.Add(n);t.Add(tip);}return Finish("Spike",v,t);
    }

    private static Mesh BuildCurvedTube(int segments,float length,float radius,float bend)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();int sides=6;for(int i=0;i<=segments;i++){float u=i/(float)segments;Vector3 c=new Vector3(Mathf.Sin(u*Mathf.PI)*bend,u*length,-u*u*0.45f);float rr=Mathf.Lerp(radius,0.025f,u);for(int s=0;s<sides;s++){float a=s*Mathf.PI*2f/sides;v.Add(c+new Vector3(Mathf.Cos(a)*rr,0f,Mathf.Sin(a)*rr));}}
        for(int i=0;i<segments;i++)for(int s=0;s<sides;s++){int n=(s+1)%sides,a=i*sides+s,b=(i+1)*sides+s,c=i*sides+n,d=(i+1)*sides+n;t.Add(a);t.Add(b);t.Add(c);t.Add(c);t.Add(b);t.Add(d);}return Finish("CurvedTube",v,t);
    }

    private static Mesh BuildRockMesh(float radius,float height)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();int sides=8;v.Add(new Vector3(0f,height*0.5f,0f));v.Add(new Vector3(0f,-height*0.5f,0f));for(int i=0;i<sides;i++){float a=i*Mathf.PI*2f/sides;float jitter=1f+Mathf.Sin(i*2.17f)*0.18f;v.Add(new Vector3(Mathf.Cos(a)*radius*jitter,Mathf.Sin(i*1.7f)*height*0.12f,Mathf.Sin(a)*radius*jitter));}
        for(int i=0;i<sides;i++){int n=(i+1)%sides;t.Add(0);t.Add(2+i);t.Add(2+n);t.Add(1);t.Add(2+n);t.Add(2+i);}return Finish("Rock",v,t);
    }

    private static Mesh BuildCrystalCluster(float radius,float height)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();int sides=6;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2f/sides;v.Add(new Vector3(Mathf.Cos(a)*radius,-height*0.3f,Mathf.Sin(a)*radius));v.Add(new Vector3(Mathf.Cos(a)*radius*0.7f,height*0.18f,Mathf.Sin(a)*radius*0.7f));}v.Add(new Vector3(0f,height*0.5f,0f));for(int i=0;i<sides;i++){int n=(i+1)%sides;t.Add(i*2);t.Add(n*2);t.Add(i*2+1);t.Add(i*2+1);t.Add(n*2);t.Add(n*2+1);t.Add(i*2+1);t.Add(n*2+1);t.Add(12);}return Finish("CrystalCluster",v,t);
    }

    private static Mesh BuildTorus(int major,int minor,float radius,float tube)
    {
        List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();for(int i=0;i<=major;i++){float a=i*Mathf.PI*2f/major;Vector3 c=new Vector3(Mathf.Cos(a)*radius,0f,Mathf.Sin(a)*radius);for(int j=0;j<=minor;j++){float b=j*Mathf.PI*2f/minor;v.Add(c+new Vector3(Mathf.Cos(a)*Mathf.Cos(b)*tube,Mathf.Sin(b)*tube,Mathf.Sin(a)*Mathf.Cos(b)*tube));}}
        int row=minor+1;for(int i=0;i<major;i++)for(int j=0;j<minor;j++){int a=i*row+j,b=(i+1)*row+j;t.Add(a);t.Add(b);t.Add(a+1);t.Add(a+1);t.Add(b);t.Add(b+1);}return Finish("Torus",v,t);
    }

    private static Mesh Finish(string name,List<Vector3> v,List<int> t){Mesh m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}

    private static Material Lit(string name,Color color,float smooth,float metal){Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");Material m=new Material(shader){name=name,color=color};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);return m;}
    private static Material Emission(string name,Color color,float intensity){Material m=Lit(name,color,0.42f,0.08f);if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*intensity);}return m;}
    private static Color BodyColor(AOGNeutralMonsterType type){return type switch{AOGNeutralMonsterType.SpiritWolf=>new Color(0.04f,0.10f,0.16f),AOGNeutralMonsterType.EmberRaptor=>new Color(0.20f,0.045f,0.02f),AOGNeutralMonsterType.StoneGuardian=>new Color(0.20f,0.18f,0.14f),AOGNeutralMonsterType.AetherSentinel=>new Color(0.035f,0.12f,0.18f),_=>new Color(0.20f,0.035f,0.02f)};}
}
