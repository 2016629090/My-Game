Shader "Custom/LoadingReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("加载进度", Range(0, 1)) = 0
        _Layer1End ("图层1结束", Range(0, 1)) = 0.3
        _Layer2End ("图层2结束", Range(0, 1)) = 0.6
        _Layer3End ("图层3结束", Range(0, 1)) = 0.9
        _Desaturate ("去色强度", Range(0, 1)) = 0.8
        _Contrast ("对比度", Range(0.5, 2)) = 1
        _Brightness ("亮度", Range(-0.5, 0.5)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            // 我们的自定义参数
            float _Progress;
            float _Layer1End;
            float _Layer2End;
            float _Layer3End;
            float _Desaturate;
            float _Contrast;
            float _Brightness;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            // 辅助函数
            float3 Desaturate(float3 color, float amount)
            {
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                return lerp(color, luminance.xxx, amount);
            }
            
            // 计算对比度
            float3 ApplyContrast(float3 color, float contrast)
            {
                return saturate((color - 0.5) * contrast + 0.5);
            }
            
            // 计算巨龙区域遮罩
            float GetDragonMask(float2 uv)
            {
                // 假设巨龙在画面右上方
                float2 dragonCenter = float2(0.7, 0.7);
                float dragonRadius = 0.3;
                
                float distanceToDragon = distance(uv, dragonCenter);
                return 1.0 - smoothstep(0.0, dragonRadius, distanceToDragon);
            }
            
            // 计算勇士区域遮罩
            float GetWarriorMask(float2 uv)
            {
                float2 warriorCenter = float2(0.2, 0.2);
                float warriorRadius = 0.15;
                
                float distanceToWarrior = distance(uv, warriorCenter);
                return 1.0 - smoothstep(0.0, warriorRadius, distanceToWarrior);
            }
            
            // 计算血月区域遮罩
            float GetBloodMoonMask(float2 uv)
            {
                float2 moonCenter = float2(0.8, 0.9);
                float moonRadius = 0.05;
                
                float distanceToMoon = distance(uv, moonCenter);
                return 1.0 - smoothstep(0.0, moonRadius, distanceToMoon);
            }
            
            // 主片段着色器
           // 修改整个结构，让过渡更平滑
fixed4 frag(v2f IN) : SV_Target
{
    // 基础纹理采样
    half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
    
    // 如果进度为0，完全透明
    if (_Progress <= 0.001)
    {
        color.a = 0;
        return color;
    }
    
    // 阶段1：灰度背景显现 (0-30%)
    if (_Progress < _Layer1End)
    {
        float progress = _Progress / _Layer1End;
        
        // 灰度化
        float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
        
        // 从黑到灰
        float3 grayColor = lerp(float3(0,0,0), float3(luminance, luminance, luminance), progress);
        
        return fixed4(grayColor, color.a * progress);
    }
    
    // 阶段2：剪影强化 (30-60%)
    else if (_Progress < _Layer2End)
    {
        float progress = (_Progress - _Layer1End) / (_Layer2End - _Layer1End);
        
        // 获取巨龙区域
        float dragonMask = GetDragonMask(IN.texcoord);
        
        // 基础灰度
        float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
        
        // 巨龙区域更暗，背景稍亮
        float dragonDarkness = 0.3; // 巨龙区域暗度
        float backgroundBrightness = 0.7; // 背景亮度
        
        // 混合
        float finalLuminance = lerp(luminance * backgroundBrightness, 
                                   luminance * dragonDarkness, 
                                   dragonMask);
        
        // 根据进度显现
        finalLuminance = lerp(0, finalLuminance, progress);
        
        return fixed4(finalLuminance, finalLuminance, finalLuminance, color.a);
    }
    
    // 阶段3：颜色和对比度恢复 (60-90%)
    else if (_Progress < _Layer3End)
    {
        float progress = (_Progress - _Layer2End) / (_Layer3End - _Layer2End);
        
        // 使用平滑曲线（缓入缓出）
        float smoothProgress = smoothstep(0, 1, progress);
        
        // 1. 颜色恢复
        float3 originalColor = color.rgb;
        float3 grayColor = Desaturate(originalColor, 1.0);
        
        // 从灰度渐变到彩色
        float3 colorMix = lerp(grayColor, originalColor, smoothProgress);
        
        // 2. 对比度恢复
        float contrastProgress = smoothProgress * smoothProgress; // 二次曲线，对比度恢复更快
        float currentContrast = lerp(0.7, _Contrast, contrastProgress);
        colorMix = ApplyContrast(colorMix, currentContrast);
        
        // 3. 亮度微调
        float brightnessProgress = progress; // 线性
        float currentBrightness = lerp(-0.1, _Brightness, brightnessProgress);
        colorMix = saturate(colorMix + currentBrightness);
        
        return fixed4(colorMix, color.a);
    }
    
    // 阶段4：最终微调 (90-100%)
    else
    {
        // 最终颜色已经是原色，这里可以加一些微妙的润色
        float progress = (_Progress - _Layer3End) / (1.0 - _Layer3End);
        
        // 轻微提升饱和度作为最终润色（可选）
        float3 finalColor = color.rgb;
        
        // 如果需要，可以在这里添加非常微妙的最终调整
        // 例如：轻微提升对比度
        if (progress > 0.5f)
        {
            finalColor = ApplyContrast(finalColor, 1.1f);
        }
        
        return fixed4(finalColor, color.a);
    }
}
            ENDCG
        }
    }
}