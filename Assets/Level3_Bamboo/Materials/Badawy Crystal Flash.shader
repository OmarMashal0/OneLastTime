Shader "Level3 Bamboo/Badawy Crystal Flash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _CrystalFlash ("Crystal Flash", Range(0, 1)) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            float _CrystalFlash;

            fixed4 Fragment(v2f input) : SV_Target
            {
                fixed4 sprite = SampleSpriteTexture(input.texcoord) * input.color;
                float crystal = saturate((max(sprite.b, sprite.r) - sprite.g) * 4)
                              * saturate((sprite.b - 0.20) * 4);
                sprite.rgb = lerp(sprite.rgb, float3(0.83, 0.68, 1), crystal * _CrystalFlash);
                sprite.rgb *= sprite.a;
                return sprite;
            }
            ENDCG
        }
    }
}

