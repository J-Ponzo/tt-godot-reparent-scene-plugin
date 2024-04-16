using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TurboTartine.ReparentScenePlugin
{
    //Godot icons https://github.com/godotengine/godot/tree/master/editor/icons
   
    public partial class ExtractParentDialog : ConfirmationDialog
    {
        private PackedScene dialagContentPanelScn = GD.Load<PackedScene>("res://addons/tt-godot-reparent-scene-plugin/ExtractParentDialogContent.tscn");
        private LineEdit originalScnPathLineEdit;
        private Button selectOriginScnBtn;
        private CheckBox backupCheckBox;
        private SceneTreeInfo treeInfo;
        private Tree sceneTree;
        private SceneTreeHandler treeHandler;

        private void InitFromPath(string scenePath)
        {
            treeInfo = new SceneTreeInfo(scenePath);
            treeHandler = new SceneTreeHandler(sceneTree, treeInfo);
            UpdateOkButton();
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
            GetOkButton().Disabled = treeInfo == null || !treeInfo.IsValid();
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

        private void OnItemSelected()
        {
            treeHandler.ToggleNode(sceneTree.GetSelected());
        }

        private void ExtractParent()
        {
            string pathNoExtention = treeInfo.boundScene.ResourcePath.GetBaseName();
            string extention = treeInfo.boundScene.ResourcePath.GetExtension();
            SceneState boundScnState = treeInfo.boundScene.GetState(); 
            Node boundScnTree = treeInfo.boundScene.Instantiate();

            if (backupCheckBox.ButtonPressed)
            {
                string backupScenePath = pathNoExtention + "_Backup." + extention;
                PackedScene backupscene = (PackedScene)treeInfo.boundScene.Duplicate();
                ResourceSaver.Singleton.Save(backupscene, backupScenePath);
            }

            List<SceneTreeInfo.NodeInfo> childSceneNodeInfos = new List<SceneTreeInfo.NodeInfo>();
            foreach(SceneTreeInfo.NodeInfo nodeInfo in treeInfo.nodeInfos)
            {
                if (!treeHandler.ParentSceneNodeInfos.Exists(n => n.path == nodeInfo.path))
                    childSceneNodeInfos.Add(nodeInfo);
            }

            //ExtractParent
            Node parentScnTree = treeInfo.boundScene.Instantiate(PackedScene.GenEditState.MainInherited);
            foreach(SceneTreeInfo.NodeInfo childInfo in childSceneNodeInfos)
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
            foreach (SceneTreeInfo.NodeInfo childInfo in childSceneNodeInfos)
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
