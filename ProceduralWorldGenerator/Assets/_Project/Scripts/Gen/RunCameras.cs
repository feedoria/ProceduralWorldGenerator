using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

// two pictures of the terrain: top.png (straight down, like a map) and oblique.png (angled view)
// uses a temporary hidden camera, so the scene camera stays untouched
public static class RunCameras
{
    public static void Capture(Terrain terrain, string outDir)
    {
        var go = new GameObject("GenCaptureCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        cam.enabled = false;   // renders only when we ask

        try
        {
            Vector3 size = terrain.terrainData.size;
            Vector3 center = terrain.transform.position + new Vector3(size.x / 2f, 0f, size.z / 2f);
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = (size.x + size.y + size.z) * 4f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // top: orthographic, the whole terrain fits exactly
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(size.x, size.z) / 2f;
            cam.transform.position = center + Vector3.up * (size.y + 100f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Save(cam, 1024, 1024, Path.Combine(outDir, "top.png"));

            // oblique: from the south-west, 35 degrees down
            cam.orthographic = false;
            cam.fieldOfView = 40f;
            Vector3 target = center + Vector3.up * size.y * 0.15f;
            float diag = Mathf.Sqrt(size.x * size.x + size.z * size.z);
            float dist = diag * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.75f;  // 0.75 = a bit closer
            Vector3 dir = Quaternion.Euler(35f, 45f, 0f) * Vector3.forward;   // looks north-east and down
            cam.transform.position = target - dir * dist;
            cam.transform.LookAt(target);
            Save(cam, 1600, 900, Path.Combine(outDir, "oblique.png"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static void Save(Camera cam, int w, int h, string path)
    {
        var rt = new RenderTexture(w, h, 24);
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        var prev = RenderTexture.active;
        try
        {
            cam.aspect = (float)w / h;

            // URP way of rendering a camera into a texture on demand, old way as fallback
            var req = new RenderPipeline.StandardRequest();
            if (RenderPipeline.SupportsRenderRequest(cam, req))
            {
                req.destination = rt;
                RenderPipeline.SubmitRenderRequest(cam, req);
            }
            else
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
            }

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = prev;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
