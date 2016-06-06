﻿using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Characters
{
    public class CharacterEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private Character editedCharacter;
        private OptionGroupViewModel<Identity> identitiesViewModel;
        private OptionGroupViewModel<AnimatedAbility> animatedAbilitiesViewModel;
        private HashedObservableCollection<ICrowdMemberModel, string> characterCollection;

        #endregion

        #region Events

        #endregion

        #region Public Properties
        
        public Character EditedCharacter
        {
            get
            {
                return editedCharacter;
            }
            set
            {
                editedCharacter = value;
                OnPropertyChanged("EditedCharacter");
            }
        }

        public OptionGroupViewModel<Identity> IdentityViewModel
        {
            get
            {
                return identitiesViewModel;
            }
            set
            {
                identitiesViewModel = value;
                OnPropertyChanged("IdentityViewModel");
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

        #region Commands

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand<object> SavePositionCommand { get; private set; }
        public DelegateCommand<object> PlaceCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetedCommand { get; private set; }
        public DelegateCommand<object> TargetAndFollowCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCameraCommand { get; private set; }
        public DelegateCommand<object> ToggleManeuverWithCameraCommand { get; private set; }

        #endregion

        #region Constructor

        public CharacterEditorViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditCharacterEvent>().Subscribe(this.LoadCharacter);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(this.UnLoadCharacter);
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(this.Spawn, this.CanSpawn);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop, this.CanClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(this.ToggleTargeted, this.CanToggleTargeted);
            this.SavePositionCommand = new DelegateCommand<object>(this.SavePostion, this.CanSavePostion);
            this.PlaceCommand = new DelegateCommand<object>(this.Place, this.CanPlace);
            this.TargetAndFollowCommand = new DelegateCommand<object>(this.TargetAndFollow, this.CanTargetAndFollow);
            this.MoveTargetToCameraCommand = new DelegateCommand<object>(this.MoveTargetToCamera, this.CanMoveTargetToCamera);
            this.ToggleManeuverWithCameraCommand = new DelegateCommand<object>(this.ToggleManeuverWithCamera, this.CanToggleManeuverWithCamera);
        }

        internal void LoadCharacter(object state)
        {
            Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>> tuple = state as Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>;
            if (tuple != null)
            {
                Character character = tuple.Item1 as Character;
                HashedObservableCollection<ICrowdMemberModel, string> collection;
                if (tuple.Item2 != null)
                    collection = new HashedObservableCollection<ICrowdMemberModel, string>(tuple.Item2, x => x.Name);
                else
                {
                    collection = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name);
                    collection.Add(character as CrowdMemberModel);
                }
                if(character != null && collection != null)
                {
                    this.IdentityViewModel = this.Container.Resolve<OptionGroupViewModel<Identity>>(
                        new ParameterOverride("optionGroup", character.AvailableIdentities),
                        new ParameterOverride("owner", character)
                        );
                    this.AnimatedAbilitiesViewModel = this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                        new ParameterOverride("optionGroup", character.AnimatedAbilities),
                        new ParameterOverride("owner", character)
                        );
                    this.EditedCharacter = character;
                    this.characterCollection = collection;
                }
            }
        }

        private void UnLoadCharacter(object state)
        {
            ICrowdMemberModel model = state as ICrowdMemberModel;
            //if (model != null && this.EditedCharacter.Name == model.Name)
            //    this.EditedCharacter = null;
        }

        #endregion

        #region Methods

        private void Commands_RaiseCanExecuteChanged()
        {
            this.SpawnCommand.RaiseCanExecuteChanged();
            this.ClearFromDesktopCommand.RaiseCanExecuteChanged();
            this.ToggleTargetedCommand.RaiseCanExecuteChanged();
            this.SavePositionCommand.RaiseCanExecuteChanged();
            this.PlaceCommand.RaiseCanExecuteChanged();
            this.TargetAndFollowCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            this.ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
        }

        #region Spawn
        private bool CanSpawn(object state)
        {
            return EditedCharacter != null;
        }

        private void Spawn(object state)
        {
            //Check if is in roster, if not add to it
            if ((EditedCharacter as CrowdMemberModel).RosterCrowd == null)
            {
                eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<CrowdMemberModel, CrowdModel>(EditedCharacter as CrowdMemberModel, null));
            }
            EditedCharacter.Spawn();
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region Clear from Desktop
        private bool CanClearFromDesktop(object state)
        {
            return EditedCharacter != null && EditedCharacter.HasBeenSpawned;
        }

        private void ClearFromDesktop(object state)
        {
            EditedCharacter.ClearFromDesktop();
            Commands_RaiseCanExecuteChanged();
        }

        #endregion

        #region Save Positon

        private bool CanSavePostion(object state)
        {
            bool canSavePosition = false;
            if (this.EditedCharacter != null && this.EditedCharacter.HasBeenSpawned)
            {
                canSavePosition = true;
            }
            return canSavePosition;
        }

        private void SavePostion(object state)
        {
            (EditedCharacter as CrowdMemberModel).SavePosition();
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.PlaceCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Place
        private bool CanPlace(object state)
        {
            bool canPlace = false;
            if (this.EditedCharacter != null)
            {
                var crowdMemberModel = EditedCharacter as CrowdMemberModel;
                if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME && crowdMemberModel.SavedPosition != null)
                {
                    canPlace = true;
                }
                else if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    CrowdModel rosterCrowdModel = crowdMemberModel.RosterCrowd as CrowdModel;
                    if (rosterCrowdModel.SavedPositions.ContainsKey(crowdMemberModel.Name))
                    {
                        canPlace = true;
                    }
                }
            }
            return canPlace;
        }
        private void Place(object state)
        {
            (EditedCharacter as CrowdMemberModel).Place();
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region ToggleTargeted

        private bool CanToggleTargeted(object state)
        {
            bool canToggleTargeted = false;
            if (this.EditedCharacter != null && EditedCharacter.HasBeenSpawned)
            {
                canToggleTargeted = true;
            }
            return canToggleTargeted;
        }

        private void ToggleTargeted(object obj)
        {
            EditedCharacter.ToggleTargeted();
        }

        #endregion

        #region Target And Follow

        private bool CanTargetAndFollow(object state)
        {
            return CanToggleTargeted(state);
        }

        private void TargetAndFollow(object obj)
        {
            editedCharacter.TargetAndFollow();
        }

        #endregion

        #region Move Target to Camera

        private bool CanMoveTargetToCamera(object arg)
        {
            bool canMoveTargetToCamera = true;
            if (this.EditedCharacter == null)
            {
                canMoveTargetToCamera = false;
                return canMoveTargetToCamera;
            }
            else
            {
                return editedCharacter.HasBeenSpawned;
            }
        }

        private void MoveTargetToCamera(object obj)
        {
            EditedCharacter.MoveToCamera();
        }

        #endregion

        #region ToggleManeuverWithCamera


        private bool CanToggleManeuverWithCamera(object arg)
        {
            bool canManeuverWithCamera = false;
            if (this.EditedCharacter != null && (EditedCharacter.HasBeenSpawned || EditedCharacter.ManeuveringWithCamera))
            {
                canManeuverWithCamera = true;
            }
            return canManeuverWithCamera;
        }

        private void ToggleManeuverWithCamera(object obj)
        {
            EditedCharacter.ToggleManueveringWithCamera();
        }

        #endregion

        #endregion
    }
}
