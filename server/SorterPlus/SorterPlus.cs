namespace PipiModSandbox.SorterPlus
{
    using Eco.Core.Controller;
    using Eco.Core.Items;
    using Eco.Core.Systems;
    using Eco.Core.Utils;
    using Eco.Gameplay.Components;
    using Eco.Gameplay.Components.Auth;
    using Eco.Gameplay.Components.Storage;
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Objects;
    using Eco.Gameplay.Utils;
    using Eco.Shared.Localization;
    using Eco.Shared.Logging;
    using Eco.Shared.Networking;
    using Eco.Shared.Serialization;
    using Eco.Shared.Utils;
    using Eco.Simulation.Time;
    using System.ComponentModel;
    using System.Diagnostics;


    public static class Constants
    {
        public const string DISPLAY_NAME = "Sorter Plus";
        public const int INTERVAL_IN_SECONDS = 30;
        public const bool SHOULD_LOG = true; 
    }

    [Serialized]
    [MaxStackSize(1)]
    [LocDisplayName(Constants.DISPLAY_NAME)]
    [LocDescription("Configure sorter items to sort out")]
    public class SorterPlusItem : WorldObjectItem<SorterPlusObject>
    {
    
    }

    [Serialized]
    [RequireComponent(typeof(OnOffComponent), null)]
    [RequireComponent(typeof(SorterPlusComponent))]
    [RequireComponent(typeof(MustBeOwnedComponent))]
    [RequireComponent(typeof(PropertyAuthComponent))]
    public class SorterPlusObject : WorldObject
    {
        public override LocString DisplayName => Localizer.DoStr(Constants.DISPLAY_NAME);
    }

    [Serialized, AutogenClass, LocDisplayName(Constants.DISPLAY_NAME)]
    [Tag("Table"), Category("Hidden"), HasIcon(null)]
    [RequireComponent(typeof(InOutLinkedInventoriesComponent), null)]
    [RequireComponent(typeof(SharedLinkComponent), null)]
    public class SorterPlusComponent : WorldObjectComponent, IHasClientControlledContainers, IHasUniversalID
    {

        [SyncToView, Autogen, AutoRPC]
        [GuestHidden, AllowEmpty]
        public ControllerList<Item> SortingList { get; set; }

        private SharedLinkComponent linkComponent;


        [Eco, ClientInterfaceProperty, GuestHidden]
        public string SortingName { get; set; } = "Default Sorting";

        [SyncToView, Autogen, AutoRPC, Serialized]
        public float Teste { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            linkComponent = Parent.GetComponent<SharedLinkComponent>(null);
            linkComponent.Initialize(7f);
        }

        public SorterPlusComponent()
        {
            SortingList = new ControllerList<Item>(this, "SortingList");
        }

        private void SortItems() {
            try
            {
                // Initial validations to avoid unecessary processing
                if (!Parent.Enabled || !SortingList.AnyNotNull())
                    return;

                // Cache dos componentes para evitar múltiplas chamadas
                var sourceComponentList = linkComponent
                    .GetSortedLinkedComponents(Parent.Creator, true, false)
                    .Where(c => !c.Parent.HasComponent<VehicleComponent>() && c.Inventory != null)
                    .ToList();

                var targetComponentList = linkComponent
                    .GetSortedLinkedComponents(Parent.Creator, false, true)
                    .Where(c => !c.Parent.HasComponent<VehicleComponent>() && c.Inventory != null)
                    .ToList();

                // Early return if there are no valid components
                if (!sourceComponentList.Any() || !targetComponentList.Any())
                    return;

                // Pre processing of SortingList to a more efficient lookup
                var sortingTypes = new HashSet<Type>(
                    SortingList
                        .Where(item => item?.Type != null)
                        .Select(item => item.Type)
                );

                // main loop
                foreach (var inputComponent in sourceComponentList)
                {
                    var inputInventory = inputComponent.Inventory;

                    foreach (var outputComponent in targetComponentList)
                    {
                        // skip if component is the same
                        if (inputComponent == outputComponent)
                            continue;

                        var outputInventory = outputComponent.Inventory;

                        // Only process types on sorting list
                        foreach (var stack in inputInventory.NonEmptyStacks)
                        {
                            if (stack?.Item?.Type == null || !sortingTypes.Contains(stack.Item.Type))
                                continue;

                            // calculate ammount to move
                            var maxAccepted = outputInventory.GetMaxAcceptedVal(stack.Item, stack.Quantity);
                            var quantityToMove = Math.Min(maxAccepted, stack.Quantity);

                            if (quantityToMove > 0 && inputInventory.TryMoveItems<int>(stack.Item.Type, quantityToMove, outputInventory, null, null, null).Success)
                            {
                                // early return when successfull
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger($"error during Sorter execution {ex.Message}");
                Logger($"statcktrace {ex.StackTrace}");
            }
        }

        private double lastRunTime;
        Stopwatch stopwatch = new Stopwatch();
        public override void Tick()
        {
            // only runs every X seconds
            if (WorldTime.Seconds - lastRunTime < Constants.INTERVAL_IN_SECONDS)
                return;

            lastRunTime = WorldTime.Seconds;

            stopwatch.Start();
            SortItems();
            stopwatch.Stop();
            Logger($"sorter took {stopwatch.ElapsedMilliseconds} milliseconds to run");
        }

        private void Logger(string message) {
            if (Constants.SHOULD_LOG) {
                Log.WriteLine(Localizer.DoStr($"[CUSTOM_LOG] {message}"));
            }   
        }
    }
}
