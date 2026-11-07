using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor.Build;
using UnityEngine;

namespace Gatefall.Editor
{
    public static class UrbanWardAssetAudit
    {
        [Serializable] private sealed class Row { public string key,sha256;public int width,height;public float worldHeight,pivotY; }
        [Serializable] private sealed class Report { public string version;public Row[] assets;public string scope="Source alpha/hash, imported texture budget and runtime sprite scale/pivot/cache. Native play review remains separate."; }
        public static void Validate()
        {
            const string folder="Assets/Resources/Art/UrbanWard/";
            var manifest=JsonUtility.FromJson<GatefallArt.WardManifest>(File.ReadAllText(folder+"manifest.json"));
            if(manifest.assets.Length!=5)throw new BuildFailedException("Urban ward asset count must be5.");
            var art=new GatefallArt();var rows=new List<Row>();
            try {
                foreach(var entry in manifest.assets) {
                    byte[] bytes=File.ReadAllBytes(folder+entry.key+".png");
                    string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
                    if(hash!=entry.sha256)throw new BuildFailedException("Urban ward source changed: "+entry.key);
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try {
                        if(!source.LoadImage(bytes) || source.width!=entry.sourceWidth || source.height!=entry.sourceHeight)
                            throw new BuildFailedException("Urban ward source dimensions: "+entry.key);
                        int transparent=0,opaque=0;
                        // Generated antialiased PNGs may contain1/255 corner noise and254/255 interiors.
                        foreach(Color32 pixel in source.GetPixels32()){if(pixel.a==0)transparent++;if(pixel.a>=250)opaque++;}
                        if(transparent<source.width*source.height/5 || opaque<100 || source.GetPixel(0,0).a>2f/255)
                            throw new BuildFailedException("Urban ward transparent cutout required: "+entry.key);
                    } finally {UnityEngine.Object.DestroyImmediate(source);}
                    Texture2D imported=art.Texture(entry.key);Sprite sprite=art.Sprite(entry.key);
                    if(!art.HasUrbanWard(entry.key) || Mathf.Max(imported.width,imported.height)>512 ||
                        Mathf.Abs(sprite.bounds.size.y-entry.heightWorld)>.002f ||
                        Mathf.Abs(sprite.pivot.y/imported.height-entry.pivotY)>.002f || !ReferenceEquals(sprite,art.Sprite(entry.key)))
                        throw new BuildFailedException("Urban ward runtime import/scale/cache failed: "+entry.key);
                    if(entry.key.StartsWith("tower-")) {
                        float visible=(entry.alphaBounds[3]-entry.alphaBounds[1])*entry.heightWorld/entry.sourceHeight;
                        if(Mathf.Abs(visible-3)>.01f || entry.pivotY<.10f || entry.pivotY>.23f)
                            throw new BuildFailedException("Urban ward tower ground scale failed: "+entry.key);
                    }
                    rows.Add(new Row{key=entry.key,sha256=hash,width=imported.width,height=imported.height,worldHeight=sprite.bounds.size.y,pivotY=entry.pivotY});
                }
                Directory.CreateDirectory("TestResults/chapter2-design-20261107");
                File.WriteAllText("TestResults/chapter2-design-20261107/unity-assets-audit.json",JsonUtility.ToJson(new Report{version=Application.version,assets=rows.ToArray()},true));
                Debug.Log("GATEFALL URBAN WARD PASSED:5 original alpha assets,512px import budget, stable world scale/pivots and sprite cache.");
            } finally { art.Dispose(); }
        }
    }
}
