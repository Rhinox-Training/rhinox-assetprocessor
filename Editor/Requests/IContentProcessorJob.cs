namespace Rhinox.AssetProcessor.Editor
{
    public interface IContentProcessorJob
    {
        ImportedContentCache ImportedContent { get; }
    }
}