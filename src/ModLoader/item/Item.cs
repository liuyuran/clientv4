namespace ModLoader.item;

/// <summary>
/// the item need composition with other class.
/// for example, if a player wants to equip an item, the program needs to use Equipment.IsEquipable to check if the item is equipable.
/// </summary>
public abstract class Item {
    public virtual string name => throw new System.NotImplementedException();
    public virtual string iconPath => throw new System.NotImplementedException();
    public virtual long maxStack => 64;
    public long stackCount { get; set; } = 1;
    public readonly Dictionary<string, object> Config = new();

    public Item Clone() {
        return (Item)this.MemberwiseClone();
    }
}