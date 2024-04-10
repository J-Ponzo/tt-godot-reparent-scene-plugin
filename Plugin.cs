#if TOOLS

using Godot;
using System;

namespace TurboTartine.Godot.ReparentScene
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

        private void OpenReparentSceneDialog()
        {
            GD.Print("OpenReparentSceneDialog");
        }
    }
}
#endif