#if TOOLS

using Godot;
using System;
using System.Diagnostics;

namespace TurboTartine.Godot.ReparentScenePlugin
{
    [Tool]
    public partial class Plugin : EditorPlugin
    {
        private const string TOOL_MENU_ITEM_NAME = "TT ReparentScene";
        private const string CREATE_ENTITY_MENU_ITEM_NAME = "Open";

        private PopupMenu pluginMenu;

        public override void _EnterTree()
        {
            pluginMenu = new PopupMenu();
            pluginMenu.AddItem(CREATE_ENTITY_MENU_ITEM_NAME, 0);
            pluginMenu.IdPressed += OnPluginMenuItemPressed;

            AddToolSubmenuItem(TOOL_MENU_ITEM_NAME, pluginMenu);
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

        public override void _ExitTree()
        {
            pluginMenu.IdPressed -= OnPluginMenuItemPressed;
            RemoveToolMenuItem(TOOL_MENU_ITEM_NAME);
        }
    }
}
#endif