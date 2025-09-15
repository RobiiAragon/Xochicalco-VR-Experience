using UnityEngine;
using UnityEngine.Video;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Create a RenderTexture for rendering video to a target renderer.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayerRenderTexture : MonoBehaviour
    {
        const string k_ShaderName = "Unlit/Texture";

        [SerializeField]
        [Tooltip("The target Renderer which will display the video.")]
        Renderer m_Renderer;

        [SerializeField]
        [Tooltip("The width of the RenderTexture which will be created.")]
        int m_RenderTextureWidth = 1920;

        [SerializeField]
        [Tooltip("The height of the RenderTexture which will be created.")]
        int m_RenderTextureHeight = 1080;

        [SerializeField]
        [Tooltip("The bit depth of the depth channel for the RenderTexture which will be created.")]
        int m_RenderTextureDepth;

        RenderTexture _rt;
        Material _mat;

        void Start()
        {
            _rt = new RenderTexture(m_RenderTextureWidth, m_RenderTextureHeight, m_RenderTextureDepth);
            _rt.Create();
            _mat = new Material(Shader.Find(k_ShaderName));
            _mat.mainTexture = _rt;
            var vp = GetComponent<VideoPlayer>();
            vp.targetTexture = _rt;
            if (m_Renderer != null)
                m_Renderer.material = _mat;
        }

        void OnDestroy()
        {
            if (_rt != null)
            {
                if (_rt.IsCreated()) _rt.Release();
#if UNITY_EDITOR
                DestroyImmediate(_rt);
#else
                Destroy(_rt);
#endif
            }
            if (_mat != null)
#if UNITY_EDITOR
                DestroyImmediate(_mat);
#else
                Destroy(_mat);
#endif
        }
    }
}
