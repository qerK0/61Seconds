using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Creates and cleans up ScriptableObjects for tests.</summary>
    public sealed class TestData
    {
        private readonly List<Object> created = new List<Object>();

        public T Make<T>() where T : ScriptableObject
        {
            T obj = ScriptableObject.CreateInstance<T>();
            created.Add(obj);
            return obj;
        }

        public Item Item(string id)
        {
            Item item = Make<Item>();
            item.Configure(id, "item." + id + ".name", "item." + id + ".desc");
            return item;
        }

        public Tip Tip(string id)
        {
            Tip tip = Make<Tip>();
            tip.Configure(id, id + ".title", id);
            return tip;
        }

        public SceneNode Node(string id, int act = 1)
        {
            SceneNode node = Make<SceneNode>();
            node.Configure(id, id + ".title", id + ".body", "room", act);
            return node;
        }

        public Hazard Hazard(string id, HazardEffect player, HazardEffect wife, HazardEffect child, ActMask acts = ActMask.All)
        {
            Hazard hazard = Make<Hazard>();
            hazard.Configure(id, acts, player, wife, child, HazardMotion.None, id);
            hazard.ConfigureText("death." + id, "death.wife." + id, "death.child." + id, Tip("tip." + id));
            return hazard;
        }

        public void Cleanup()
        {
            foreach (Object obj in created)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }

            created.Clear();
            GameEvents.ClearAll();
        }
    }
}
