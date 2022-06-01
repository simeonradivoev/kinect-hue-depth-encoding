using System;
using UnityEngine;

namespace UnityTemplateProjects
{
    public class KinectTextureDataProvider : MonoBehaviour, ITexture2DDataProvider
    {
        public Texture2D Texture { get; private set; }
        Texture ITextureDataProvider.Texture => Texture;
        public Vector2 Range => m_range;
        public float Angle => m_sensorAngle;
        public bool FlipY => false;
        public bool IsEnabled => m_initialized;

        [SerializeField] private bool m_useRaw;
        [SerializeField] private bool m_nearMode;
        [SerializeField] private int m_sensorAngle;
        [SerializeField] private Vector2 m_range;

        private IntPtr m_depthStreamHandle;
        private IntPtr m_colorStreamHandle;
        private int m_depthMapSize;
        private ushort[] m_usersDepthMap;
        private ushort[] m_usersPrevState;
        private float[] m_usersHistogramMap = new float[8192];
        private byte[] m_videoBuffer;
        private Color32[] m_colorBuffer;
        private bool m_initialized;
        private Color32[] m_textureBuffer;
        private bool m_textureDirty;
        private bool m_isActive;

        private void OnEnable()
        {
            var hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesDepth |
                                                 KinectWrapper.NuiInitializeFlags.UsesColor);

            if (hr != 0)
            {
                throw new Exception("NuiInitialize Failed");
            }

            m_depthStreamHandle = IntPtr.Zero;

            hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.Depth,
                KinectWrapper.Constants.DepthImageResolution, 0, 2, IntPtr.Zero, ref m_depthStreamHandle);

            if (hr != 0)
            {
                throw new Exception("Cannot open depth stream");
            }

