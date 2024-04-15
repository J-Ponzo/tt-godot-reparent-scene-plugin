#if TOOLS

using Godot;
using System;
using System.Diagnostics;

namespace TurboTartine.ReparentScenePlugin
{
    [Tool]
    public partial class Plugin : EditorPlugin
    {
        private const string TOOL_MENU_ITEM_NAME = "TT ReparentScene";
        private const string CREATE_ENTITY_MENU_ITEM_NAME = "Extract Parent";

        public const string PROJECT_SETTING_DEFAULT_BACKUP_ORIGINAL = "tt_reparent_utils/settings/common/default_backup_original_scene";

        private PopupMenu pluginMenu;

        public override void _EnterTree()
        {
            pluginMenu = new PopupMenu();
            pluginMenu.AddItem(CREATE_ENTITY_MENU_ITEM_NAME, 0);
            pluginMenu.IdPressed += OnPluginMenuItemPressed;

            AddToolSubmenuItem(TOOL_MENU_ITEM_NAME, pluginMenu);

            AddCustomProjectSettings(PROJECT_SETTING_DEFAULT_BACKUP_ORIGINAL, true);
        }

        private void OnPluginMenuItemPressed(long id)
        {
            if (id == 0) OpenReparentSceneDialog();
        }

        EditorFileDialog selectSceneToReparentdialog;
        private void OpenReparentSceneDialog()
        {
            selectSceneToReparentdialog = new EditorFileDialog();
            selectSceneToReparentdialog.Title = "Choose a scene to reparent";
            selectSceneToReparentdialog.Filters = new string[] { "*.tscn" };
            selectSceneToReparentdialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
            selectSceneToReparentdialog.FileSelected += OnSceneToReparentSelected;

            EditorInterface.Singleton.PopupDialogCentered(selectSceneToReparentdialog, new Vector2I(500, 500));
        }

        private void OnSceneToReparentSelected(string path)
        {
            ReparentSceneDialog dialog = new ReparentSceneDialog(path);
            EditorInterface.Singleton.PopupDialogCentered(dialog, new Vector2I(500, 500));
        }

        private void AddCustomProjectSettings(string name, Variant value)
        {
            if (ProjectSettings.HasSetting(name)) return;

            ProjectSettings.SetSetting(name, value);
        }

        public override void _ExitTree()
        {
            pluginMenu.IdPressed -= OnPluginMenuItemPressed;
            RemoveToolMenuItem(TOOL_MENU_ITEM_NAME);
        }
    }
}
#endif