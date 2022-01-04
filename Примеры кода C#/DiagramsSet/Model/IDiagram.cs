
namespace Monitel.SCADA.UICommon.DiagramsSet
{
    /// <summary>
    /// Элемент дерева наборов
    /// </summary>
    public interface IDiagramItem
    {
        string UID { get; }
        string Name { get; set; }
        string Path { get; set; }
        AccessLayer AccessLayer { get; set; }
        FolderItem Parent { get; set; }

        void Remove();
    }
}
