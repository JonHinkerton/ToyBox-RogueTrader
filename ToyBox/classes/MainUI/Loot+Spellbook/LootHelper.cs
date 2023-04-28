﻿using Kingmaker;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._VM.Loot;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using ModKit;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utils;
using Owlcat.Runtime.UI.Controls.Button;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ToyBox {
    public static class LootHelper {
        public static string NameAndOwner(this ItemEntity u, bool showRating) =>
            (showRating ? $"{u.Rating()} ".orange().bold() : "")
            + (u.Owner != null ? $"({u.Owner.CharacterName}) ".orange() : "")
            + u.Name;
        public static string NameAndOwner(this ItemEntity u) => u.NameAndOwner(Main.Settings.showRatingForEnchantmentInventoryItems);
        public static bool IsLootable(this ItemEntity item, RarityType filter = RarityType.None) {
            var rarity = item.Rarity();
            if ((int)rarity < (int)filter) return false;
            return item.IsLootable;
        }
        public static List<ItemEntity> Lootable(this List<ItemEntity> loots, RarityType filter = RarityType.None) => loots.Where(l => l.IsLootable(filter)).ToList();
        public static string GetName(this LootWrapper present) {
            if (present.InteractionLoot != null) {
                //                var name = present.InteractionLoot.Owner.View.name;
                var name = present.InteractionLoot.Source.name;
                if (name == null || name.Length == 0) name = "Ground";
                return name;
            }
            if (present.Unit != null) return present.Unit.CharacterName;
            return null;
        }

        public static List<ItemEntity> GetInteraction(this LootWrapper present) {
            if (present.InteractionLoot != null) return present.InteractionLoot.Loot.Items;
            if (present.Unit != null) return present.Unit.Inventory.Items;
            return null;
        }
        public static IEnumerable<ItemEntity> Search(this IEnumerable<ItemEntity> items, string searchText) => items.Where(i => searchText.Length > 0 ? i.Name.ToLower().Contains(searchText.ToLower()) : true);
        public static List<ItemEntity> GetLewtz(this LootWrapper present, string searchText = "") {
            if (present.InteractionLoot != null) return present.InteractionLoot.Loot.Items.Search(searchText).ToList();
            if (present.Unit != null) return present.Unit.Inventory.Items.Search(searchText).ToList();
            return null;
        }
        public static IEnumerable<LootWrapper> GetMassLootFromCurrentArea() {
            List<LootWrapper> lootWrapperList = new();
            var units = Game.Instance.State.Units.All
                .Where<UnitEntityData>((Func<UnitEntityData, bool>)(u => u.IsInGame && !u.Descriptor.IsPartyOrPet()));
            //.Where<UnitEntityData>((Func<UnitEntityData, bool>)(u => u.IsRevealed && u.IsDeadAndHasLoot));
            foreach (var unitEntityData in units)
                lootWrapperList.Add(new LootWrapper() {
                    Unit = unitEntityData
                });
            var interactionLootParts = Game.Instance.State.MapObjects.All
                .Where<EntityDataBase>(e => e.IsInGame)
                .Select<EntityDataBase, InteractionLootPart>(i => i.Get<InteractionLootPart>())
                .Where<InteractionLootPart>(i => i?.Loot != Game.Instance.Player.SharedStash)
                .NotNull<InteractionLootPart>();
            var source = TempList.Get<InteractionLootPart>();
            foreach (var interactionLootPart in interactionLootParts) {
                if (// interactionLootPart.Owner.IsRevealed && 
                    interactionLootPart.Loot.HasLoot
                    //&& (
                    //    interactionLootPart.LootViewed || interactionLootPart.View is DroppedLoot && !(bool)(EntityPart)interactionLootPart.Owner.Get<DroppedLoot.EntityPartBreathOfMoney>() || (bool)(UnityEngine.Object)interactionLootPart.View.GetComponent<SkinnedMeshRenderer>()
                    //    )
                    )
                    source.Add(interactionLootPart);
            }
            var collection = source.Distinct<InteractionLootPart>((IEqualityComparer<InteractionLootPart>)new MassLootHelper.LootDuplicateCheck()).Select<InteractionLootPart, LootWrapper>((Func<InteractionLootPart, LootWrapper>)(i => new LootWrapper() {
                InteractionLoot = i
            }));
            lootWrapperList.AddRange(collection);
            return (IEnumerable<LootWrapper>)lootWrapperList;
        }
        public static void ShowAllChestsOnMap(bool hidden = false) {
            var interactionLootParts = Game.Instance.State.MapObjects.All
                .Where<EntityDataBase>(e => e.IsInGame)
                .Select<EntityDataBase, InteractionLootPart>(i => i.Get<InteractionLootPart>())
                .Where<InteractionLootPart>(i => i?.Loot != Game.Instance.Player.SharedStash)
                .NotNull<InteractionLootPart>();
            foreach (var interactionLootPart in interactionLootParts) {
                if (hidden) interactionLootPart.Owner.IsPerceptionCheckPassed = true;
                interactionLootPart.Owner.SetIsRevealedSilent(true);
            }
        }

        public static void ShowAllInevitablePortalLoot() {
            var interactionLootRevealers = Game.Instance.State.MapObjects.All.OfType<MapObjectEntityData>()
                .Where(e => e.IsInGame)
                .SelectMany(e => e.Interactions).OfType<InteractionSkillCheckPart>().NotNull()
                .Where(i => i.Settings?.DC == 0 && i.Settings.Skill == StatType.Unknown)
                .SelectMany(i => i.Settings.CheckPassedActions?.Get()?.Actions?.Actions ?? new GameAction[0]).OfType<HideMapObject>()
                .Where(a => a.Unhide)
                .Where(a => a.MapObject.GetValue()?.Get<InteractionLootPart>() is not null);
            foreach (var revealer in interactionLootRevealers) {
                revealer.RunAction();
            }
        }
        public static void OpenMassLoot() {
            // Access to LootContextVM
            var contextVM = RootUIContext.Instance.InGameVM?.StaticPartVM?.LootContextVM;
            if (contextVM == null) return;
            // Add new loot...
            var loot = MassLootHelper.GetMassLootFromCurrentArea();
            var lootVM = new LootVM(LootContextVM.LootWindowMode.ZoneExit, loot, null, () => contextVM.DisposeAndRemove(contextVM.LootVM));

            // Open window add lootVM int contextVM
            contextVM.LootVM.Value = lootVM;

            //EventBus.RaiseEvent((Action<ILootInterractionHandler>)(e => e.HandleZoneLootInterraction(null)));
        }
        public static void OpenPlayerChest() {
            // Access to LootContextVM
            var contextVM = RootUIContext.Instance.InGameVM?.StaticPartVM?.LootContextVM;
            if (contextVM == null) return;
            // Add new loot...
            var objects = new EntityViewBase[] { }; 
            var lootVM = new LootVM(LootContextVM.LootWindowMode.PlayerChest, objects , () => contextVM.DisposeAndRemove(contextVM.LootVM));
            var sharedStash = Game.Instance.Player.SharedStash;
            var lootObjectVM = new LootObjectVM("Player Chest", "", sharedStash, LootContextVM.LootWindowMode.PlayerChest, 1);
            lootVM.ContextLoot.Add(lootObjectVM);
            lootVM.AddDisposable(lootObjectVM);
            // Open window add lootVM int contextVM
            contextVM.LootVM.Value = lootVM;
        }
    }
}