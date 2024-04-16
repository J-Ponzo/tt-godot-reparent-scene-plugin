using Godot;
using System;

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
        private SceneTreeHandler originTreeHandler;
        private Tree newParentScnTree;
        private SceneTreeInfo newParentTreeInfo;
        private SceneTreeHandler newParentTreeHandler;

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
            originTreeHandler = new SceneTreeHandler(originScnTree, originTreeInfo);
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
            newParentTreeHandler = new SceneTreeHandler(newParentScnTree, newParentTreeInfo);
        }

        private void Reparent()
        {
            GD.Print("Reparent");
        }
    }
}
