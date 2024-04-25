using Godot;
using System;
using System.Diagnostics;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class ReparentedTreeHandler : SceneTreeHandler
    {
        private Color yellowColor = Color.FromString("yellow", new Color());
        private Color redColor = Color.FromString("red", new Color());
        private Color whiteColor = Color.FromString("white", new Color());

        private SceneTreeInfo newParent;

        public ReparentedTreeHandler(Tree sceneTree, SceneTreeInfo sceneTreeInfo) : base(sceneTree, sceneTreeInfo)
        {
        }

        public void UpdateTree(SceneTreeInfo newParent)
        {
            this.newParent = newParent;
            UpdateTree();
        }

        public bool IsValidOrigin()
        {
            foreach (SceneTreeInfo.NodeInfo nodeInfo in sceneTreeInfo.nodeInfos)
            {
                if (!IsNodeValid(nodeInfo)) return false;
            }
            return true;
        }

        private bool IsNodeValid(SceneTreeInfo.NodeInfo nodeInfo)
        {
            return nodeInfo.path == "." || nodeInfo.ownerPath != "";                        //It's seems nodeInfo.ownerPath == "" means the node belong to an instance scene but we needs it because of "editable children" is on and this instance's node is the parent of a non-instance added node
        }

        protected override void UpdateNode(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo)
        {
            base.UpdateNode(item, nodeInfo);

            if (newParent == null)
            {
                item.SetCustomColor(0, whiteColor);
                item.SetIcon(2, null);
                item.SetIconModulate(1, whiteColor);
            }
            else
            {
                SceneTreeInfo.NodeInfo newParentCounterpart = newParent.FindNodeInfoByPath(nodeInfo.path);
                if (newParentCounterpart == null)
                {
                    item.SetCustomColor(0, whiteColor);
                    item.SetIcon(2, null);
                    item.SetIconModulate(1, whiteColor);
                }
                else
                {
                    item.SetCustomColor(0, yellowColor);
                    item.SetIcon(2, sceneTree.GetThemeIcon("Node", "EditorIcons"));
                    item.SetIconModulate(2, yellowColor);
                    item.SetIconModulate(1, yellowColor);
                }
            }

            if (!IsNodeValid(nodeInfo))
            {
                item.SetCustomColor(0, redColor);
                item.SetIcon(2, sceneTree.GetThemeIcon("Node", "EditorIcons"));
                item.SetIconModulate(2, redColor);
                item.SetIconModulate(1, redColor);
            }
        }
    }
}
