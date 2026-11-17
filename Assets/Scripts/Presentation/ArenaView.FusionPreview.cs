using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class ArenaView
    {
        readonly FusionPreview fusionPreview=new FusionPreview();
        readonly SpriteRenderer[] previewEdges=new SpriteRenderer[26];
        readonly Vector3[] previewVertices=new Vector3[26];
        SpriteRenderer previewStrip, previewSplash;
        Mesh previewFan;
        MeshRenderer previewFanRenderer;
        Material previewMaterial;
        float previewCone=-1;
        void InitializeFusionPreview()
        {
            previewStrip=Make("Fusion line footprint",square,Vector3.zero,Vector3.one,Color.white,140);
            previewSplash=Make("Fusion splash radius",art.Sprite("fx-range"),Vector3.zero,Vector3.one,Color.white,142);
            for(int i=0;i<previewEdges.Length;i++)previewEdges[i]=Make("Fusion shape edge",square,Vector3.zero,Vector3.one,Color.white,141);
            previewFan=new Mesh{name="Cached fusion fan"};owned.Add(previewFan);
            var triangles=new int[24*3];
            for(int i=0;i<24;i++){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
            previewFan.vertices=previewVertices;previewFan.triangles=triangles;
            previewMaterial=new Material(defaultEffectMaterial);owned.Add(previewMaterial);
            var fan=new GameObject("Fusion cone footprint");fan.transform.SetParent(root,false);
            fan.AddComponent<MeshFilter>().sharedMesh=previewFan;
            previewFanRenderer=fan.AddComponent<MeshRenderer>();previewFanRenderer.sharedMaterial=previewMaterial;previewFanRenderer.sortingOrder=140;
            HideFusionPreview();
        }
        void HideFusionPreview()
        {
            previewStrip.enabled=previewSplash.enabled=previewFanRenderer.enabled=false;
            for(int i=0;i<previewEdges.Length;i++)previewEdges[i].enabled=false;
        }
        void PreviewEdge(int index,Vector3 a,Vector3 b,Color color)
        {
            SpriteRenderer edge=previewEdges[index];Vector3 delta=b-a;
            edge.enabled=true;edge.color=color;edge.transform.position=(a+b)*.5f;
            edge.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            edge.transform.localScale=new Vector3(delta.magnitude,.045f,1);
        }
        void DrawFusionPreview(DefenseSimulation game,FusionRecipe recipe,TowerStats s,V2 origin)
        {
            V2 impact;float radius;V2 aim=FusionPreview.Aim(game,s,origin,out impact,out radius);
            Vector3 p=Point(origin), forward=Point(aim), side=new Vector3(-aim.Y,aim.X,0);
            Color role=GatefallArt.RoleColor(recipe.Result), rim=Alpha(role,.8f);
            bool line=s.LineHalfWidth>0, cone=!line && s.ConeCos>0;
            rangeRing.enabled=rangeFill.enabled=!line && !cone;
            rangeRing.color=rim;rangeFill.color=Alpha(role,.07f);
            if(line) {
                previewStrip.enabled=true;previewStrip.color=Alpha(role,.12f);
                previewStrip.transform.position=p+forward*(s.Range*.5f);
                previewStrip.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.Y,aim.X)*Mathf.Rad2Deg);
                previewStrip.transform.localScale=new Vector3(s.Range,s.LineHalfWidth*2,1);
                Vector3 end=p+forward*s.Range, width=side*s.LineHalfWidth;
                PreviewEdge(0,p-width,end-width,rim);PreviewEdge(1,end-width,end+width,rim);
                PreviewEdge(2,end+width,p+width,rim);PreviewEdge(3,p+width,p-width,rim);
            } else if(cone) {
                if(previewCone!=s.ConeCos) {
                    previewCone=s.ConeCos;float half=Mathf.Acos(Mathf.Clamp(s.ConeCos,-1,1));
                    for(int i=0;i<=24;i++){float angle=-half+2*half*i/24;previewVertices[i+1]=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);}
                    previewFan.vertices=previewVertices;previewFan.RecalculateBounds();
                }
                previewFanRenderer.enabled=true;previewMaterial.color=Alpha(role,.12f);
                Transform fan=previewFanRenderer.transform;fan.position=p;fan.localScale=new Vector3(s.Range,s.Range,1);
                fan.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.Y,aim.X)*Mathf.Rad2Deg);
                PreviewEdge(0,p,fan.TransformPoint(previewVertices[1]),rim);
                for(int i=1;i<=24;i++)PreviewEdge(i,fan.TransformPoint(previewVertices[i]),fan.TransformPoint(previewVertices[i+1]),rim);
                PreviewEdge(25,fan.TransformPoint(previewVertices[25]),p,rim);
            }
            // Radius is an impact footprint, distinct from the tower's acquisition range.
            if(!line && !cone && s.Radius>0) {
                previewSplash.enabled=true;previewSplash.color=rim;previewSplash.transform.position=Point(impact);
                previewSplash.transform.localScale=Vector3.one*(radius*2/RangeDiameter);
            }
        }
    }
}
