using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>Registry of all nodes, items and tips plus the start node.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Game Content", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        [Tooltip("First node of the game.")]
        [SerializeField] private SceneNode startNode;

        [Tooltip("All nodes (needed to resolve rewinds by id).")]
        [SerializeField] private List<SceneNode> nodes = new List<SceneNode>();

        [Tooltip("All items (bag icons/names).")]
        [SerializeField] private List<Item> items = new List<Item>();

        [Tooltip("All tips (journal x/total).")]
        [SerializeField] private List<Tip> tips = new List<Tip>();

        public SceneNode StartNode => startNode;
        public IReadOnlyList<SceneNode> Nodes => nodes;
        public IReadOnlyList<Item> Items => items;
        public IReadOnlyList<Tip> Tips => tips;

        public SceneNode FindNode(string id) => nodes.Find(n => n != null && n.Id == id);
        public Item FindItem(string id) => items.Find(i => i != null && i.Id == id);
        public Tip FindTip(string id) => tips.Find(t => t != null && t.Id == id);

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(SceneNode start, IEnumerable<SceneNode> allNodes, IEnumerable<Item> allItems, IEnumerable<Tip> allTips)
        {
            startNode = start;
            nodes = new List<SceneNode>(allNodes);
            items = new List<Item>(allItems);
            tips = new List<Tip>(allTips);
        }
    }
}
