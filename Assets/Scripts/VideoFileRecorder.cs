using System;
using FFmpegOut;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace UnityTemplateProjects
{
    /// <summary>
    /// Records a texture provider to a video file.
    /// Video files are stored in the root project folder.
    /// </summary>
    public class VideoFileRecorder : MonoBehaviour
    {
        [SerializeField] private int m_framerate;
        [SerializeField] FFmpegPreset m_preset;
        [SerializeField] private string m_additionalParameters;

        private bool m_isRecording;
        private FFmpegSession m_session;
        private ITexture2DDataProvider m_textureProvider;
        private int m_frameCount;
        private float m_startTime;
        private int m_frameDropCount;

        float FrameTime
        {
            get { return m_startTime + (m_frameCount - 0.5f) / m_framerate; }
        }

        private void StartRecording()
        {
            m_textureProvider = GetComponentInChildren<ITexture2DDataProvider>();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = m_framerate;

            name += System.DateTime.Now.ToString(" yyyy MMdd HHmmss");
            var path = name.Replace(" ", "_") + m_preset.GetSuffix();
            
            var arguments = "-y -f rawvideo -vcodec rawvideo -pixel_format rgba" +
                            " -colorspace bt709" +
                            " -video_size " +
                            m_textureProvider.Texture.width +
                            "x" +
                            m_textureProvider.Texture.height +
                            " -framerate " +
                            m_framerate +
                            " -loglevel warning -i - " +
                            m_preset.GetOptions() + 
                            m_additionalParameters +
                            " \"" +
                            path +
                            "\"";

            // Start an FFmpeg session.
            m_session = FFmpegSession.CreateWithArguments(arguments);

            m_startTime = Time.time;
            m_frameCount = 0;
            m_frameDropCount = 0;
        }

        private void StopRecording()
        {
            m_session.Dispose();
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
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
                if (m_session != null)
                {
                    var gap = Time.time - FrameTime;
                    var delta = 1 / m_framerate;

                    if (gap < 0)
                    {
                        // Update without frame data.
                        m_session.PushFrame(null);
                    }
                    else if (gap < delta)
                    {
                        // Single-frame behind from the current time:
                        // Push the current frame to FFmpeg.
                        m_session.PushFrame(m_textureProvider.Texture);
                        m_frameCount++;
                    }
                    else if (gap < delta * 2)
                    {
                        // Two-frame behind from the current time:
                        // Push the current frame twice to FFmpeg. Actually this is not
                        // an efficient way to catch up. We should think about
                        // implementing frame duplication in a more proper way. #fixme
                        m_session.PushFrame(m_textureProvider.Texture);
                        m_session.PushFrame(m_textureProvider.Texture);
                        m_frameCount += 2;
                    }
                    else
                    {
                        // Show a warning message about the situation.
                        WarnFrameDrop();

                        // Push the current frame to FFmpeg.
                        m_session.PushFrame(m_textureProvider.Texture);

                        // Compensate the time delay.
                        m_frameCount += Mathf.FloorToInt(gap * m_framerate);
                    }
                }

                m_session?.CompletePushFrames();
            }
        }

        void WarnFrameDrop()
        {
            if (++m_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        private void OnDisable()
        {
            if (m_isRecording)
            {
                StopRecording();
                m_isRecording = false;
            }
        }
    }
}