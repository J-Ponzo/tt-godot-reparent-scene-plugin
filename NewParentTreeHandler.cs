using Godot;
using System;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class NewParentTreeHandler : SceneTreeHandler
    {
        private Color greenColor = Color.FromString("green", new Color());
        private Color redColor = Color.FromString("red", new Color());
        private Color whiteColor = Color.FromString("white", new Color());

        private SceneTreeInfo reparented;

        public NewParentTreeHandler(Tree sceneTree, SceneTreeInfo sceneTreeInfo) : base(sceneTree, sceneTreeInfo)
        {
        }

        public void UpdateTree(SceneTreeInfo reparented)
        {
            this.reparented = reparented;
            UpdateTree();
        }

        public bool IsValidParent()
        {
            if (reparented == null) return false;
            foreach(SceneTreeInfo.NodeInfo nodeInfo in sceneTreeInfo.nodeInfos)
            {
                SceneTreeInfo.NodeInfo reparentedCounterpart = reparented.FindNodeInfoByPath(nodeInfo.path);
                if (reparentedCounterpart == null || reparentedCounterpart.type != nodeInfo.type) return false;
            }
            return true;
        }

        protected override void UpdateNode(TreeItem item, SceneTreeInfo.NodeInfo nodeInfo)
        {
            base.UpdateNode(item, nodeInfo);
            if (reparented == null)
            {
                item.SetCustomColor(0, whiteColor);
                item.SetIcon(2, null);
                item.SetIconModulate(1, whiteColor);
                return;
            }

            SceneTreeInfo.NodeInfo reparentedCounterpart = reparented.FindNodeInfoByPath(nodeInfo.path);
            if (reparentedCounterpart != null && reparentedCounterpart.type == nodeInfo.type)
            {
                item.SetCustomColor(0, greenColor);
                item.SetIcon(2, sceneTree.GetThemeIcon("Node", "EditorIcons"));
                item.SetIconModulate(2, greenColor);
                item.SetIconModulate(1, greenColor);
            }
            else
            {
                item.SetCustomColor(0, redColor);
                item.SetIcon(2, sceneTree.GetThemeIcon("Node", "EditorIcons"));
                item.SetIconModulate(2, redColor);
                item.SetIconModulate(1, redColor);
            }
        }
    }
}
