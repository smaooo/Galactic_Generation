using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// https://stackoverflow.com/questions/13055391/get-indexes-of-all-matching-values-from-list-using-linq

public class Visualization : MonoBehaviour
{
    //private float unitLength = 1f;
    [SerializeField]
    private Material material;

    public static List<Vector3> verts = new List<Vector3>();

    void Start()
    {
        List<Matrix4x4> poses = new List<Matrix4x4>();
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                poses.Add(Matrix4x4.TRS(new Vector3(i, Random.Range(-0.5f, 0.5f), j), Quaternion.identity, Vector3.one * 1));

            }
        }
        //poses.Add(Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, Vector3.one));
        //poses.Add(Matrix4x4.TRS(new Vector3(0, 0, 1f), Quaternion.identity, Vector3.one));
        //poses.Add(Matrix4x4.TRS(new Vector3(0, 0, 2f), Quaternion.identity, Vector3.one));
        CreateMesh(poses, 1.0f, material);
    }
    void OnDrawGizmos()
    {
        if (verts.Count > 0)
        {
            foreach (var v in verts)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(v, 0.01f);


            }
        }
    }
    public static void CreateMesh(List<Matrix4x4> positions, float unitLength, Material material, Transform parent = null)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Vector3 size = Vector3.one * unitLength/2;
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 m = Vector3.zero;
            var p = positions[i];
            List<Vector3> tmpVerts = new List<Vector3>();
            Vector3 curPos = p.GetColumn(3);
            print(curPos);
            //Vector3 curScale = new Vector3(p.GetColumn(0).magnitude, p.GetColumn(1).magnitude, p.GetColumn(2).magnitude);
            Vector3 curScale = Vector3.one * 0.1f;
            Vector3 offset = new Vector3(size.x / curScale.x, size.y / curScale.y, size.z / curScale.z);
            m = vertices.ToList().Find(v => v.x == curPos.x + offset.x && v.z == curPos.z + offset.z);
            SwapMean(ref vertices, m, curPos, offset);

            m = vertices.ToList().Find(v => v.x == curPos.x - offset.x && v.z == curPos.z + offset.z);
            SwapMean(ref vertices, m, curPos, offset);
            
            m = vertices.ToList().Find(v => v.x == curPos.x + offset.x && v.z == curPos.z - offset.z);
            SwapMean(ref vertices, m, curPos, offset);
            
            m = vertices.ToList().Find(v => v.x == curPos.x - offset.x && v.z == curPos.z + offset.z);
            SwapMean(ref vertices, m, curPos, offset);
            //Vector3 minXminZ = tmpVerts.OrderBy(v => v.x).OrderBy(v => v.z).First();
            //Vector3 maxXmaxZ = tmpVerts.OrderByDescending(v => v.x).ThenByDescending(v => v.z).First();
            //Vector3 minXmaxZ = tmpVerts.OrderBy(v => v.x).ThenByDescending(v => v.z).First();
            //Vector3 maxXminZ = tmpVerts.OrderByDescending(v => v.x).ThenBy(v => v.z).First();

            //vertices.Add(minXminZ);
            //int fIndex = vertices.ToList().IndexOf(vertices.Last()) == 0 ? 0 : vertices.ToList().IndexOf(vertices.Last());
            //vertices.Add(minXmaxZ);
            //vertices.Add(maxXmaxZ);
            //triangles.Add(fIndex);
            //triangles.Add(positions.IndexOf(p) > 0 ? vertices.Count - 3 : vertices.Count - 2);
            //triangles.Add(vertices.Count - 1);
            //vertices.Add(maxXminZ);
            //triangles.Add(fIndex);
            //triangles.Add(vertices.Count - 2);
            //triangles.Add(vertices.Count - 1);

        }

        foreach (var v in vertices)
        {
            //print(v);
            float far = Mathf.Sqrt((unitLength * unitLength) * 2);
            Vector3 up = vertices.ToList().Find(x => x.x == v.x && x.z - v.z == 1.0f);
            Vector3 right = vertices.ToList().Find(x => x.z == v.z && x.x - v.x == 1.0f);

            Vector3 ortho = vertices.ToList().Find(x => x.x == v.x + 1.0f && x.z == v.z + 1.0f);
            if (up == Vector3.zero || right == Vector3.zero || ortho == Vector3.zero) continue;
            //print("up " + up);
            //print("right " + right);
            //print("ortho " + ortho);


            triangles.Add(vertices.ToList().IndexOf(v));
            triangles.Add(vertices.ToList().IndexOf(up));
            triangles.Add(vertices.ToList().IndexOf(ortho));
            triangles.Add(vertices.ToList().IndexOf(v));
            triangles.Add(vertices.ToList().IndexOf(ortho));
            triangles.Add(vertices.ToList().IndexOf(right));

        }


            
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.Optimize();
        //mesh.RecalculateNormals();
        //mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        GameObject obj = new GameObject();
        if (parent != null) obj.transform.SetParent(parent);

        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = material;
        print(vertices.Count);
        verts = new List<Vector3>(vertices);
    }

    private static void SwapMean(ref List<Vector3> verts, Vector3 meanPoint, Vector3 curPos, Vector3 offset)
    {
        if (meanPoint != Vector3.zero)
        {
            int index = verts.IndexOf(meanPoint);
            meanPoint.y = (meanPoint.y + curPos.y + offset.y) / 2;
            verts[index] = meanPoint;
        }
        else
        {
            Vector3 p = new Vector3(curPos.x + offset.x, curPos.y + offset.y, curPos.z + offset.z);
            //print(p);
            if (!verts.Contains(p))
            {
                verts.Add(p);

            }
        }
    }

}
