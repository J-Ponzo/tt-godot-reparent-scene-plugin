using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class ReparentDialog : ConfirmationDialog
    {
        private PackedScene dialagContentPanelScn = GD.Load<PackedScene>("res://addons/tt-godot-reparent-scene-plugin/ReparentDialogContent.tscn");

        private LineEdit originScnPathLineEdit;
        private Button selectOriginScnBtn;
        private LineEdit newParentScnPathLineEdit;
        private Button selectNewParentScnBtn;
        private CheckBox backupCheckBox;
        private Tree originScnTree;
        private SceneTreeInfo originTreeInfo;
        private ReparentedTreeHandler originTreeHandler;
        private Tree newParentScnTree;
        private SceneTreeInfo newParentTreeInfo;
        private NewParentTreeHandler newParentTreeHandler;

        public override void _EnterTree()
        {
            base._EnterTree();
            this.Title = Plugin.REPARENT_MENU_ITEM_NAME;
            this.Confirmed += Reparent;

            Panel panel = dialagContentPanelScn.Instantiate<Panel>();

            originScnPathLineEdit = panel.GetNode<LineEdit>("%OriginScnPathLineEdit");
            selectOriginScnBtn = panel.GetNode<Button>("%SelectOriginScnBtn");
            selectOriginScnBtn.Pressed += OnClickSelectOriginScn;

            newParentScnPathLineEdit = panel.GetNode<LineEdit>("%NewParentScnPathLineEdit");
            selectNewParentScnBtn = panel.GetNode<Button>("%SelectNewParentScnBtn");
            selectNewParentScnBtn.Pressed += OnClickSelectNewParentScn;

            backupCheckBox = panel.GetNode<CheckBox>("%BackupCheckBox");
            backupCheckBox.SetPressedNoSignal(ProjectSettings.GetSetting(Plugin.PROJECT_SETTING_DEFAULT_BACKUP_ORIGINAL).AsBool());

            originScnTree = panel.GetNode<Tree>("%OriginalScnTree");
            newParentScnTree = panel.GetNode<Tree>("%NewParentScnTree");

            this.AddChild(panel);

            UpdateOkButton();
        }

        private void OnClickSelectOriginScn()
        {
            EditorFileDialog dialog = new EditorFileDialog();
            dialog.Title = "Choose a scene you want to reparent";
            dialog.Filters = new string[] { "*.tscn" };
            dialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
            dialog.FileSelected += OnOriginScnSelected;
            EditorInterface.Singleton.PopupDialogCentered(dialog, new Vector2I(500, 500));
        }

        private void OnOriginScnSelected(string path)
        {
            originScnPathLineEdit.Text = path;
            originTreeInfo = new SceneTreeInfo(path);
            originTreeHandler = new ReparentedTreeHandler(originScnTree, originTreeInfo);
            UpdateAll();
        }

        private void OnClickSelectNewParentScn()
        {
            EditorFileDialog dialog = new EditorFileDialog();
            dialog.Title = "Choose a scene you want to be the new parent";
            dialog.Filters = new string[] { "*.tscn" };
            dialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
            dialog.FileSelected += OnNewParentScnSelected;
            EditorInterface.Singleton.PopupDialogCentered(dialog, new Vector2I(500, 500));
        }

        private void OnNewParentScnSelected(string path)
        {
            newParentScnPathLineEdit.Text = path;
            newParentTreeInfo = new SceneTreeInfo(path);
            newParentTreeHandler = new NewParentTreeHandler(newParentScnTree, newParentTreeInfo);
            UpdateAll();
        }

        private void UpdateAll()
        {
            if (newParentTreeHandler != null) newParentTreeHandler.UpdateTree(originTreeInfo);
            if (originTreeHandler != null) originTreeHandler.UpdateTree(newParentTreeInfo);
            UpdateOkButton();
        }

        private void UpdateOkButton()
        {
            GetOkButton().Disabled = !(newParentTreeHandler != null && newParentTreeHandler.IsValidParent());
        }

        private void Reparent()
        {
            Debugger.Launch();
            string pathNoExtention = originTreeInfo.boundScene.ResourcePath.GetBaseName();
            string extention = originTreeInfo.boundScene.ResourcePath.GetExtension();
            SceneState reparentScnState = originTreeInfo.boundScene.GetState();
            Node originScnTree = originTreeInfo.boundScene.Instantiate();

            if (backupCheckBox.ButtonPressed)
            {
                string backupScenePath = pathNoExtention + "_Backup." + extention;
                PackedScene backupscene = (PackedScene)originTreeInfo.boundScene.Duplicate();
                ResourceSaver.Singleton.Save(backupscene, backupScenePath);
            }

            //TODO Extract to fonction to factorise with ExtractParentDialog logic
            List<SceneTreeInfo.NodeInfo> childSceneNodeInfos = new List<SceneTreeInfo.NodeInfo>();
            foreach (SceneTreeInfo.NodeInfo nodeInfo in originTreeInfo.nodeInfos)
            {
                if (!newParentTreeInfo.nodeInfos.Exists(n => n.path == nodeInfo.path))
                    childSceneNodeInfos.Add(nodeInfo);
            }

            PackedScene newParentScn = ResourceLoader.Load<PackedScene>(newParentTreeInfo.boundScenePath);           //Workaround https://github.com/godotengine/godot/issues/27243
            PackedScene reparentedScn = CreateInheridetScene(newParentScn);
            string reparentedScenePath = pathNoExtention + "." + extention;
            Node reparentedScnTree = reparentedScn.Instantiate(PackedScene.GenEditState.MainInherited);
            foreach (SceneTreeInfo.NodeInfo childInfo in childSceneNodeInfos)
            {
                Node childInOriginScene = originScnTree.GetNode(childInfo.path);
                Node parentInOriginScn = childInOriginScene.GetParent();
                Node parentInNewParentScene = reparentedScnTree.GetNode(originScnTree.GetPathTo(parentInOriginScn));
                Node childInNewParentScene = childInOriginScene.Duplicate();
                foreach (Node child in childInNewParentScene.GetChildren())
                    childInNewParentScene.RemoveChild(child);
                parentInNewParentScene.AddChild(childInNewParentScene);
                childInNewParentScene.Owner = reparentedScnTree;
            }
            reparentedScn.Pack(reparentedScnTree);
            DirAccess.RemoveAbsolute(reparentedScenePath);                                      // Changes are not applied if we do not remove the file first
            ResourceSaver.Singleton.Save(reparentedScn, reparentedScenePath);
        }

        //TODO Extract to fonction to factorise with ExtractParentDialog logic
        private PackedScene CreateInheridetScene(PackedScene baseScene, string rootName = null)
        {
            if (rootName == null) rootName = baseScene.GetState().GetNodeName(0);

            List<string> names = new List<string> { rootName };
            List<Variant> variants = new List<Variant>(new Variant[] { baseScene });
            List<int> nodes = new List<int>(new int[] { -1, -1, 2147483647, 0, -1 });

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
            for (int i = 0; i < grpsCount; i++)
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
