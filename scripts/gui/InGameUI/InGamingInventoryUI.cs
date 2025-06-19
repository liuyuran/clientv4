namespace game.scripts.gui.InGameUI;

public partial class InGamingUI {
    private void InitializeInventory() {
        _inventory.Visible = false;
    }
    
    private void HideInventory() {
        _inventory.Visible = false;
    }
    
    private void ShowInventory() {
        _inventory.Visible = true;
        // TODO load data
    }
}