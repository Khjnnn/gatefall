Shader "Gatefall/CombatFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Flash ("White flash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            half _Flash;
            fixed4 frag(v2f v) : SV_Target {
                fixed4 tex = SampleSpriteTexture(v.texcoord);
                fixed4 result = fixed4(lerp(tex.rgb, fixed3(1,1,1), saturate(_Flash)) * v.color.rgb, tex.a * v.color.a);
                result.rgb *= result.a;
                return result;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
