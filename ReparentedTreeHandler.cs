using Godot;
using System;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class ReparentedTreeHandler : SceneTreeHandler
    {
        private Color yellowColor = Color.FromString("yellow", new Color());
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

        protected override void UpdateNode(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo)
        {
            base.UpdateNode(item, nodeInfo);

            if (newParent == null)
            {
                item.SetCustomColor(0, whiteColor);
                item.SetIcon(2, null);
                item.SetIconModulate(1, whiteColor);
                return;
            }

            SceneTreeInfo.NodeInfo newParentCounterpart = newParent.FindNodeInfoByPath(nodeInfo.path);
            if (newParentCounterpart != null && newParentCounterpart.type == nodeInfo.type)
            {
                item.SetCustomColor(0, yellowColor);
                item.SetIcon(2, sceneTree.GetThemeIcon("Node", "EditorIcons"));
                item.SetIconModulate(2, yellowColor);
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
}
