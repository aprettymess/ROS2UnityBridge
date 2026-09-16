using UnityEngine;
using UnityEngine.Rendering;

namespace WorkspaceMapper.Scripts
{
    public static class MaterialUtil
    {
        static Shader _unlit;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        static readonly int CullId = Shader.PropertyToID("_Cull");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");

        public static Shader UnlitShader
        {
            get
            {
                if (_unlit == null)
                {
                    string[] names = { "Universal Render Pipeline/Unlit", "Sprites/Default", "Unlit/Texture", "Unlit/Color" };
                    for (int i = 0; i < names.Length && _unlit == null; i++) _unlit = Shader.Find(names[i]);
                }
                return _unlit;
            }
        }

        static Shader _lit;
        public static Shader LitShader
        {
            get
            {
                if (_lit == null)
                {
                    string[] names = { "Universal Render Pipeline/Lit", "Standard" };
                    for (int i = 0; i < names.Length && _lit == null; i++) _lit = Shader.Find(names[i]);
                    if (_lit == null) _lit = UnlitShader;
                }
                return _lit;
            }
        }

        public static Material MakeUnlit() => new Material(UnlitShader);
        public static Material MakeLit() => new Material(LitShader);

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
            m.color = c;
        }

        public static void SetTexture(Material m, Texture tex)
        {
            if (m.HasProperty(BaseMapId)) m.SetTexture(BaseMapId, tex);
            if (m.HasProperty(MainTexId)) m.SetTexture(MainTexId, tex);
            m.mainTexture = tex;
        }

        public static void SetTransparent(Material m)
        {
            if (m.HasProperty(SurfaceId)) m.SetFloat(SurfaceId, 1f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (m.HasProperty(SrcBlendId)) m.SetInt(SrcBlendId, (int)BlendMode.SrcAlpha);
            if (m.HasProperty(DstBlendId)) m.SetInt(DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty(ZWriteId)) m.SetInt(ZWriteId, 0);
            if (m.HasProperty(CullId)) m.SetInt(CullId, 0);
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}