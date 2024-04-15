using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TurboTartine.ReparentScenePlugin
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
        public StringName type;
        public bool isInstancePlaceHolder;
    }

    public partial class ExtractParentDialog : ConfirmationDialog
    {
        private Color yellowColor = Color.FromString("yellow", new Color());
        private Color whiteColor = Color.FromString("white", new Color());

        private PackedScene dialagContentPanelScn = GD.Load<PackedScene>("res://addons/tt-godot-reparent-scene-plugin/ExtractParentDialogContent.tscn");
        private PackedScene boundScene;
        private string boundScenePath;
        private LineEdit originalScnPathLineEdit;
        private Button selectOriginScnBtn;
        private CheckBox backupCheckBox;
        private Tree sceneTree;
        private List<NodeInfo> nodeInfos = new List<NodeInfo>();

        private Dictionary<NodeInfo, TreeItem> treeItemsLookupTable = new Dictionary<NodeInfo, TreeItem>();
        private Dictionary<TreeItem, NodeInfo> nodeInfosLookupTable = new Dictionary<TreeItem, NodeInfo>();
        private List<NodeInfo> parentSceneNodeInfos = new List<NodeInfo>();

        private void InitFromPath(string scenePath)
        {
            nodeInfos.Clear();

            boundScenePath = scenePath;
            boundScene = ResourceLoader.Load<PackedScene>(scenePath);
            SceneState state = boundScene.GetState();
            nodeInfos = new List<NodeInfo>();
            for (int i = 0; i < state.GetNodeCount(); i++)
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

                nodeInfo.type = state.GetNodeType(i);
                nodeInfo.isInstancePlaceHolder = state.IsNodeInstancePlaceholder(i);

                nodeInfos.Add(nodeInfo);
            }

            SetupTree();
            UpdateTree();
            UpdateOkButton();
        }

        private void SetupTree()
        {
            sceneTree.Clear();
            treeItemsLookupTable.Clear();
            nodeInfosLookupTable.Clear();
            parentSceneNodeInfos.Clear();

            foreach (NodeInfo nodeInfo in nodeInfos)
            {
                NodeInfo parentNodeInfo = FindParentNodeInfo(nodeInfo);
                if (parentNodeInfo == null) parentSceneNodeInfos.Add(nodeInfo);

                TreeItem parentItem = parentNodeInfo != null ? treeItemsLookupTable[parentNodeInfo] : null;
                TreeItem item = sceneTree.CreateItem(parentItem);
                item.SetText(0, nodeInfo.name);
                item.SetIcon(0, GetThemeIcon(IconNameFromNode(nodeInfo.type), "EditorIcons"));
                item.SetIconModulate(2, yellowColor);

                treeItemsLookupTable.Add(nodeInfo, item);
                nodeInfosLookupTable.Add(item, nodeInfo);
            }
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            this.Title = Plugin.EXTRACT_PARENT_MENU_ITEM_NAME;
            this.Confirmed += ExtractParent;

            Panel panel = dialagContentPanelScn.Instantiate<Panel>();

            backupCheckBox = panel.GetNode<CheckBox>("%BackupCheckBox");
            backupCheckBox.SetPressedNoSignal(ProjectSettings.GetSetting(Plugin.PROJECT_SETTING_DEFAULT_BACKUP_ORIGINAL).AsBool());
            originalScnPathLineEdit = panel.GetNode<LineEdit>("%OriginScnPathLineEdit");
            selectOriginScnBtn = panel.GetNode<Button>("%SelectOriginScnBtn");
            selectOriginScnBtn.Pressed += OnClickSelect;

            sceneTree = panel.GetNode<Tree>("%SceneTree");
            sceneTree.SetColumnExpand(0, true);
            sceneTree.SetColumnExpand(1, false);
            sceneTree.SetColumnExpand(2, false);
            sceneTree.ItemSelected += OnItemSelected;

            this.AddChild(panel);

            UpdateOkButton();
        }

        private void UpdateOkButton()
        {
            GetOkButton().Disabled = nodeInfos.Count == 0;
        }

        private void OnClickSelect()
        {
            EditorFileDialog dialog = new EditorFileDialog();
            dialog.Title = "Choose a scene you want to extract a parent";
            dialog.Filters = new string[] { "*.tscn" };
            dialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
            dialog.FileSelected += OnOriginalSceneSelected;
            EditorInterface.Singleton.PopupDialogCentered(dialog, new Vector2I(500, 500));
        }

        private void OnOriginalSceneSelected(string path)
        {
            originalScnPathLineEdit.Text = path;
            InitFromPath(path);
        }

        private NodeInfo FindParentNodeInfo(NodeInfo nodeInfo)
        {
            int nameCount = nodeInfo.path.GetNameCount();
            if (nameCount == 1) return null;

            string pathString = "";
            for (int i = 0; i < nameCount - 1; i++) pathString += nodeInfo.path.GetName(i) + "/";
            pathString = pathString.Remove(pathString.Length - 1);

            NodeInfo parentNodeInfo = FindNodeInfoByPath(pathString);
            if (parentNodeInfo == null) parentNodeInfo = FindNodeInfoByPath(nodeInfo.ownerPath);

            return parentNodeInfo;
        }

        private List<NodeInfo> FindAncestorsNodeInfo(NodeInfo nodeInfo)
        {
            List<NodeInfo> ancestors = new List<NodeInfo>();

            int nameCount = nodeInfo.path.GetNameCount();
            if (nameCount == 1) return ancestors;

            string pathString = "";
            for (int i = 0; i < nameCount - 1; i++)
            {
                pathString += nodeInfo.path.GetName(i);
                ancestors.Insert(0, FindNodeInfoByPath(pathString));
                pathString += "/";
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

        private NodeInfo FindNodeInfoByPath(string stringPath)
        {
            foreach (NodeInfo nodeInfo in nodeInfos)
            {
                if (nodeInfo.path.GetConcatenatedNames() == stringPath) return nodeInfo;
            }

            return null;
        }

        private void UpdateTree()
        {
            foreach(TreeItem item in nodeInfosLookupTable.Keys)
            {
                if (parentSceneNodeInfos.Contains(nodeInfosLookupTable[item]))
                {
                    item.SetCustomColor(0, yellowColor);
                    item.SetIcon(2, GetThemeIcon("Node", "EditorIcons"));
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

        private void OnItemSelected()
        {
            NodeInfo nodeInfo = nodeInfosLookupTable[sceneTree.GetSelected()];
            ToggleNode(nodeInfo);
        }

        private void ToggleNode(NodeInfo nodeInfo)
        {
            List<NodeInfo> ancestors = FindAncestorsNodeInfo(nodeInfo);
            List<NodeInfo> desendants = FindDescendantsNodeInfo(nodeInfo);

            foreach (NodeInfo ancestor in ancestors)
            {
                if (ancestor != null && !parentSceneNodeInfos.Contains(ancestor))
                    parentSceneNodeInfos.Add(ancestor);
            }

            if (!parentSceneNodeInfos.Contains(nodeInfo)) parentSceneNodeInfos.Add(nodeInfo);

            foreach (NodeInfo descendant in desendants)
            {
                parentSceneNodeInfos.Remove(descendant);
            }

            UpdateTree();
        }

        private string IconNameFromNode(string type)
        {
            if (type == "") return "PackedScene";
            return type.Replace("Godot.", "");
        }

        private void ExtractParent()
        {
            string pathNoExtention = boundScene.ResourcePath.GetBaseName();
            string extention = boundScene.ResourcePath.GetExtension();
            SceneState boundScnState = boundScene.GetState(); 
            Node boundScnTree = boundScene.Instantiate();

            if (backupCheckBox.ButtonPressed)
            {
                string backupScenePath = pathNoExtention + "_Backup." + extention;
                PackedScene backupscene = (PackedScene)boundScene.Duplicate();
                ResourceSaver.Singleton.Save(backupscene, backupScenePath);
            }

            List<NodeInfo> childSceneNodeInfos = new List<NodeInfo>();
            foreach(NodeInfo nodeInfo in nodeInfos)
            {
                if (!parentSceneNodeInfos.Exists(n => n.path == nodeInfo.path))
                    childSceneNodeInfos.Add(nodeInfo);
            }

            //ExtractParent
            Node parentScnTree = boundScene.Instantiate(PackedScene.GenEditState.MainInherited);
            foreach(NodeInfo childInfo in childSceneNodeInfos)
            {
                Node node = parentScnTree.GetNode(childInfo.path);
                if (node != null) node.Free();
            }
            PackedScene parentPackedScn = new PackedScene();
            parentPackedScn.Pack(parentScnTree);
            string parentScenePath = pathNoExtention + "_Parent." + extention;
            ResourceSaver.Singleton.Save(parentPackedScn, parentScenePath);

            //Override children
            parentPackedScn = ResourceLoader.Load<PackedScene>(parentScenePath);           //Workaround https://github.com/godotengine/godot/issues/27243
            PackedScene childPackedScn = CreateInheridetScene(parentPackedScn);
            string childScenePath = pathNoExtention + "." + extention;
            Node childScnTree = childPackedScn.Instantiate(PackedScene.GenEditState.MainInherited);
            foreach (NodeInfo childInfo in childSceneNodeInfos)
            {
                Node childInBoundScene = boundScnTree.GetNode(childInfo.path);
                Node parentInBoundScn = childInBoundScene.GetParent();
                Node parentInChildScene = childScnTree.GetNode(boundScnTree.GetPathTo(parentInBoundScn));
                Node childInChildScene = childInBoundScene.Duplicate();
                foreach(Node child in childInChildScene.GetChildren()) 
                    childInChildScene.RemoveChild(child);
                parentInChildScene.AddChild(childInChildScene);
                childInChildScene.Owner = childScnTree;
            }
            childPackedScn.Pack(childScnTree);
            DirAccess.RemoveAbsolute(childScenePath);                                      // Changes are not applied if we do not remove the file first
            ResourceSaver.Singleton.Save(childPackedScn, childScenePath);
        }

        private PackedScene CreateInheridetScene(PackedScene baseScene, string rootName = null)
        { 
            if (rootName == null) rootName = baseScene.GetState().GetNodeName(0);
           
            List<string> names = new List<string> { rootName };
            List<Variant> variants = new List<Variant>( new Variant[] { baseScene });
            List<int> nodes = new List<int>(new int[] { -1, -1, 2147483647, 0, -1 } );

            SceneState baseScnState = baseScene.GetState();
            int propsCount = baseScnState.GetNodePropertyCount(0);
            nodes.Add(propsCount);
            for (int i = 0; i < propsCount; i++)
            {
                int nameIdx = names.Count;
                names.Add(baseScnState.GetNodePropertyName(0, i));
                nodes.Add(nameIdx);

                int variantIdx = variants.Count;
                variants.Add(baseScnState.GetNodePropertyValue(0, i));
                nodes.Add(variantIdx);
            }

            int grpsCount = baseScnState.GetNodeGroups(0).Length;
            nodes.Add(grpsCount);
            for(int i = 0; i < grpsCount; i++)
            {
                int nameIdx = names.Count;
                names.Add(baseScnState.GetNodeGroups(0)[i]);
                nodes.Add(nameIdx);
            }

            //TODO Setup connections

            PackedScene inheritedScene = new PackedScene();
            Godot.Collections.Dictionary _bundled = inheritedScene._Bundled;
            _bundled["names"] = names.ToArray();
            _bundled["node_count"] = 1;
            _bundled["nodes"] = nodes.ToArray();
            _bundled["variants"] = new Godot.Collections.Array(variants);
            _bundled.Add("base_scene", 0);
            inheritedScene._Bundled = _bundled;

            return inheritedScene;
        }
    }
}
