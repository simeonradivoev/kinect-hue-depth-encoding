using UnityEngine;
using UnityEngine.Video;

namespace UnityTemplateProjects
{
    /// <summary>
    /// Provides texture from a video player.
    /// Range and angle must be provided manually as the video does not store those values.
    /// </summary>
    public class VideoPlayerProvider : MonoBehaviour, ITextureDataProvider
    {
        [SerializeField] private VideoPlayer m_videoPlayer;
        [SerializeField] private Vector2 m_range;
        [SerializeField] private float m_angle;

        public Texture Texture => m_videoPlayer.texture;

        public Vector2 Range => m_range;

        public float Angle => m_angle;

        public bool FlipY => false;
        
        public void SetActive(bool isActive)
        {
            if (isActive)
            {
                if (!m_videoPlayer.isPlaying)
                {
                    m_videoPlayer.Play();
                }
            }
            else
            {
                if (m_videoPlayer.isPlaying)
                {
                    m_videoPlayer.Stop();
                }
            }
        }

        public bool IsEnabled => m_videoPlayer.isActiveAndEnabled;
    }
}