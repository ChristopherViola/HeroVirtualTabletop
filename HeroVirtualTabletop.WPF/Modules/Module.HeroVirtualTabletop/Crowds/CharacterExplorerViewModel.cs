﻿using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Framework.WPF.Services.MessageBoxService;
using Module.Shared.Messages;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private EventAggregator eventAggregator;
        private ICrowdRepository crowdRepository;
        private HashedObservableCollection<ICrowdMember, string> characterCollection;

        #endregion

        #region Events

        #endregion

        #region Public Properties

        private HashedObservableCollection<CrowdModel, string> crowdCollection;
        public HashedObservableCollection<CrowdModel, string> CrowdCollection
        {
            get
            {
                return crowdCollection;
            }
            set
            {
                crowdCollection = value;
                OnPropertyChanged("CrowdCollection");
            }
        }

        private CrowdModel selectedCrowdModel;
        public CrowdModel SelectedCrowdModel
        {
            get
            {
                return selectedCrowdModel;
            }
            set
            {
                selectedCrowdModel = value;
                this.DeleteCharacterCrowdCommand.RaiseCanExecuteChanged();
            }
        }

        private CrowdMember selectedCrowdMember;
        public CrowdMember SelectedCrowdMember
        {
            get
            {
                return selectedCrowdMember;
            }
            set
            {
                selectedCrowdMember = value;
                this.DeleteCharacterCrowdCommand.RaiseCanExecuteChanged();
            }
        }

        private CrowdModel selectedCrowdParent;
        public CrowdModel SelectedCrowdParent
        {
            get
            {
                return selectedCrowdParent;
            }
            set
            {
                selectedCrowdParent = value;
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> AddCrowdCommand { get; private set; }

        public DelegateCommand<object> AddCharacterCommand { get; private set; }

        public DelegateCommand<object> DeleteCharacterCrowdCommand { get; private set; }

        public ICommand UpdateSelectedCrowdMemberCommand { get; private set; }

        #endregion

        #region Constructor

        public CharacterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ICrowdRepository crowdRepository, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.crowdRepository = crowdRepository;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            InitializeCommands();
            LoadCrowdCollection();

        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddCrowdCommand = new DelegateCommand<object>(this.AddCrowd);
            this.AddCharacterCommand = new DelegateCommand<object>(this.AddCharacter);
            this.DeleteCharacterCrowdCommand = new DelegateCommand<object>(this.DeleteCharacterCrowd, this.CanDeleteCharacterCrowd);

            UpdateSelectedCrowdMemberCommand = new SimpleCommand
            {

                ExecuteDelegate = x =>
                    UpdateSelectedCrowdMember(x)

            };
        }

        #endregion

        #region Methods

        #region Load Crowd Collection
        private void LoadCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.GetCrowdCollection(this.LoadCrowdCollectionCallback);
        }

        private void LoadCrowdCollectionCallback(List<CrowdModel> crowdList)
        {
            this.CrowdCollection = new HashedObservableCollection<CrowdModel, string>(crowdList,
                (CrowdModel c) => { return c.Name; }
                );
            CrowdModel allCharactersModel = this.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
            if (allCharactersModel == null)
                allCharactersModel = new CrowdModel();
            this.characterCollection = new HashedObservableCollection<ICrowdMember, string>(allCharactersModel.CrowdMemberCollection,
                (ICrowdMember c) => { return c.Name; }
                );
            //this.BusyService.HideBusy();
        }

        #endregion

        #region Update Selected Crowd
        private void UpdateSelectedCrowdMember(object state)
        {
            ICrowdMember selectedCrowdMember;
            Object selectedCrowdModel = Helper.GetCurrentSelectedCrowdInCrowdCollection(state, out selectedCrowdMember);
            CrowdModel crowdModel = selectedCrowdModel as CrowdModel;
            this.SelectedCrowdModel = crowdModel;
            this.SelectedCrowdMember = selectedCrowdMember as CrowdMember;
        }
        #endregion

        #region Add Crowd
        private void AddCrowd(object state)
        {
            // Create a new Crowd
            CrowdModel crowdModel = this.GetNewCrowdModel();
            // Add the crowd to List of Crowd Members as a new Crowd Member
            this.CrowdCollection.Add(crowdModel);
            // Also add the crowd under any currently selected crowd
            if(this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>();
                this.SelectedCrowdModel.CrowdMemberCollection.Add(crowdModel);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }
        private CrowdModel GetNewCrowdModel()
        {
            
            string name = "Crowd";
            string suffix = string.Empty;
            int i = 0;
            while (this.CrowdCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return new CrowdModel(name + suffix);
        }

        #endregion

        #region Add Character

        private void AddCharacter(object state)
        {
            // Create a new Character
            Character character = this.GetNewCharacter();
            // Create All Characters List if not already there
            CrowdModel crowdModelAllCharacters = this.CrowdCollection.Where(c => c.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            if (crowdModelAllCharacters == null || crowdModelAllCharacters.CrowdMemberCollection == null || crowdModelAllCharacters.CrowdMemberCollection.Count == 0)
            {
                crowdModelAllCharacters = new CrowdModel(Constants.ALL_CHARACTER_CROWD_NAME);
                this.CrowdCollection.Add(crowdModelAllCharacters);
                crowdModelAllCharacters.CrowdMemberCollection = new ObservableCollection<ICrowdMember>();
                this.characterCollection = new HashedObservableCollection<ICrowdMember, string>(crowdModelAllCharacters.CrowdMemberCollection,
                    (ICrowdMember c) => { return c.Name; });
            }
            // Add the Character under All Characters List
            crowdModelAllCharacters.CrowdMemberCollection.Add(character as CrowdMember);
            this.characterCollection.Add(character as CrowdMember);
            // Also add the character under any currently selected crowd
            if (this.SelectedCrowdModel != null && this.SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
            {
                if (this.SelectedCrowdModel.CrowdMemberCollection == null)
                    this.SelectedCrowdModel.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>();
                this.SelectedCrowdModel.CrowdMemberCollection.Add(character as CrowdMember);
            }
            // Update Repository asynchronously
            this.SaveCrowdCollection();
        }

        private Character GetNewCharacter()
        {
            string name = "Character";
            string suffix = string.Empty;
            int i = 0;
            while (this.characterCollection.ContainsKey(name + suffix))
            {
                suffix = string.Format(" ({0})", ++i);
            }
            return new CrowdMember(name + suffix);
        }

        #endregion

        #region Delete Character or Crowd

        public bool CanDeleteCharacterCrowd(object state)
        {
            bool canDeleteCharacterOrCrowd = false;
            if (SelectedCrowdModel != null)
            {
                if (SelectedCrowdModel.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                    canDeleteCharacterOrCrowd = true;
                else
                {
                    if (SelectedCrowdMember != null)
                        canDeleteCharacterOrCrowd = true;
                }
            }

            return canDeleteCharacterOrCrowd;
        }

        public void DeleteCharacterCrowd(object state)
        { 
            // Determine if Character or Crowd is to be deleted
            if (SelectedCrowdMember != null) // Delete Character
            {
                // Check if the Character is in All Characters. If so, prompt
                if(SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    var chosenOption = this.messageBoxService.ShowDialog(Messages.DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE, Messages.DELETE_CHARACTER_CAPTION, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    switch (chosenOption)
                    { 
                        case System.Windows.MessageBoxResult.Yes:
                            // Delete the Character from all the crowds
                            DeleteCrowdMemberFromAllCrowdsByName(SelectedCrowdMember.Name);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    // Delete the Character from all occurances of this crowd
                    DeleteCrowdMemberFromCrowdModelByName(SelectedCrowdModel, SelectedCrowdMember.Name);
                }
            }
            else // Delete Crowd
            {
                //If it is a nested crowd, just delete it from the parent
                if (this.SelectedCrowdParent != null)
                {
                    string nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                    DeleteNestedCrowdFromCrowdModelByName(SelectedCrowdParent, nameOfDeletingCrowdModel);
                }
                // Check if there are containing characters. If so, prompt
                else if (SelectedCrowdModel.CrowdMemberCollection != null && SelectedCrowdModel.CrowdMemberCollection.Where(cm => cm is CrowdMember).Count() > 0)
                {
                    var chosenOption = this.messageBoxService.ShowDialog(Messages.DELETE_CONTAINING_CHARACTERS_FROM_CROWD_PROMPT_MESSAGE, Messages.DELETE_CROWD_CAPTION, System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);
                    switch (chosenOption)
                    {
                        case System.Windows.MessageBoxResult.Yes:
                            // Delete crowd specific characters from All Characters and this crowd
                            List<ICrowdMember> crowdSpecificCharacters = FindCrowdSpecificCrowdMembers(this.selectedCrowdModel);
                            string nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                            DeleteCrowdMembersFromAllCrowdsByList(crowdSpecificCharacters);
                            DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                            DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
                            break;
                        case System.Windows.MessageBoxResult.No:
                            nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                            DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                            DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
                            break;
                        default:
                            break;
                    }
                }
                // or just delete the crowd from crowd collection and other crowds
                else
                {
                    string nameOfDeletingCrowdModel = SelectedCrowdModel.Name;
                    DeleteNestedCrowdFromAllCrowdsByName(nameOfDeletingCrowdModel);
                    DeleteCrowdFromCrowdCollectionByName(nameOfDeletingCrowdModel);
                }
            }
            // Finally save repository
            this.SaveCrowdCollection();
        }

        private List<ICrowdMember> FindCrowdSpecificCrowdMembers(CrowdModel crowdModel)
        {
            List<ICrowdMember> crowdSpecificCharacters = new List<ICrowdMember>();
            foreach (ICrowdMember cMember in crowdModel.CrowdMemberCollection)
            {
                if (cMember is CrowdMember)
                {
                    CrowdMember currentCrowdMember = cMember as CrowdMember;
                    foreach (CrowdModel cModel in this.CrowdCollection.Where(cm => cm.Name != SelectedCrowdModel.Name))
                    {
                        var crm = cModel.CrowdMemberCollection.Where(cm => cm is CrowdMember && cm.Name == currentCrowdMember.Name).FirstOrDefault();
                        if (crm == null)
                        {
                            if (crowdSpecificCharacters.Where(csc => csc.Name == currentCrowdMember.Name).FirstOrDefault() == null)
                                crowdSpecificCharacters.Add(currentCrowdMember);
                        }
                    }
                }
            }
            return crowdSpecificCharacters;
        }

        private void DeleteCrowdMemberFromAllCrowdsByName(string nameOfDeletingCrowdMember)
        {
            foreach (CrowdModel cModel in this.CrowdCollection)
            {
                DeleteCrowdMemberFromCrowdModelByName(cModel, nameOfDeletingCrowdMember);
            }
            DeleteCrowdMemberFromCharacterCollectionByName(nameOfDeletingCrowdMember);
        }
        private void DeleteCrowdMemberFromCrowdModelByName(CrowdModel crowdModel, string nameOfDeletingCrowdMember)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                var crm = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == nameOfDeletingCrowdMember).FirstOrDefault();
                crowdModel.CrowdMemberCollection.Remove(crm); 
            }
        }
        private void DeleteCrowdMemberFromCharacterCollectionByName(string nameOfDeletingCrowdMember)
        {
            var charFromAllCrowd = characterCollection.Where(c => c.Name == nameOfDeletingCrowdMember).FirstOrDefault();
            this.characterCollection.Remove(charFromAllCrowd);
        }
        private void DeleteCrowdMemberFromCharacterCollectionByList(List<ICrowdMember> crowdMembersToDelete)
        {
            foreach(var crowdMemberToDelete in crowdMembersToDelete)
            {
                var deletingCrowdMember = characterCollection.Where(c => c.Name == crowdMemberToDelete.Name).FirstOrDefault();
                characterCollection.Remove(deletingCrowdMember);
            }
        }

        private void DeleteCrowdMembersFromAllCrowdsByList(List<ICrowdMember> crowdMembersToDelete)
        {
            foreach (CrowdModel cModel in this.CrowdCollection)
            {
                DeleteCrowdMembersFromCrowdModelByList(cModel, crowdMembersToDelete);
            }
        }
        private void DeleteCrowdMembersFromCrowdModelByList(CrowdModel crowdModel, List<ICrowdMember> crowdMembersToDelete)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                foreach (var crowdMemberToDelete in crowdMembersToDelete)
                {
                    var deletingCrowdMemberFromModel = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == crowdMemberToDelete.Name).FirstOrDefault();
                    crowdModel.CrowdMemberCollection.Remove(deletingCrowdMemberFromModel);
                }
            }
        }
        private void DeleteNestedCrowdFromAllCrowdsByName(string nameOfDeletingCrowdModel)
        {
            foreach (CrowdModel cModel in this.CrowdCollection)
            {
                DeleteNestedCrowdFromCrowdModelByName(cModel, nameOfDeletingCrowdModel);
            }
        }
        private void DeleteNestedCrowdFromCrowdModelByName(CrowdModel crowdModel, string nameOfDeletingCrowdModel)
        {
            if (crowdModel.CrowdMemberCollection != null)
            {
                var crowdModelToDelete = crowdModel.CrowdMemberCollection.Where(cm => cm.Name == nameOfDeletingCrowdModel).FirstOrDefault();
                if (crowdModelToDelete != null)
                    crowdModel.CrowdMemberCollection.Remove(crowdModelToDelete); 
            }
        }
        private void DeleteCrowdFromCrowdCollectionByName(string nameOfDeletingCrowdModel)
        {
            var crowdToDelete = this.CrowdCollection.Where(cr => cr.Name == nameOfDeletingCrowdModel).FirstOrDefault();
            this.CrowdCollection.Remove(crowdToDelete);
        }
        #endregion

        #region Save Crowd Collection

        private void SaveCrowdCollection()
        {
            //this.BusyService.ShowBusy();
            this.crowdRepository.SaveCrowdCollection(this.SaveCrowdCollectionCallback, this.CrowdCollection.ToList());
        }

        private void SaveCrowdCollectionCallback()
        {
            //this.BusyService.HideBusy();
        }

        #endregion

        #endregion
    }
}
