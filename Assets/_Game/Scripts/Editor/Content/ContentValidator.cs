using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.World;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Validate Content. Checks: GoToNode targets, Trap/Death text + tip, ShowLockedFatal
    /// missing-item text + item, PrepPhaseNode -> PlacementNode links, hazards linked to an anchor (open
    /// scenes + prefabs) or used as an unplaced hazard, prep interactables wired, reachability from the
    /// start node, and that every referenced text key exists. Each problem is a clickable console entry.
    /// </summary>
    public static class ContentValidator
    {
        private const string Root = "Assets/_Game";

        [MenuItem("SixtyOne/Validate Content", priority = 12)]
        public static void Validate()
        {
            int problems = 0;
            List<SceneNode> nodes = LoadAll<SceneNode>();
            foreach (PlacementNode placementNode in LoadAll<PlacementNode>())
            {
                if (!nodes.Contains(placementNode)) nodes.Add(placementNode);
            }



            foreach (Hazard hazard in LoadAll<Hazard>())
            {
                problems += CheckHazard(hazard);
            }

            problems += CheckHazardLinks(nodes);
            problems += CheckWorldInteractables();
            problems += CheckReachability(nodes);
            problems += CheckTextKeys();

            string summary = $"[Content] Validate: {nodes.Count} nodes checked, {problems} problem(s).";
            if (problems > 0) Debug.LogWarning(summary); else Debug.Log(summary);
        }

        private static int CheckNode(SceneNode node)
        {
            int problems = 0;
            for (int i = 0; i < node.Choices.Count; i++)
            {
                Choice choice = node.Choices[i];
                if (choice == null) continue;
                string where = $"node '{node.Id}' choice {i} ({choice.LabelKey.Key})";
                problems += CheckOutcomes(choice.Outcomes, node, where);

                bool kills = choice.Type == ChoiceType.Trap || HasDeath(choice.Outcomes);
                if (kills) problems += CheckDeath(choice.Death, node, where);
                if (choice.Type == ChoiceType.Trap && !HasDeath(choice.Outcomes))
                {
                    problems += Report($"{where} is a Trap without a Death outcome (it still kills).", node);
                }

                if (choice.FailBehaviour == FailBehaviour.ShowLockedFatal)
                {
                    problems += CheckLockedFatal(choice, node, where);
                }
            }

            problems += CheckOutcomes(node.OnEnter, node, $"node '{node.Id}' onEnter");
            for (int i = 0; i < node.BombChecks.Count; i++)
            {
                BombCheck bomb = node.BombChecks[i];
                if (bomb == null) continue;
                string where = $"node '{node.Id}' bomb {i}";
                problems += CheckOutcomes(bomb.Outcomes, node, where);
                if (HasDeath(bomb.Outcomes)) problems += CheckDeath(bomb.Death, node, where);
            }

            if (node.TimerSeconds > 0f && !(node is PlacementNode) && node.GetTimeoutChoice() == null)
            {
                problems += Report($"node '{node.Id}' has a timer but no timeout choice and no Trap choice.", node);
            }



            if (node is PlacementNode placement)
            {
                if (placement.NextNode == null) problems += Report($"placement node '{node.Id}' has no next node.", node);
                if (placement.TimerSeconds <= 0f) problems += Report($"placement node '{node.Id}' needs a timer (the quake starts on timeout).", node);
                problems += CheckDeath(placement.ChildAloneDeath, node, $"placement node '{node.Id}' child-alone death");
            }

            return problems;
        }

        private static int CheckLockedFatal(Choice choice, Object owner, string where)
        {
            int problems = 0;
            if (choice.MissingItem == null) problems += Report($"{where}: ShowLockedFatal without a missing item.", owner);
            if (choice.MissingItemTextKey.IsEmpty) problems += Report($"{where}: ShowLockedFatal without a missing-item text key.", owner);
            if (choice.Death == null || choice.Death.Tip == null) problems += Report($"{where}: ShowLockedFatal has no tip (set it with WithDeath).", owner);

            bool requiresItem = false;
            foreach (Condition condition in choice.Conditions)
            {
                if (condition != null && condition.Type == ConditionType.HasItem && condition.Item == choice.MissingItem) requiresItem = true;
            }

            if (choice.MissingItem != null && !requiresItem)
            {
                problems += Report($"{where}: ShowLockedFatal does not require its missing item '{choice.MissingItem.Id}' (HasItem condition).", owner);
            }

            return problems;
        }

        private static int CheckWorldInteractables()
        {
            int problems = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (WorldInteractable interactable in root.GetComponentsInChildren<WorldInteractable>(true))
                    {
                        string where = $"interactable '{interactable.Id}'";
                        if (interactable.LabelKey.IsEmpty) problems += Report($"{where} has no label key.", interactable);
                        if (interactable.GrantsItem == null && interactable.Outcomes.Count == 0) problems += Report($"{where} does nothing (no item, no outcomes).", interactable);
                    }
                }
            }

            return problems;
        }

        private static int CheckOutcomes(IReadOnlyList<Outcome> outcomes, Object owner, string where)
        {
            int problems = 0;
            foreach (Outcome outcome in outcomes)
            {
                if (outcome == null) continue;
                if (outcome.Type == OutcomeType.GoToNode && outcome.TargetNode == null)
                {
                    problems += Report($"{where}: GoToNode without a target.", owner);
                }
                else if (outcome.Type == OutcomeType.SetFlag && string.IsNullOrEmpty(outcome.Flag))
                {
                    problems += Report($"{where}: SetFlag without a flag name.", owner);
                }
                else if ((outcome.Type == OutcomeType.AddItem || outcome.Type == OutcomeType.RemoveItem) && outcome.Item == null)
                {
                    problems += Report($"{where}: {outcome.Type} without an item.", owner);
                }
            }

            return problems;
        }

        private static int CheckDeath(DeathData death, Object owner, string where)
        {
            int problems = 0;
            if (death == null || death.TextKey.IsEmpty) problems += Report($"{where}: death has no text key.", owner);
            if (death == null || death.Tip == null) problems += Report($"{where}: death has no tip.", owner);
            return problems;
        }

        private static int CheckHazard(Hazard hazard)
        {
            int problems = 0;
            if (hazard.TextKey.IsEmpty) problems += Report($"hazard '{hazard.Id}' has no text key.", hazard);
            if (hazard.Tip == null) problems += Report($"hazard '{hazard.Id}' has no tip.", hazard);
            if (hazard.Acts == ActMask.None) problems += Report($"hazard '{hazard.Id}' is active in no act.", hazard);
            return problems;
        }

        private static int CheckHazardLinks(List<SceneNode> nodes)
        {
            int problems = 0;
            var linked = new HashSet<Hazard>();
            foreach (Anchor anchor in FindAnchors())
            {
                if (anchor.Hazard != null) linked.Add(anchor.Hazard);
                else if (anchor.Tag == AnchorTag.Trap) problems += Report($"Trap anchor '{anchor.Id}' has no hazard.", anchor);
            }

            foreach (SceneNode node in nodes)
            {
                if (node is PlacementNode placement && placement.UnplacedHazard != null) linked.Add(placement.UnplacedHazard);
            }

            foreach (Hazard hazard in LoadAll<Hazard>())
            {
                if (!linked.Contains(hazard))
                {
                    problems += Report($"hazard '{hazard.Id}' is not linked to any anchor (open the scene that uses it, e.g. Test_Act1).", hazard);
                }
            }

            return problems;
        }

        private static int CheckReachability(List<SceneNode> nodes)
        {
            var contents = LoadAll<GameContent>();
            if (contents.Count == 0)
            {
                return Report("No GameContent asset found; cannot check reachability.", null);
            }

            int problems = 0;
            foreach (GameContent content in contents)
            {
                if (content.StartNode == null)
                {
                    problems += Report($"GameContent '{content.name}' has no start node.", content);
                    continue;
                }

                var reached = new HashSet<SceneNode>();
                var queue = new Queue<SceneNode>();
                queue.Enqueue(content.StartNode);
                while (queue.Count > 0)
                {
                    SceneNode node = queue.Dequeue();
                    if (node == null || !reached.Add(node)) continue;
                    foreach (SceneNode next in Targets(node)) queue.Enqueue(next);
                }

                foreach (SceneNode node in nodes)
                {
                    if (!reached.Contains(node)) problems += Report($"node '{node.Id}' is unreachable from '{content.StartNode.Id}'.", node);
                }

                foreach (SceneNode node in reached)
                {
                    if (content.FindNode(node.Id) == null)
                    {
                        problems += Report($"node '{node.Id}' is reachable but missing from GameContent '{content.name}' (rewinds to it will fail).", content);
                    }
                }
            }

            return problems;
        }

        private static IEnumerable<SceneNode> Targets(SceneNode node)
        {
            foreach (Choice choice in node.Choices)
            {
                if (choice == null) continue;
                foreach (Outcome o in choice.Outcomes)
                {
                    if (o != null && o.Type == OutcomeType.GoToNode && o.TargetNode != null) yield return o.TargetNode;
                }
            }

            foreach (Outcome o in node.OnEnter)
            {
                if (o != null && o.Type == OutcomeType.GoToNode && o.TargetNode != null) yield return o.TargetNode;
            }

            if (node is PlacementNode placement && placement.NextNode != null) yield return placement.NextNode;
        }

        private static int CheckTextKeys()
        {
            TextMenu.Reload();
            Dictionary<string, List<Object>> refs = TextKeyCollector.Collect();
            int problems = 0;
            foreach (string key in TextKeyCollector.FindMissing(refs, includeEmpty: false))
            {
                List<Object> owners = refs[key];
                problems += Report($"text key '{key}' is missing from strings_tr.csv.", owners.Count > 0 ? owners[0] : null);
            }

            return problems;
        }

        private static bool HasDeath(IReadOnlyList<Outcome> outcomes)
        {
            foreach (Outcome o in outcomes)
            {
                if (o != null && o.Type == OutcomeType.Death) return true;
            }

            return false;
        }

        private static IEnumerable<Anchor> FindAnchors()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Anchor anchor in root.GetComponentsInChildren<Anchor>(true)) yield return anchor;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                foreach (Anchor anchor in prefab.GetComponentsInChildren<Anchor>(true)) yield return anchor;
            }
        }

        private static List<T> LoadAll<T>() where T : ScriptableObject
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { Root }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }

            return list;
        }

        private static int Report(string message, Object context)
        {
            Debug.LogWarning("[Content] " + message, context);
            return 1;
        }
    }
}
