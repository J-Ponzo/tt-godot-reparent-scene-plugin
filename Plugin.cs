#if TOOLS

using Godot;
using System;
using System.Diagnostics;

namespace TurboTartine.ReparentScenePlugin
{
    [Tool]
    public partial class Plugin : EditorPlugin
    {
        public const string TOOL_MENU_ITEM_NAME = "TT ReparentScene";
        public const string EXTRACT_PARENT_MENU_ITEM_NAME = "Extract Parent";
        public const string REPARENT_MENU_ITEM_NAME = "Reparent";

        public const string PROJECT_SETTING_DEFAULT_BACKUP_ORIGINAL = "tt_reparent_utils/settings/common/default_backup_original_scene";

        private PopupMenu pluginMenu;

        public override void _EnterTree()
        {
            pluginMenu = new PopupMenu();
            pluginMenu.AddItem(EXTRACT_PARENT_MENU_ITEM_NAME, 0);
            pluginMenu.AddItem(REPARENT_MENU_ITEM_NAME, 1);
            pluginMenu.IdPressed += OnPluginMenuItemPressed;

            AddToolSubmenuItem(TOOL_MENU_ITEM_NAME, pluginMenu);

            AddCustomProjectSettings(PROJECT_SETTING_DEFAULT_BACKUP_ORIGINAL, true);
        }

        private void OnPluginMenuItemPressed(long id)
        {
            if (id == 0) OpenExtractParentDialog();
            else if(id == 1) OpenReparentDialog();
        }

        private void OpenReparentDialog()
        {
            ReparentDialog dialog = new ReparentDialog();
            EditorInterface.Singleton.PopupDialogCentered(dialog, new Vector2I(750, 500));
        }

        private void OpenExtractParentDialog()
        {
            ExtractParentDialog dialog = new ExtractParentDialog();
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