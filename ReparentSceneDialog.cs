using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TurboTartine.Godot.ReparentScenePlugin
{
    //Godot icons https://github.com/godotengine/godot/tree/master/editor/icons
   
    
    internal class NodeInfo 
    {
        public string[] groups;
        public int index;
        public PackedScene instance;
        public string instancePlaceholder;
        public StringName name;
        public NodePath ownerPath;
        public NodePath path;
        public Dictionary<StringName, Variant> porperties;
        public bool isInstancePlaceHolder;
    }

    public partial class ReparentSceneDialog : ConfirmationDialog
    {
        private PackedScene dialagContentPanelScn = GD.Load<PackedScene>("res://addons/tt-godot-reparent-scene-plugin/ReparentSceneDialogContent.tscn");
        private PackedScene boundScene;
        private Node boundSceneRoot;
        private Tree sceneTree;
        private NodeInfo[] nodeInfos;

        //private Dictionary<Node, TreeItem> treeItemsLookupTable = new Dictionary<Node, TreeItem>();
        //private Dictionary<TreeItem, Node> nodesLookupTable = new Dictionary<TreeItem, Node>();
        //private List<Node> parentSceneNodes = new List<Node>();
        private Dictionary<NodeInfo, TreeItem> treeItemsLookupTable = new Dictionary<NodeInfo, TreeItem>();
        private Dictionary<TreeItem, NodeInfo> nodeInfosLookupTable = new Dictionary<TreeItem, NodeInfo>();
        private List<NodeInfo> parentSceneNodeInfos = new List<NodeInfo>();

        public ReparentSceneDialog(string scenePath) : base()
        {
            boundScene = ResourceLoader.Load<PackedScene>(scenePath);
            SceneState state = boundScene.GetState();
            nodeInfos = new NodeInfo[state.GetNodeCount()];
            for (int i = 0; i < nodeInfos.Length; i++)
            {
                NodeInfo nodeInfo = new NodeInfo();
                nodeInfo.groups = state.GetNodeGroups(i);
                nodeInfo.index = state.GetNodeIndex(i); 
                nodeInfo.instance = state.GetNodeInstance(i);
                nodeInfo.instancePlaceholder = state.GetNodeInstancePlaceholder(i);
                nodeInfo.name = state.GetNodeName(i);
                nodeInfo.ownerPath = state.GetNodeOwnerPath(i);
                nodeInfo.path = state.GetNodePath(i);

                nodeInfo.porperties = new Dictionary<StringName, Variant>();
                for (int j = 0; j < state.GetNodePropertyCount(i); j++)
                {
                    nodeInfo.porperties.Add(state.GetNodePropertyName(i, j), state.GetNodePropertyValue(i, j));
                }

                nodeInfo.isInstancePlaceHolder = state.IsNodeInstancePlaceholder(i);

                nodeInfos[i] = nodeInfo;
            }

            boundSceneRoot = boundScene.Instantiate<Node>(PackedScene.GenEditState.Instance);
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            this.Title = "Select the nodes to extract to the new parent scene";
            this.Confirmed += ReparentScene;
            
            Panel panel = dialagContentPanelScn.Instantiate<Panel>();

            sceneTree = panel.GetNode<Tree>("%SceneTree"); 
            sceneTree.ItemSelected += OnItemSelected;
            //sceneTree.ButtonClicked += OnTreeItemClicked;

            foreach(NodeInfo nodeInfo in nodeInfos)
            {
                NodeInfo parentNodeInfo = FindParentNodeInfo(nodeInfo);
                if (parentNodeInfo == null) parentSceneNodeInfos.Add(parentNodeInfo);

                TreeItem parentItem = parentNodeInfo != null ? treeItemsLookupTable[parentNodeInfo] : null;
                TreeItem item = sceneTree.CreateItem(parentItem);
                item.SetText(0, nodeInfo.name);
                item.SetIcon(0, GetThemeIcon(IconNameFromNode(nodeInfo.GetType()), "EditorIcons"));
                if (nodeInfo.instance != null) item.SetIcon(1, GetThemeIcon("PackedScene", "EditorIcons"));

                treeItemsLookupTable.Add(nodeInfo, item);
                nodeInfosLookupTable.Add(item, nodeInfo);
            }
            //List<Node> nodes = FlattenBoundSceneTree(boundSceneRoot);

            //foreach (Node node in nodes)
            //{
            //    Node parentNode = node.GetParent();
            //    if (parentNode == null) parentSceneNodes.Add(node);

            //    string name = node.Name;
            //    bool isInstance = node.GetSceneInstanceLoadPlaceholder();
            //    TreeItem parentItem = parentNode != null ? treeItemsLookupTable[parentNode] : null;

            //    TreeItem treeItem = sceneTree.CreateItem(parentItem);
            //    treeItem.SetText(0, name);
            //    treeItem.SetIcon(0, GetThemeIcon(IconNameFromNode(node), "EditorIcons"));
            //    if (isInstance)
            //    {
            //        treeItem.SetText(1, "Instanced");
            //        treeItem.SetIcon(1, GetThemeIcon("PackedScene", "EditorIcons"));
            //        instancedSceneNodes.Add(node);
            //    }
            //    else
            //    {
            //        treeItem.SetText(1, "Not Instanced");
            //    }

            //    treeItemsLookupTable.Add(node, treeItem);
            //    nodesLookupTable.Add(treeItem, node);
            //}

            UpdateTree();

            this.AddChild(panel);
        }

        private NodeInfo FindParentNodeInfo(NodeInfo nodeInfo)
        {
            int nameCount = nodeInfo.path.GetNameCount();
            if (nameCount == 1) return null;

            StringName parentName = nodeInfo.path.GetName(nameCount - 2);
            return FindNodeInfoByName(parentName);
        }

        private List<NodeInfo> FindAncestorsNodeInfo(NodeInfo nodeInfo)
        {
            List<NodeInfo> ancestors = new List<NodeInfo>();

            int nameCount = nodeInfo.path.GetNameCount();
            if (nameCount == 1) return ancestors;

            for (int i = nameCount - 2; i > -1; i--)
            {
                ancestors.Add(FindNodeInfoByName(nodeInfo.path.GetName(i)));
            }

            return ancestors;
        }

        private List<NodeInfo> FindDescendantsNodeInfo(NodeInfo nodeInfo)
        {
            List<NodeInfo> descendant = new List<NodeInfo>();

            foreach(NodeInfo candidate in nodeInfos)
            {
                if (IsDescendantNodeInfo(candidate, nodeInfo))
                    descendant.Add(candidate);
            }

            return descendant;
        }

        //TODO optimize
        private bool IsDescendantNodeInfo(NodeInfo candidate, NodeInfo parent)
        {
            List<NodeInfo> ancestors = FindAncestorsNodeInfo(candidate);
            foreach (NodeInfo node in ancestors)
            {
                if (parent == node) return true;
            }

            return false;
        }

        private NodeInfo FindNodeInfoByName(StringName name)
        {
            foreach(NodeInfo nodeInfo in nodeInfos)
            {
                if (nodeInfo.name == name) return nodeInfo;
            }

            return null;
        }

        private void OnItemSelected()
        {
            NodeInfo nodeInfo = nodeInfosLookupTable[sceneTree.GetSelected()];
            List<NodeInfo> ancestors = FindAncestorsNodeInfo(nodeInfo);
            List<NodeInfo> desendants = FindDescendantsNodeInfo(nodeInfo);

            foreach (NodeInfo ancestor in ancestors)
            {
                if (!parentSceneNodeInfos.Contains(ancestor))
                    parentSceneNodeInfos.Add(ancestor);
            }

            foreach (NodeInfo descendant in desendants)
            {
                parentSceneNodeInfos.Remove(descendant);
            }

            UpdateTree();
        }

        private void UpdateTree()
        {
            foreach(TreeItem item in nodeInfosLookupTable.Keys)
            {
                if (parentSceneNodeInfos.Contains(nodeInfosLookupTable[item]))
                {
                    item.SetCustomColor(0, Color.FromString("yellow", new Color()));
                }
                else
                {
                    item.SetCustomColor(0, Color.FromString("white", new Color()));
                }
            }
        }

        //private void OnTreeItemClicked(TreeItem item, long column, long id, long mouseButtonIndex)
        //{
        //    NodeInfo node = nodeInfosLookupTable[item];
        //    List<Node> ancestors = NodeUtils.FindNodesInParents<Node>(node, true);
        //    List<Node> desendants = NodeUtils.FindNodesInChildren<Node>(node, false);

        //    foreach (Node ancestor in ancestors)
        //    {
        //        if (!parentSceneNodeInfos.Contains(ancestor))
        //            parentSceneNodeInfos.Add(ancestor);
        //    }

        //    foreach (Node descendant in desendants)
        //    {
        //        parentSceneNodeInfos.Remove(descendant);
        //    }

        //    UpdateTree();
        //}

        //private Texture2D GetThemeIcon(StringName name, StringName themeType = null)
        //{
        //    return EditorInterface.Singleton.GetBaseControl().GetThemeIcon(name, themeType);
        //}

        private string IconNameFromNode(Type type)
        {
            string iconName = type.ToString();
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
