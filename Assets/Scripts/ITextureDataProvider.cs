using UnityEngine;

namespace UnityTemplateProjects
{
    /// <summary>
    /// Provides parameters needed for the point cloud shader updated by <see cref="KinectVideoPlay"/>.
    /// </summary>
    public interface ITextureDataProvider
    {
        /// <summary>
        /// Hue Encoded Depth Texture Used in Point Cloud Shader
        /// </summary>
        Texture Texture { get; }
        
        /// <summary>
        /// The Range of the depth.
        /// </summary>
        Vector2 Range { get; }
        
        /// <summary>
        /// The Angle of the device.
        /// </summary>
        float Angle { get; }
        
        /// <summary>
        /// Should the Y axis of the texture be flipped in the point cloud shader.
        /// </summary>
        bool FlipY { get; }

        void SetActive(bool isActive);
        
        /// <summary>
        /// Is the texture provider the currently active one.
        /// </summary>
        bool IsEnabled { get; }
    }
}