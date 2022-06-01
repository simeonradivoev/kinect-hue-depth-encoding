using System;
using System.IO;
using UnityEngine;

namespace UnityTemplateProjects
{
    /// <summary>
    /// Texture provider for files recorded by <see cref="CustomFileRecorder"/>.
    /// </summary>
    public class CustomFileDataProvider : MonoBehaviour, ITexture2DDataProvider
    {
        public Vector2 Range => m_range;
        public bool FlipY => false;

        public bool IsEnabled => m_fileExists;

        public float Angle => m_angle;
        private Stream m_fileStream;
        private BinaryReader m_binaryReader;
        public Texture2D Texture { get; private set; }
        Texture ITextureDataProvider.Texture => Texture;
        private int m_width;
        private int m_height;
        private int m_framerate;
        private byte[] m_rawData;
        private bool m_isActive;
        private bool m_fileExists;
        private Vector2 m_range;
        private float m_angle;

        private void OnEnable()
        {
            m_fileExists = File.Exists("Assets/Videos/CustomVideo.dat");
            if (m_fileExists)
            {
                m_fileStream = File.OpenRead("Assets/Videos/CustomVideo.dat");
                m_binaryReader = new BinaryReader(m_fileStream);
                ReadHeader();
                Texture = new Texture2D(m_width, m_height, TextureFormat.RGB24, false, false);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = m_framerate;
            }
        }

        private void ReadHeader()
        {
            m_width = m_binaryReader.ReadInt16();
            m_height = m_binaryReader.ReadInt16();
            m_framerate = m_binaryReader.ReadByte();
            m_range = new Vector2(m_binaryReader.ReadSingle(), m_binaryReader.ReadSingle());
            m_angle = m_binaryReader.ReadSingle();
        }
        
        public void SetActive(bool isActive)
        {
            m_isActive = isActive;
        }

        private void Update()
        {
            if (m_isActive)
            {
                bool isEnd = m_binaryReader.ReadBoolean();
                if (isEnd)
                {
                    m_fileStream.Position = 0;
                    ReadHeader();
                }
                else
                {
                    int length = m_binaryReader.ReadInt32();
                    if (m_rawData == null)
                    {
                        m_rawData = new byte[length];
                    }
                    else if (m_rawData.Length < length)
                    {
                        Array.Resize(ref m_rawData,length);
                    }

                    m_binaryReader.Read(m_rawData, 0, length);
                    Texture.LoadImage(lz4.Decompress(m_rawData));
                }
            }
        }

        private void OnDisable()
        {
            m_fileStream.Dispose();
            m_binaryReader.Dispose();
            Destroy(Texture);
        }
    }
}