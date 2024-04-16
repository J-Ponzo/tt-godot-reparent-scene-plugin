using Godot;
using System;
using System.Collections.Generic;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class SelectParentSubTreeHandler : SceneTreeHandler
    {
        private Color yellowColor = Color.FromString("yellow", new Color());
        private Color whiteColor = Color.FromString("white", new Color());

        protected List<SceneTreeInfo.NodeInfo> parentSceneNodeInfos = new List<SceneTreeInfo.NodeInfo>();
        public List<SceneTreeInfo.NodeInfo> ParentSceneNodeInfos { get => parentSceneNodeInfos; }

        public SelectParentSubTreeHandler(Tree sceneTree, SceneTreeInfo sceneTreeInfo) : base(sceneTree, sceneTreeInfo)
        {
        }

        protected override void SetupNode(SceneTreeInfo.NodeInfo nodeInfo, SceneTreeInfo.NodeInfo parentNodeInfo)
        {
            base.SetupNode(nodeInfo, parentNodeInfo);
            if (parentNodeInfo == null) parentSceneNodeInfos.Add(nodeInfo);
        }

        protected override void SetupTreeItem(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo, SceneTreeInfo.NodeInfo parentNodeInfo)
        {
            base.SetupTreeItem(item, nodeInfo, parentNodeInfo);
            item.SetIconModulate(2, yellowColor);
        }

        protected override void UpdateNode(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo)
        {
            base.UpdateNode(item, nodeInfo);
            if (parentSceneNodeInfos.Contains(nodeInfo))
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
