using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class SceneTreeHandler
    {
        protected Tree sceneTree;
        protected SceneTreeInfo sceneTreeInfo;

        protected Dictionary<SceneTreeInfo.NodeInfo, TreeItem> treeItemsLookupTable = new Dictionary<SceneTreeInfo.NodeInfo, TreeItem>();
        protected Dictionary<TreeItem, SceneTreeInfo.NodeInfo> nodeInfosLookupTable = new Dictionary<TreeItem, SceneTreeInfo.NodeInfo>();

        public SceneTreeHandler(Tree sceneTree, SceneTreeInfo sceneTreeInfo)
        {
            this.sceneTree = sceneTree;
            this.sceneTreeInfo = sceneTreeInfo;

            sceneTree.Clear();
            SetupTree();
            UpdateTree();
        }

        private void SetupTree()
        {
            sceneTree.SetColumnExpand(0, true);
            sceneTree.SetColumnExpand(1, false);
            sceneTree.SetColumnExpand(2, false);

            foreach (SceneTreeInfo.NodeInfo nodeInfo in sceneTreeInfo.nodeInfos)
            {
                SceneTreeInfo.NodeInfo parentNodeInfo = sceneTreeInfo.FindParentNodeInfo(nodeInfo);
                SetupNode(nodeInfo, parentNodeInfo);
            }
        }

        protected virtual void SetupNode(SceneTreeInfo.NodeInfo nodeInfo, SceneTreeInfo.NodeInfo parentNodeInfo)
        {
            TreeItem parentItem = parentNodeInfo != null ? treeItemsLookupTable[parentNodeInfo] : null;
            TreeItem item = sceneTree.CreateItem(parentItem);
            SetupTreeItem(item, nodeInfo, parentNodeInfo);

            treeItemsLookupTable.Add(nodeInfo, item);
            nodeInfosLookupTable.Add(item, nodeInfo);
        }

        protected virtual void SetupTreeItem(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo, SceneTreeInfo.NodeInfo parentNodeInfo)
        {
            item.SetText(0, nodeInfo.name);
            item.SetIcon(0, sceneTree.GetThemeIcon(IconNameFromNode(nodeInfo.type), "EditorIcons"));
        }

        public virtual void UpdateTree()
        {
            foreach (TreeItem item in nodeInfosLookupTable.Keys)
            {
                UpdateNode(item, nodeInfosLookupTable[item]);
            }
        }

        protected virtual void UpdateNode(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo)
        {
            
        }

        private string IconNameFromNode(string type)
        {
            if (type == "") return "PackedScene";
            return type.Replace("Godot.", "");
        }
    }
}
