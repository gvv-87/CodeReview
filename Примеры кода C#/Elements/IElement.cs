namespace Aggregator.Elements
{
    public interface IElement
    {
        void Visit(IVisitor visitor);
    }
}
