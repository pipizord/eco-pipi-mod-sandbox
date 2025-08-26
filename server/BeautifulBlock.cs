using Eco.Gameplay.Items;
using Eco.Mods.TechTree;
using Eco.Shared.Localization;
using Eco.Shared.Serialization;
using Eco.World.Blocks;

namespace Eco.Client
{
    [Serialized]
    [Solid, Constructed, Wall]
    public class BeautifulBlock : Block, IRepresentsItem
    {
        public Type RepresentedItemType => typeof(BeautifulBlockItem);

    }

    [Serialized]
    [MaxStackSize(50)]
    [Weight(50)]
    public partial class BeautifulBlockItem : BlockItem<BeautifulBlock> 
    {
        public override LocString DisplayName => Localizer.DoStr("Beautiful Block");
    }

    [Serialized]
    [Solid, Constructed, Wall]
    [IsForm(typeof(WallFormType), typeof(BeautifulBlockItem))]
    public partial class BeautifulWallBlock : Block, IRepresentsItem
    {
        public Type RepresentedItemType => typeof(BeautifulBlockItem);
    }

    [Serialized]
    [Solid, Constructed, Wall]
    [IsForm(typeof(CubeFormType), typeof(BeautifulBlockItem))]
    public partial class BeautifulCubeBlock : Block, IRepresentsItem
    {
        public Type RepresentedItemType => typeof(BeautifulBlockItem);
    }
}
