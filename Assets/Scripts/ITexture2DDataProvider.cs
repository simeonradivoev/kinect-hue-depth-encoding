using UnityEngine;

namespace UnityTemplateProjects
{
    public interface ITexture2DDataProvider : ITextureDataProvider
    {
        new Texture2D Texture { get; }
    }
}