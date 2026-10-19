namespace SixtyOne.Nodes
{
    /// <summary>Hides choices the world already offers (doors, NPC hotspots) from the choice panel.</summary>
    public interface IChoiceFilter
    {
        bool Hides(SceneNode node, Choice choice);
    }
}
