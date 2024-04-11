using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TurboTartine.Godot.ReparentScenePlugin
{
    //Godot icons https://github.com/godotengine/godot/tree/master/editor/icons

    public partial class ReparentSceneDialog : ConfirmationDialog
    {
        private PackedScene dialagContentPanelScn = GD.Load<PackedScene>("res://addons/tt-godot-reparent-scene-plugin/ReparentSceneDialogContent.tscn");
        private PackedScene boundScene;
        private Node boundSceneRoot;
        Tree sceneTree;

        private Dictionary<Node, TreeItem> treeItemsLookupTable = new Dictionary<Node, TreeItem>();
        private Dictionary<TreeItem, Node> nodesLookupTable = new Dictionary<TreeItem, Node>();
        private List<Node> parentSceneNodes = new List<Node>();
        private List<Node> instancedSceneNodes = new List<Node>();

        public ReparentSceneDialog(string scenePath) : base()
        {
            boundScene = ResourceLoader.Load<PackedScene>(scenePath);
            SceneState state = boundScene.GetState();
            for (int i = 0; i < state.GetNodeCount(); i++)
            {
                PackedScene subScene = state.GetNodeInstance(i);
                GD.Print(subScene);
            }

            boundSceneRoot = boundScene.Instantiate<Node>(PackedScene.GenEditState.Instance);
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            this.Title = "Select the nodes to extract to the new parent scene";
            this.Confirmed += ReparentScene;
            
            Panel panel = dialagContentPanelScn.Instantiate<Panel>();

            sceneTree = panel.GetNode<Tree>("%SceneTree"); sceneTree.ItemSelected += OnItemSelected;
            sceneTree.ButtonClicked += OnTreeItemClicked;

            List<Node> nodes = FlattenBoundSceneTree(boundSceneRoot);

            foreach (Node node in nodes)
            {
                Node parentNode = node.GetParent();
                if (parentNode == null) parentSceneNodes.Add(node);

                string name = node.Name;
                bool isInstance = node.GetSceneInstanceLoadPlaceholder();
                TreeItem parentItem = parentNode != null ? treeItemsLookupTable[parentNode] : null;

                TreeItem treeItem = sceneTree.CreateItem(parentItem);
                treeItem.SetText(0, name);
                treeItem.SetIcon(0, GetThemeIcon(IconNameFromNode(node), "EditorIcons"));
                if (isInstance)
                {
                    treeItem.SetText(1, "Instanced");
                    treeItem.SetIcon(1, GetThemeIcon("PackedScene", "EditorIcons"));
                    instancedSceneNodes.Add(node);
                }
                else
                {
                    treeItem.SetText(1, "Not Instanced");
                }

                treeItemsLookupTable.Add(node, treeItem);
                nodesLookupTable.Add(treeItem, node);
            }

            UpdateTree();

            this.AddChild(panel);
        }

        private void OnItemSelected()
        {
            Node node = nodesLookupTable[sceneTree.GetSelected()];
            List<Node> ancestors = NodeUtils.FindNodesInParents<Node>(node, true);
            List<Node> desendants = NodeUtils.FindNodesInChildren<Node>(node, false);

            foreach (Node ancestor in ancestors)
            {
                if (!parentSceneNodes.Contains(ancestor))
                    parentSceneNodes.Add(ancestor);
            }

            foreach (Node descendant in desendants)
            {
                parentSceneNodes.Remove(descendant);
            }

            UpdateTree();
        }

        private void UpdateTree()
        {
            foreach(TreeItem item in nodesLookupTable.Keys)
            {
                if (parentSceneNodes.Contains(nodesLookupTable[item]))
                {
                    item.SetCustomColor(0, Color.FromString("yellow", new Color()));
                }
                else
                {
                    item.SetCustomColor(0, Color.FromString("white", new Color()));
                }
            }
        }

        private void OnTreeItemClicked(TreeItem item, long column, long id, long mouseButtonIndex)
        {
            Node node = nodesLookupTable[item];
            List<Node> ancestors = NodeUtils.FindNodesInParents<Node>(node, true);
            List<Node> desendants = NodeUtils.FindNodesInChildren<Node>(node, false);

            foreach (Node ancestor in ancestors)
            {
                if (!parentSceneNodes.Contains(ancestor))
                    parentSceneNodes.Add(ancestor);
            }

            foreach (Node descendant in desendants)
            {
                parentSceneNodes.Remove(descendant);
            }

            UpdateTree();
        }

        //private Texture2D GetThemeIcon(StringName name, StringName themeType = null)
        //{
        //    return EditorInterface.Singleton.GetBaseControl().GetThemeIcon(name, themeType);
        //}

        private string IconNameFromNode(Node node)
        {
            string iconName = node.GetType().ToString();
            iconName = iconName.Replace("Godot.", "");
            return iconName;
        }

        private List<Node> FlattenBoundSceneTree(Node node)
        {
            List<Node> flattenTree = new List<Node>();
            flattenTree.Add(node);
            for (int i = 0; i < node.GetChildCount(); i++)
            {
                flattenTree.AddRange(FlattenBoundSceneTree(node.GetChild(i)));
            }
            return flattenTree;
        }

        private void ReparentScene()
        {
            GD.Print("Reparent");
        }
    }
}