            m_colorStreamHandle = IntPtr.Zero;
            hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.Color,
                KinectWrapper.Constants.ColorImageResolution, 0, 2, IntPtr.Zero, ref m_colorStreamHandle);
            if (hr != 0)
            {
                throw new Exception("Cannot open color stream");
            }

            KinectWrapper.NuiCameraElevationGetAngle(out var currentAngle);
            if (currentAngle != m_sensorAngle)
            {
                KinectWrapper.NuiCameraElevationSetAngle(m_sensorAngle);
            }

            int depthWidth = KinectWrapper.GetDepthWidth();
            int depthHeight = KinectWrapper.GetDepthHeight();
            m_depthMapSize = depthWidth * depthHeight;
            Texture = new Texture2D(depthWidth, depthHeight, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Bilinear
            };
            m_textureBuffer = new Color32[m_depthMapSize];
            for (int i = 0; i < m_textureBuffer.Length; i++)
            {
                m_textureBuffer[i] = Color.black;
            }
            m_usersDepthMap = new ushort[m_depthMapSize];
            m_usersPrevState = new ushort[m_depthMapSize];

            int colorWidth = KinectWrapper.GetColorWidth();
            int colorHeight = KinectWrapper.GetColorHeight();
            m_videoBuffer = new byte[colorWidth * colorHeight];
            m_colorBuffer = new Color32[colorWidth * colorHeight];

            m_initialized = true;
        }

        private void UpdateData()
        {
            if (m_initialized)
            {
                if (m_depthStreamHandle != IntPtr.Zero &&
                    KinectWrapper.PollDepth(m_depthStreamHandle, m_nearMode, ref m_usersDepthMap))
                {
                    m_textureDirty = true;
                }

                if (m_colorStreamHandle != IntPtr.Zero &&
                    KinectWrapper.PollColor(m_colorStreamHandle, ref m_videoBuffer, ref m_colorBuffer))
                {
                    m_textureDirty = true;
                }

                if (m_textureDirty)
                {
                    UpdateDepthMap();
                    m_textureDirty = false;
                }
            }
        }

        private void Update()
        {
            if (m_isActive)
            {
                UpdateData();
            }
        }
        
        public void SetActive(bool isActive)
        {
            m_isActive = isActive;
        }

        private void UpdateDepthMap()
        {
            /*int numOfPoints = 0;

            Array.Clear(m_usersHistogramMap, 0, m_usersHistogramMap.Length);
            // Calculate cumulative histogram for depth
            for (int i = 0; i < m_usersDepthMap.Length; i++)
            {
                // Only calculate for depth that contains users
                if ((m_usersDepthMap[i] & 7) != 0)
                {
                    ushort userDepth = (ushort)(m_usersDepthMap[i] >> 3);
                    m_usersHistogramMap[userDepth]++;
                    numOfPoints++;
                }
            }

            if (numOfPoints > 0)
            {
                for (int i = 1; i < m_usersHistogramMap.Length; i++)
                {
                    m_usersHistogramMap[i] += m_usersHistogramMap[i - 1];
                }

                for (int i = 0; i < m_usersHistogramMap.Length; i++)
                {
                    m_usersHistogramMap[i] = 1.0f - (m_usersHistogramMap[i] / numOfPoints);
                }
            }*/

            // dummy structure needed by the coordinate mapper
            KinectWrapper.NuiImageViewArea pcViewArea = new KinectWrapper.NuiImageViewArea
            {
                eDigitalZoom = 0,
                lCenterX = 0,
                lCenterY = 0
            };

            Color clearColor = Color.black;
            for (int i = 0; i < m_usersDepthMap.Length; i++)
            {
                /*// Flip the texture as we convert label map to color array
                int flipIndex = i; // usersMapSize - i - 1;

                ushort userMap = (ushort)(m_usersDepthMap[i] & 7);
                ushort userDepth = (ushort)(m_usersDepthMap[i] >> 3);

                ushort nowUserPixel = userMap != 0 ? (ushort)((userMap << 13) | userDepth) : userDepth;
                ushort wasUserPixel = m_usersPrevState[flipIndex];

                if (nowUserPixel != wasUserPixel)
                {
                    m_usersPrevState[flipIndex] = nowUserPixel;

                    ref var color = ref m_textureBuffer[i];

                    KinectWrapper.NuiImageGetColorPixelCoordinatesFromDepthPixelAtResolution(
                        KinectWrapper.Constants.ColorImageResolution,
                        KinectWrapper.Constants.DepthImageResolution,
                        ref pcViewArea,
                        i % KinectWrapper.Constants.DepthImageWidth, i / KinectWrapper.Constants.DepthImageWidth,
                        m_usersDepthMap[i],
                        out var cx, out var cy);

                    StoreDepth(ref color, userDepth);
                    //color.r = LinearRgbToLuminance(m_colorBuffer[cy * KinectWrapper.Constants.ColorImageWidth + cx]);
                }*/

                ushort rawDepth = m_usersDepthMap[i];

                if (rawDepth != m_usersPrevState[i])
                {
                    ref var color = ref m_textureBuffer[i];
                    KinectWrapper.NuiImageGetColorPixelCoordinatesFromDepthPixelAtResolution(
                        KinectWrapper.Constants.ColorImageResolution,
                        KinectWrapper.Constants.DepthImageResolution,
                        ref pcViewArea,
                        i % KinectWrapper.Constants.DepthImageWidth, i / KinectWrapper.Constants.DepthImageWidth,
                        rawDepth,
                        out var cx, out var cy);

                    StoreDepth(ref color, m_usersDepthMap[i]);

                    m_usersPrevState[i] = rawDepth;
                }

                
            }

            Texture.SetPixels32(m_textureBuffer);
            Texture.Apply();
        }

        private void StoreDepth(ref Color32 color, ushort depth)
        {
            var dFloat = (depth >> 3) / (float)(1 << 13);
            var dNormal = Mathf.Clamp01((dFloat - m_range.x) / (m_range.y - m_range.x));
            HSVtoRGB(dNormal, ref color);
        }

        private void HSVtoRGB(float hue, ref Color32 color32)
        {
            float h = hue * 6 - 2;
            float r = Mathf.Abs(h - 1) - 1;
            float g = 2 - Mathf.Abs(h);
            float b = 2 - Mathf.Abs(h - 2);
            color32.r = (byte)(Mathf.Clamp01(r) * 255);
            color32.g = (byte)(Mathf.Clamp01(g) * 255);
            color32.b = (byte)(Mathf.Clamp01(b) * 255);
        }

        private void OnDisable()
        {
            if (m_initialized)
            {
                KinectWrapper.NuiShutdown();
                Destroy(Texture);
                m_initialized = false;
            }
        }
    }
}