namespace SixtyOne.Nodes
{
    /// <summary>Shows the room a node takes place in.</summary>
    public interface IRoomPresenter
    {
        /// <summary>preferredRoomId: a room of the same flat to show instead (the room a rewind restored). May be null.</summary>
        void ShowRoom(string roomId, string preferredRoomId);
    }
}
