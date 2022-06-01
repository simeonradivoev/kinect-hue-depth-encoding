using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityTemplateProjects
{
    /// <summary>
    /// Updates the point cloud shader from an active <see cref="ITextureDataProvider"/> that provides a Hue Encoded Depth Texture.
    /// </summary>
    public class KinectVideoPlay : MonoBehaviour
    {
        [SerializeField] [ColorUsage(true,true)]private Color _color;
        [SerializeField] [Range(0, 1)] private float _pointPositionRandomness;
        [SerializeField] private float _pointSize;
        [SerializeField] [Range(0, 1)] private float _pointSizeRandomness;
        private Vector2 _RayScale;
        [SerializeField] private Material m_material;
        [SerializeField] private RawImage m_rawImage;
        [SerializeField] private Vector3 m_size;
        [SerializeField] private bool m_useRaw;

        private ITextureDataProvider[] m_textureDataProviders;
        private Material m_materialInstance;
        private int m_activeProviderIndex;

        private ITextureDataProvider ActiveProvider => m_textureDataProviders?[m_activeProviderIndex];

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;   // kinect v1 only supports 30 fps
            
            m_textureDataProviders = GetComponentsInChildren<ITextureDataProvider>().Where(p => p.IsEnabled).ToArray();
            var scale = Mathf.Tan(KinectWrapper.Constants.NuiDepthHorizontalFOV * Mathf.Deg2Rad * 0.5f) /
                        (KinectWrapper.Constants.DepthImageWidth * 0.5f);
            _RayScale = new Vector2(scale, -scale);
            m_materialInstance = new Material(m_material);
            UpdateProvider(m_textureDataProviders.First(),null);
        }

        private void UpdateProvider(ITextureDataProvider newProvider,ITextureDataProvider oldProvider)
        {
            if (oldProvider != null)
            {
                oldProvider.SetActive(false);
            }

            if (newProvider != null)
            {
                newProvider.SetActive(true);
            }
            
            if (newProvider?.FlipY ?? false)
            {
                m_materialInstance.EnableKeyword("FLIP_Y");
            }
            else
            {
                m_materialInstance.DisableKeyword("FLIP_Y");
            }
        }

        private void OnDestroy()
        {
            Destroy(m_materialInstance);
        }

        private void Update()
        {
            m_rawImage.texture = ActiveProvider?.Texture;

            if (Input.GetKeyDown(KeyCode.Tab) && m_textureDataProviders.Length > 0)
            {
                var lastProvider = ActiveProvider;
                m_activeProviderIndex++;
                m_activeProviderIndex %= m_textureDataProviders.Length;
                UpdateProvider(ActiveProvider,lastProvider);
            }
        }

        private void OnDrawGizmos()
        {
            if (ActiveProvider != null)
            {
                var range = ActiveProvider.Range;
                var angle = ActiveProvider.Angle;

                Gizmos.matrix = transform.localToWorldMatrix *
                                Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.left));

                Gizmos.DrawFrustum(Vector3.zero, KinectWrapper.Constants.NuiDepthVerticalFOV,
                    m_size.z * range.x, m_size.z * range.y,
                    KinectWrapper.Constants.DepthImageWidth / (float) KinectWrapper.Constants.DepthImageHeight);
            }
        }

        private void OnRenderObject()
        {
            var texture = ActiveProvider?.Texture ? ActiveProvider?.Texture : Texture2D.blackTexture;
            var angle = ActiveProvider?.Angle ?? 0;
            var range = ActiveProvider?.Range ?? new Vector2(0, 1);

            m_materialInstance.SetPass(0);

            Shader.SetGlobalTexture("_DepthMap", texture);
            Shader.SetGlobalInt("_UseRaw", m_useRaw ? 1 : 0);
            Shader.SetGlobalMatrix("_Transform",
                transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.left)));
            Shader.SetGlobalVector("_CutOff", range);
            Shader.SetGlobalVector("_Size", m_size);
            Shader.SetGlobalVector("_RayScale", _RayScale);
            Shader.SetGlobalVector("_Resolution",
                new Vector4(texture.width, texture.height));
            Shader.SetGlobalFloat("_PointSize", _pointSize);
            Shader.SetGlobalFloat("_PointSizeRandomness", _pointSizeRandomness);
            Shader.SetGlobalFloat("_PointPositionRandomness", _pointPositionRandomness);
            Shader.SetGlobalColor("_Color", _color);
            Graphics.DrawProceduralNow(MeshTopology.Points, texture.width * texture.height);
        }
    }
}