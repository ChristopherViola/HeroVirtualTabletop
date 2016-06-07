﻿using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.OptionGroups;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Module.HeroVirtualTabletop.Roster
{
    public class ActiveCharacterWidgetViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private RosterExplorerViewModel rosterExpVM;
        private OptionGroupViewModel<Identity> identitiesViewModel;
        private OptionGroupViewModel<AnimatedAbility> animatedAbilitiesViewModel;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        private Visibility visibility = Visibility.Collapsed;
        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                visibility = value;
                OnPropertyChanged("Visibility");
            }
        }

        private Character activeCharacter;
        public Character ActiveCharacter
        {
            get
            {
                return activeCharacter;
            }
            set
            {
                activeCharacter = value;
                OnPropertyChanged("ActiveCharacter");
            }
        }

        public OptionGroupViewModel<Identity> IdentitiesViewModel
        {
            get
            {
                return identitiesViewModel;
            }
            set
            {
                identitiesViewModel = value;
                OnPropertyChanged("IdentitiesViewModel");
            }
        }

        public OptionGroupViewModel<AnimatedAbility> AnimatedAbilitiesViewModel
        {
            get
            {
                return animatedAbilitiesViewModel;
            }
            set
            {
                animatedAbilitiesViewModel = value;
                OnPropertyChanged("AnimatedAbilitiesViewModel");
            }
        }

        #endregion

        #region Constructor

        public ActiveCharacterWidgetViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.rosterExpVM = this.Container.Resolve<RosterExplorerViewModel>();
            this.rosterExpVM.PropertyChanged += RosterExpVM_PropertyChanged;
            InitializeCommands();
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {

        }

        #endregion

        #region Private Methods
        
        private void RosterExpVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveCharacter")
            {
                LoadCharacter(rosterExpVM.ActiveCharacter as Character);
            }
        }

        private void LoadCharacter(Character character)
        {
            this.ActiveCharacter = character;
            if (character != null)
            {
                this.Visibility = Visibility.Visible;
                this.IdentitiesViewModel = this.Container.Resolve<OptionGroupViewModel<Identity>>(
                    new ParameterOverride("optionGroup", character.AvailableIdentities),
                    new ParameterOverride("owner", character),
                    new ParameterOverride("editable", false)
                    );
                this.AnimatedAbilitiesViewModel = this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                    new ParameterOverride("optionGroup", character.AnimatedAbilities),
                    new ParameterOverride("owner", character),
                    new ParameterOverride("editable", false)
                    );
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
    }
}
