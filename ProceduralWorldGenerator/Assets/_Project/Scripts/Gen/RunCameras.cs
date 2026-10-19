using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

// the two screenshots of every run come from fixed cameras, not from the camera i move around with
// so two runs can be compared because they're always framed the same way
public static class RunCameras
{
    public static void Capture(RunContext ctx)
    {
        TerrainData td = ctx.Data;
        Vector3 size = td.size;
        // the middle of the terrain at ground level
        Vector3 center = ctx.terrain.transform.position + new Vector3(size.x / 2f, 0, size.z / 2f);

        // a temporary hidden camera, it doesnt show in the hierarchy and doesnt get saved in the scene
        var go = new GameObject("RunCamera");
        go.hideFlags = HideFlags.HideAndDontSave;
        var cam = go.AddComponent<Camera>();
        cam.enabled = false; // it only renders when i ask it to
        try
        {
            // TOP: orthographic so there's no perspective, it looks like a map (north is up)
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(size.x, size.z) / 2f; // half of the terrain fits on each side
            cam.transform.position = center + Vector3.up * (size.y + 50f); // above the highest possible point
            cam.transform.rotation = Quaternion.Euler(90, 0, 0); // looking straight down
            cam.nearClipPlane = 1f;
            cam.farClipPlane = size.y + 200f;
            Render(cam, 1024, 1024, Path.Combine(ctx.outDir, "top.png"));

            // OBLIQUE: from the south-west corner looking down at ~35 deg, so you actually see the relief
            cam.orthographic = false;
            cam.fieldOfView = 40f;
            float diag = new Vector2(size.x, size.z).magnitude;
            // how far the camera has to be so the whole diagonal fits in the view (+10% margin)
            float dist = diag * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.1f;
            float elev = 35f * Mathf.Deg2Rad;
            // direction from the center towards the corner, lifted up by the elevation angle
            Vector3 dir = new Vector3(-0.7071f * Mathf.Cos(elev), Mathf.Sin(elev), -0.7071f * Mathf.Cos(elev));
            cam.transform.position = center + dir * dist;
            cam.transform.LookAt(center + Vector3.up * size.y * 0.15f); // a bit above ground so it's not cut off
            cam.nearClipPlane = 1f;
            cam.farClipPlane = dist + diag + size.y;
            Render(cam, 1600, 900, Path.Combine(ctx.outDir, "oblique.png"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static void Render(Camera cam, int w, int h, string path)
    {
        // the camera draws into this texture instead of the screen
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.aspect = (float)w / h;

        // this is how URP renders a camera on demand in unity 6, the old Render() is just a backup
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

        // copying the pixels from the gpu texture into a normal texture so i can save it as png
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }
}