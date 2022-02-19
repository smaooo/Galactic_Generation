using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// https://stackoverflow.com/questions/13055391/get-indexes-of-all-matching-values-from-list-using-linq

public class Visualization : MonoBehaviour
{

    private float unitLength = 1f;
    [SerializeField]
    private Material material;

    public List<Vector3> verts = new List<Vector3>();

    void Start()
    {
        List<Matrix4x4> poses = new List<Matrix4x4>();
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                poses.Add(Matrix4x4.TRS(new Vector3(i, Random.Range(-0.5f, 0.5f), j), Quaternion.identity, Vector3.one));

            }
        }
        //poses.Add(Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, Vector3.one));
        //poses.Add(Matrix4x4.TRS(new Vector3(0, 0, 1f), Quaternion.identity, Vector3.one));
        //poses.Add(Matrix4x4.TRS(new Vector3(0, 0, 2f), Quaternion.identity, Vector3.one));
        CreateMesh(poses);
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
    void CreateMesh(List<Matrix4x4> positions)
    {
        HashSet<Vector3> vertices = new HashSet<Vector3>();
        List<int> triangles = new List<int>();
        Vector3 size = Vector3.one * unitLength/2;
        for (int i = 0; i < positions.Count; i++)
        {
            IEnumerable<Vector3> m = null;
            var p = positions[i];
            List<Vector3> tmpVerts = new List<Vector3>();
            Vector3 curPos = p.GetColumn(3);
            Vector3 curScale = new Vector3(p.GetColumn(0).magnitude, p.GetColumn(1).magnitude, p.GetColumn(2).magnitude);
            Vector3 offset = new Vector3(size.x / curScale.x, size.y / curScale.y, size.z / curScale.z);
            m = vertices.Where(v => v.x == curPos.x + offset.x && v.z == curPos.z + offset.z);
            m.ToList().Add(new Vector3(0,curPos.y + offset.y,0));
            vertices.Add(new Vector3(curPos.x + offset.x, m.Count() > 0 ? m.Average(v => v.y) : curPos.y + offset.y, curPos.z + offset.z));
            m = vertices.Where(v => v.x == curPos.x - offset.x && v.z == curPos.z + offset.z);
            m.ToList().Add(new Vector3(0,curPos.y + offset.y,0));
            vertices.Add(new Vector3(curPos.x - offset.x, m.Count() > 0 ? m.Average(v => v.y) : curPos.y + offset.y, curPos.z + offset.z));
            m = vertices.Where(v => v.x == curPos.x + offset.x && v.z == curPos.z - offset.z);
            m.ToList().Add(new Vector3(0,curPos.y + offset.y,0));
            vertices.Add(new Vector3(curPos.x + offset.x, m.Count() > 0 ? m.Average(v => v.y) : curPos.y + offset.y, curPos.z - offset.z));
            m = vertices.Where(v => v.x == curPos.x - offset.x && v.z == curPos.z + offset.z);
            m.ToList().Add(new Vector3(0,curPos.y + offset.y,0));
            vertices.Add(new Vector3(curPos.x - offset.x, m.Count() > 0 ? m.Average(v => v.y) : curPos.y + offset.y, curPos.z - offset.z));
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

        foreach(var v in vertices)
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


        //List<Triangle> tris = Delaunay.TriangulateByFlippingEdges(vertices.ToList());
        //List<Vertex> vs = new List<Vertex>();
        //foreach (var v in vertices)
        //{
        //    vs.Add(new Vertex(v));
        //}
        //List<Triangle> tris = IncrementalTriangulationAlgorithm.TriangulatePoints(vs);
        //foreach (var t in tris)
        //{
        //    triangles.Add(vertices.ToList().IndexOf(t.v1.position));
        //    triangles.Add(vertices.ToList().IndexOf(t.v2.position));
        //    triangles.Add(vertices.ToList().IndexOf(t.v3.position));
        //}
        //print(vertices.Count);
        ////foreach (var t in triangles)
        ////{
        ////    print(t);
        ////}
        //print(tris.Count);
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        GameObject obj = new GameObject();
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = material;

        verts = new List<Vector3>(vertices);
    }



}
