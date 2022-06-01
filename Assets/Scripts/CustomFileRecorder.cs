using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace UnityTemplateProjects
{
    /// <summary>
    /// Records the currently provided texture by the avtive <see cref="ITextureDataProvider"/> and writes each frame as a PNG to a custom file format.
    /// This is used for lossless compression as lossless video might not work on every machine.
    /// This also has the benefit of storing the kinect parameters such as the angle.
    /// </summary>
    public class CustomFileRecorder : MonoBehaviour
    {
        [SerializeField]
        private int m_framerate;
        [SerializeField]
        private int m_compressionLevel;
        private Stream m_fileStream;
        private BinaryWriter m_binaryWriter;
        private ITexture2DDataProvider m_textureProvider;

        private bool m_isRecording;

        private void StartRecording()
        {
            m_textureProvider = GetComponentInChildren<ITexture2DDataProvider>();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = m_framerate;

            string path = "Assets/Videos/CustomVideo.dat";
            var directory = Path.GetDirectoryName(path) ?? throw new Exception("Could not find directory");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            m_fileStream = File.Open(path, File.Exists(path) ? FileMode.Truncate : FileMode.CreateNew, FileAccess.Write,
                FileShare.Write);

            m_binaryWriter = new BinaryWriter(m_fileStream);
            m_binaryWriter.Write((short)m_textureProvider.Texture.width);
            m_binaryWriter.Write((short)m_textureProvider.Texture.height);
            m_binaryWriter.Write((byte)m_framerate);
            m_binaryWriter.Write(m_textureProvider.Range.x);
            m_binaryWriter.Write(m_textureProvider.Range.y);
            m_binaryWriter.Write(m_textureProvider.Angle);
        }

        private void StopRecording()
        {
            m_binaryWriter.Write(true);
            m_binaryWriter.Dispose();
            m_fileStream.Dispose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (m_isRecording)
                {
                    m_isRecording = false;
                    StopRecording();
                }
                else
                {
                    m_isRecording = true;
                    StartRecording();
                }
            }

            if (m_isRecording)
            {
                var data = m_textureProvider.Texture.EncodeToPNG();
                var compressed = lz4.Compress(data, m_compressionLevel);
                m_binaryWriter.Write(false);
                m_binaryWriter.Write(compressed.Length);
                m_binaryWriter.Write(compressed);
            }
        }

        private void OnDisable()
        {
            if (m_isRecording)
            {
                StopRecording();
            }
        }
    }
}