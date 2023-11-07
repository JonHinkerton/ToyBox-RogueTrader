﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using UniRx;
using ModKit;
using Kingmaker.EntitySystem.Interfaces;

namespace ToyBox.classes.MainUI.Inventory {
    internal class SelectedCharacterObserver : IEntitySubscriber {

    public static SelectedCharacterObserver Shared { get; private set; } = new();
        private IDisposable m_SelectedUnitUpdate;
        public UnitEntityData SelectedUnit = null;
        public delegate void NotifyDelegate();
        public NotifyDelegate Notifiers;

        public SelectedCharacterObserver() {
            EventBus.Subscribe((object)this);
            Game.Instance.SelectionCharacter.SelectedUnit.Subscribe<BaseUnitEntity>(delegate(BaseUnitEntity u) {
                SelectedUnit = u;
                Mod.Debug($"SelectedCharacterObserver - selected character changed to {SelectedUnit?.CharacterName.orange() ?? "null"} notifierCount: {Notifiers?.GetInvocationList()?.Length}");
                Notifiers?.Invoke();
            });
        }
        public IEntity GetSubscribingEntity() => throw new NotImplementedException();
    }
}
