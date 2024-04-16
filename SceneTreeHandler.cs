using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class SceneTreeHandler
    {
        private Color yellowColor = Color.FromString("yellow", new Color());
        private Color whiteColor = Color.FromString("white", new Color());

        private Tree sceneTree;
        private SceneTreeInfo sceneTreeInfo;

        private Dictionary<SceneTreeInfo.NodeInfo, TreeItem> treeItemsLookupTable = new Dictionary<SceneTreeInfo.NodeInfo, TreeItem>();
        private Dictionary<TreeItem, SceneTreeInfo.NodeInfo> nodeInfosLookupTable = new Dictionary<TreeItem, SceneTreeInfo.NodeInfo>();
        private List<SceneTreeInfo.NodeInfo> parentSceneNodeInfos = new List<SceneTreeInfo.NodeInfo>();

        public List<SceneTreeInfo.NodeInfo> ParentSceneNodeInfos { get => parentSceneNodeInfos; }

        public SceneTreeHandler(Tree sceneTree, SceneTreeInfo sceneTreeInfo)
        {
            this.sceneTree = sceneTree;
            this.sceneTreeInfo = sceneTreeInfo;
            SetupTree();
            UpdateTree();
        }

        private void SetupTree()
        {
            foreach (SceneTreeInfo.NodeInfo nodeInfo in sceneTreeInfo.nodeInfos)
            {
                SceneTreeInfo.NodeInfo parentNodeInfo = sceneTreeInfo.FindParentNodeInfo(nodeInfo);
                if (parentNodeInfo == null) parentSceneNodeInfos.Add(nodeInfo);

                TreeItem parentItem = parentNodeInfo != null ? treeItemsLookupTable[parentNodeInfo] : null;
                TreeItem item = sceneTree.CreateItem(parentItem);
                item.SetText(0, nodeInfo.name);
                item.SetIcon(0, sceneTree.GetThemeIcon(IconNameFromNode(nodeInfo.type), "EditorIcons"));
                item.SetIconModulate(2, yellowColor);

                treeItemsLookupTable.Add(nodeInfo, item);
                nodeInfosLookupTable.Add(item, nodeInfo);
            }
        }

        public void UpdateTree()
        {
            foreach (TreeItem item in nodeInfosLookupTable.Keys)
            {
                if (parentSceneNodeInfos.Contains(nodeInfosLookupTable[item]))
                {
                    item.SetCustomColor(0, yellowColor);
                    item.SetIcon(2, sceneTree.GetThemeIcon("Node", "EditorIcons"));
                    item.SetIconModulate(1, yellowColor);
                }
                else
                {
                    item.SetCustomColor(0, whiteColor);
                    item.SetIcon(2, null);
                    item.SetIconModulate(1, whiteColor);
                }
            }
        }

        private string IconNameFromNode(string type)
        {
            if (type == "") return "PackedScene";
            return type.Replace("Godot.", "");
        }

        public void ToggleNode(TreeItem treeItem)
        {
            SceneTreeInfo.NodeInfo nodeInfo = nodeInfosLookupTable[sceneTree.GetSelected()];

            List<SceneTreeInfo.NodeInfo> ancestors = sceneTreeInfo.FindAncestorsNodeInfo(nodeInfo);
            List<SceneTreeInfo.NodeInfo> desendants = sceneTreeInfo.FindDescendantsNodeInfo(nodeInfo);

            foreach (SceneTreeInfo.NodeInfo ancestor in ancestors)
            {
                if (ancestor != null && !parentSceneNodeInfos.Contains(ancestor))
                    parentSceneNodeInfos.Add(ancestor);
            }

            if (!parentSceneNodeInfos.Contains(nodeInfo)) parentSceneNodeInfos.Add(nodeInfo);

            foreach (SceneTreeInfo.NodeInfo descendant in desendants)
            {
                parentSceneNodeInfos.Remove(descendant);
            }

            UpdateTree();
        }
    }
}
