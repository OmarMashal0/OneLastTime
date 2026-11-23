Shader "Level3 Bamboo/River Scenic Layer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _Opening ("Left Fade Start/End, Right Fade Start/End", Vector) = (0,0,0,0)
        _DistanceColor ("Distance Color", Color) = (0.4,0.55,0.58,1)
        _PaletteBlend ("Palette Blend", Range(0,1)) = 0
        _SoilFade ("Soil Fade Bottom / Top", Vector) = (0,0,0,0)
        _EdgeSoftness ("Horizontal Texture Edge Fade", Range(0,0.25)) = 0
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
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnitySprites.cginc"
            float4 _Opening;
            float4 _DistanceColor;
            float _PaletteBlend;
            float4 _SoilFade;
            float _EdgeSoftness;

            struct ScenicVertex
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 world : TEXCOORD1;
            };

            ScenicVertex Vertex(appdata_t input)
            {
                ScenicVertex o;
                v2f sprite = SpriteVert(input);
                o.vertex = sprite.vertex;
                o.color = sprite.color;
                o.uv = sprite.texcoord;
                o.world = mul(unity_ObjectToWorld, UnityFlipSprite(input.vertex.xyz, _Flip)).xy;
                return o;
            }

            fixed4 Fragment(ScenicVertex input) : SV_Target
            {
                fixed4 sprite = SampleSpriteTexture(input.uv);
                float luminance = dot(sprite.rgb, float3(0.299,0.587,0.114));
                sprite.rgb = lerp(sprite.rgb, _DistanceColor.rgb * (0.45 + luminance), _PaletteBlend);
                float left = _Opening.y > _Opening.x ? 1 - smoothstep(_Opening.x,_Opening.y,input.world.x) : 0;
                float right = _Opening.w > _Opening.z ? smoothstep(_Opening.z,_Opening.w,input.world.x) : 0;
                float forest = (_Opening.y > _Opening.x || _Opening.w > _Opening.z) ? saturate(left + right) : 1;
                sprite *= input.color;
                if (_SoilFade.y > _SoilFade.x)
                {
                    float coverage = forest * smoothstep(_SoilFade.x, _SoilFade.y, input.world.y);
                    float2 pixel = floor(input.world * 17.27);
                    float grain = frac(0.7548777 * pixel.x + 0.5698403 * pixel.y);
                    forest = step(grain + 0.0001, coverage);
                }
                if (_EdgeSoftness > 0)
                    forest *= smoothstep(0, _EdgeSoftness, min(input.uv.x, 1 - input.uv.x));
                sprite.a *= forest;
                sprite.rgb *= sprite.a;
                return sprite;
            }
            ENDCG
        }
    }
}

