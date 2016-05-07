﻿using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.Shared;
using Module.Shared.Messages;
using Moq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Module.UnitTest.Crowd
{
    #region Crowd Repository Test
    [TestClass]
    public class CrowdRepositoryTest : BaseCrowdTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void GetCrowdCollection_CreatesNewRepositoryIfNoPresentRepositoryFile()
        {
            string testRepoFileName = "test.data";
            string fullFilePath = Path.Combine(testRepoFileName);
            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);
            // Here we are directly testing the repository and need to verify file i/o. So, not mocking.
            CrowdRepository crowdRepository = new CrowdRepository();
            crowdRepository.CrowdRepositoryPath = fullFilePath;
            List<CrowdModel> retrievedCrowdList = null;
            AutoResetEvent terminateEvent = new AutoResetEvent(false);
            bool testPassed = false;
            ThreadStart ts = delegate
            {
                crowdRepository.GetCrowdCollection(
                (List<CrowdModel> crowdList) =>
                {
                    try
                    {
                        retrievedCrowdList = crowdList;
                        Assert.IsTrue((File.Exists(fullFilePath)));
                        File.Delete(fullFilePath);
                        testPassed = true;
                    }
                    catch (AssertFailedException ex)
                    {
                        terminateEvent.Set();
                    }
                    finally
                    {
                        terminateEvent.Set();
                    }
                }
                );
            };
            Thread t = new Thread(ts);
            t.Start();
            terminateEvent.WaitOne();
            Assert.IsTrue(testPassed);
        }

        /// <summary>
        /// This is actually the acceptance test for crowd repository. Although the method name suggests only SaveCrowdCollection related test, it actually performs tests on both GetCrowdCollection
        /// and SaveCrowdCollection for their basic functionalities.
        /// </summary>
        [TestMethod]
        public void SaveCrowdCollection_SavesCrowdCollectionsConsistently()
        {
            CrowdModel crowd = new CrowdModel { Name = "Test Crowd 1" };
            CrowdModel childCrowd = new CrowdModel { Name = "Child Crowd 1" };
            CrowdMember crowdMember1 = new CrowdMember { Name = "Test CrowdMember 1" };
            CrowdMember crowdMember2 = new CrowdMember { Name = "Test CrowdMember 1.1" };
            crowd.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember1, childCrowd };
            childCrowd.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember2 };
            string testRepoFileName = "test.data";
            CrowdRepository crowdRepository = new CrowdRepository();
            crowdRepository.CrowdRepositoryPath = testRepoFileName;
            List<CrowdModel> crowdCollection = new List<CrowdModel>() { crowd };
            AutoResetEvent terminateEvent = new AutoResetEvent(false);
            bool testPassed = false;

            ThreadStart ts = delegate
            {
                crowdRepository.SaveCrowdCollection(() =>
                {
                    // More crowd members being added, repository shouldn't know
                    crowdCollection.Add(new CrowdModel() { Name = "New Crowd 1" });
                    crowd.CrowdMemberCollection.Add(new CrowdMember() { Name = "New CrowdMember 1" });

                    List<CrowdModel> retrievedCrowdList = null;
                    crowdRepository.GetCrowdCollection((List<CrowdModel> crowdList) =>
                    {
                        retrievedCrowdList = crowdList;
                        try
                        {
                            CrowdModel cmodel1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                            Assert.IsNull(cmodel1);
                            CrowdMember cm1 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "New CrowdMember 1").FirstOrDefault() as CrowdMember;
                            Assert.IsNull(cm1);
                            CrowdModel cmodel2 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                            Assert.IsNotNull(cmodel2);
                            CrowdMember cm2 = cmodel2.CrowdMemberCollection.Where(c => c.Name == "Test CrowdMember 1.1").FirstOrDefault() as CrowdMember;
                            Assert.IsNotNull(cm2);
                        }
                        catch (AssertFailedException ex)
                        {
                            terminateEvent.Set();
                        }
                        // Now save the updated crowd and check if repsitory knows about them
                        crowdRepository.SaveCrowdCollection(() =>
                        {
                            crowdRepository.GetCrowdCollection((List<CrowdModel> crowdListAnother) =>
                            {
                                retrievedCrowdList = crowdListAnother;
                                try
                                {
                                    CrowdModel cmodel1 = retrievedCrowdList.Where(c => c.Name == "New Crowd 1").FirstOrDefault();
                                    Assert.IsNotNull(cmodel1);
                                    CrowdMember cm1 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "New CrowdMember 1").FirstOrDefault() as CrowdMember;
                                    Assert.IsNotNull(cm1);
                                    CrowdModel cmodel2 = retrievedCrowdList[0].CrowdMemberCollection.Where(c => c.Name == "Child Crowd 1").FirstOrDefault() as CrowdModel;
                                    Assert.IsNotNull(cmodel2);
                                    CrowdMember cm2 = cmodel2.CrowdMemberCollection.Where(c => c.Name == "Test CrowdMember 1.1").FirstOrDefault() as CrowdMember;
                                    Assert.IsNotNull(cm2);
                                    File.Delete(testRepoFileName);
                                    testPassed = true;
                                }
                                catch (AssertFailedException ex)
                                {
                                    terminateEvent.Set();
                                }
                                finally
                                {
                                    terminateEvent.Set();
                                }
                            });
                        }, crowdCollection);
                    });
                }, crowdCollection);
            };
            Thread t = new Thread(ts);
            t.Start();
            terminateEvent.WaitOne();
            Assert.IsTrue(testPassed);
        }
    }
    #endregion

    #region Crowd Member Test
    [TestClass]
    public class CrowdMemberTest : BaseCrowdTest
    {
        private CharacterExplorerViewModel characterExplorerViewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            InitializeDefaultList();
            this.numberOfItemsFound = 0;
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        
        #region Add Crowd Tests
        /// <summary>
        /// Adding a crowd should obviously call the repository to update the crowd collection in the repository. This is an acceptance test. 
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryTest
        /// to make sure that the repository can save to file what it is being passed. So, making sure that repository is indeed being called with correct parameters would suffice here.
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdInRepository() 
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());// Starting with an empty repository
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == "Crowd")));
        }

        /// <summary>
        /// When the repository is empty, Adding a crowd should add it as a stand alone crowd and the All Characters list should not be present at this time
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesOnlyOneNewCrowdWithDefaultNameIfRepositoryIsEmpty()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cmodel2 = crowdList.Where(cr => cr.Name == "Crowd").FirstOrDefault();
            Assert.IsTrue(cmodel1 == null && cmodel2 != null);

        }

        /// <summary>
        /// The name of an added crowd should be unique, and should be "Crowd" or "Crowd (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesCrowdsWithUniqueNames()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == "Crowd");
            var cr2 = crowdList.Where(cr => cr.Name == "Crowd (1)").FirstOrDefault();
            Assert.IsTrue(cr1.Count() == 1 && cr2 != null);
        }

        /// <summary>
        /// If no crowd or character is selected, the new crowd should just be added as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesStandAloneCrowdIfNoCrowdOrCharactersSelected()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new crowd should be added under it and also as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdUnderSelectedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Gotham City" is selected
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added crowd should be in a total of two places - one stand alone and one nested position
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm=>cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded); 
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new crowd should be added under all of them, as well as a stand alone crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdUnderAllOccurrancesOfSelectedCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Assuming "The Narrows" is selected
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 4); // The added crowd should be in a total of 4 places - one stand alone and three nested positions under The Narrows
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd1.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "League of Shadows").FirstOrDefault();
            crowdAdded = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            crowdAdded = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNotNull(crowdAdded);
        }
        /// <summary>
        /// If the current selected member is a character under All Characters or the All Characters crowd itself, the new crowd should be added as just another crowd and not under All Characters
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdAsStandAloneIfSelectedCrowdIsAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            // "All Characters" is the selected crowd as selecting a character under All Characters would also result in the containing crowd being selected in view model
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; 
            characterExplorerViewModel.AddCrowdCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Crowd");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added crowd should be added only as a stand alone crowd
            CrowdModel crowdAdded = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault();
            Assert.IsNotNull(crowdAdded);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            crowdAdded = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Crowd").FirstOrDefault() as CrowdModel;
            Assert.IsNull(crowdAdded);
            // And so on...
        }
        /// <summary>
        /// If the current selected member is a Character not under All Characters Crowd but in another Crowd, the new crowd should be added as a sibling of that character in all 
        /// occurances of that crowd
        /// </summary>
        [TestMethod]
        public void AddCrowd_CreatesNewCrowdAsSiblingOfSelectedCharacterNotUnderAllCharacters()
        {
            // Since selecting a character would result in the containing crowd to be selected in the view model, here the containing Crowd would be selected and this case 
            // has already been covered in another test above.
            AddCrowd_CreatesNewCrowdUnderAllOccurrancesOfSelectedCrowd();
        }
        #endregion

        #region Add Character Tests

        /// <summary>
        /// Adding a character should add it in the crowd. Depending on the situation, it could be added to one or more crowds. 
        /// This test is a combination of several other test cases.
        /// This is an acceptance test.
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterInCrowd()
        {
            AddCharacter_CreatesNewCharacterUnderAllCharactersIfNoCrowdOrCharacterIsSelected();
            TestInitialize();
            AddCharacter_CreatesNewCharacterUnderSelectedCrowd();
            TestInitialize();
            AddCharacter_CreatesNewCharacterUnderAllOccurrancesOfSelectedCrowd();
        }
        /// <summary>
        /// Adding a character should obviously call the repository to insert the character in the repository.
        /// Importantly, we are not checking the contents of repository file to see if the file got updated, that is because we have tested the repository separately in CrowdRepositoryTest
        /// to make sure that the repository can save to file what it is being passed. So, making sure that repository is indeed being called with correct parameters would suffice here.
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterInRepository()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());// Starting with an empty repository
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                a => a.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Count == 1 && cmList[0].Name == Constants.ALL_CHARACTER_CROWD_NAME && cmList[0].CrowdMemberCollection.Count == 1 && cmList[0].CrowdMemberCollection[0].Name == "Character")));
        }
        /// <summary>
        /// When the repository is empty, Adding a character should first create the default All Characters crowd and the new character should be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesAllCharactersCrowdAndAddsNewCharacterUnderAllCharactersIfRepositoryIsEmpty()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cmodel1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            Assert.IsNotNull(cmodel1);
            var char1 = cmodel1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(char1);
        }
        /// <summary>
        /// The name of an added character should be unique, and should be "Character" or "Character (*)" where * stands for the empty string or first available number from 1
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesCharactersWithUniqueNames()
        {
            InitializeCrowdRepositoryMockWithList(new List<CrowdModel>());
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var cr1 = crowdList.Where(cr => cr.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            Assert.IsTrue(cr1 != null && cr1.CrowdMemberCollection.Count() == 2);
            var cm1 = cr1.CrowdMemberCollection.Where(c => c.Name == "Character");
            var cm2 = cr1.CrowdMemberCollection.Where(c => c.Name == "Character (1)").FirstOrDefault();
            Assert.IsTrue(cm1 != null && cm1.Count() == 1 && cm2 != null);
        }
        /// <summary>
        /// If no crowd or character is selected, the new character should just be added under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderAllCharactersIfNoCrowdOrCharacterIsSelected()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = null;
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 1);
            var cm1 = crowdList.ToList()[0].CrowdMemberCollection.Where(c => c.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd, the new character should be added under it and also under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderSelectedCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Assuming "Gotham City" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 2);// The added character should be in a total of two places - one under All Characters and one under Gotham City
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            cm1 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }
        /// <summary>
        /// If the current selected member is a Crowd that appears in multiple locations, the new character should be added under all of them, as well as under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterUnderAllOccurrancesOfSelectedCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Assuming "The Narrows" is selected
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            // The added character should be in a total of 4 places - one under All Characters and thrice under The Narrows as it appears in three places in the Crowd list
            Assert.IsTrue(this.numberOfItemsFound == 4);
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(c => c.Name == "Character");
            Assert.IsNotNull(cm1);
            CrowdModel crowd1 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "Gotham City").FirstOrDefault();
            var cm2 = crowd1.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm2);
            var cm3 = (crowd1.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm3);
            CrowdModel crowd2 = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == "League of Shadows").FirstOrDefault();
            var cm4 = crowd2.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNull(cm4);
            var cm5 = (crowd2.CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm5);
        }
        /// <summary>
        /// If the current selected member is a Character under All Characters, the new character should be added as a sibling of that character under All Characters
        /// </summary>
        [TestMethod]
        public void AddCharacter_CreatesNewCharacterAsSiblingOfSelectedCharacter()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            // Assuming a character is selected under All Characters. Then All Characters is the selected crowd
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0];
            characterExplorerViewModel.AddCharacterCommand.Execute(null);
            IEnumerable<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Character");
            Assert.IsTrue(this.numberOfItemsFound == 1); // The added character should be added under All Characters
            CrowdModel crowdAllCharacters = characterExplorerViewModel.CrowdCollection.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).FirstOrDefault();
            var cm1 = crowdAllCharacters.CrowdMemberCollection.Where(cm => cm.Name == "Character").FirstOrDefault();
            Assert.IsNotNull(cm1);
        }

        #endregion

        #region Delete Crowd Tests
        /// <summary>
        /// Here we just need to make sure that the repository is being called with updated collection that does not have the deleted crowd in it.
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdFromRepository() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No); // Just Delete the crowd
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null; // The selected crowd is in main tree, not nested in another crowd
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.IsAny<List<CrowdModel>>()), Times.Once());
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => cmList.Where(cm=>cm.Name == "Gotham City").FirstOrDefault() == null)));
        }
        /// <summary>
        /// User should not be allowed to delete the All Characters crowd
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_PreventsUserFromDeletingAllCharactersCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; // Selecting All Characters to delete it
            bool b = characterExplorerViewModel.DeleteCharacterCrowdCommand.CanExecute(null);
            Assert.IsFalse(b); // The delete command will not be available to the user
        }
        /// <summary>
        /// If the crowd is not in another crowd, rather in the main crowd collection, it will be removed from the main collection and repository
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdFromCrowdCollectionIfNotNestedWithinAnotherCrowd()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No); // Just Delete the crowd
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null; // Since the crowd is in main collection, there is no parent for this crowd
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var crowd = crowdList.Where(cr => cr.Name == "Gotham City").FirstOrDefault();
            Assert.IsNull(crowd);
        }       
        /// <summary>
        /// If the crowd is in another crowd, it will be removed only from all instances of the containing crowd, but not from main collection or repository or any other crowd
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdOnlyFromContainingCrowdIfNestedWithinAnotherCrowd()
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // Selecting The Narrows for deletion
            characterExplorerViewModel.SelectedCrowdParent = characterExplorerViewModel.CrowdCollection[1] as CrowdModel;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var crowd = crowdList[1].CrowdMemberCollection.Where(c => c.Name == "The Narrows").FirstOrDefault();
            Assert.IsNull(crowd);
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "The Narrows");
            Assert.IsTrue(this.numberOfItemsFound > 0);
            crowd = crowdList.Where(cr => cr.Name == "The Narrows").FirstOrDefault();
            Assert.IsNotNull(crowd);
        }
        /// <summary>
        /// A deletion attempt for a crowd in main collection should prompt the user about deleting contained characters
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_PromptsUserWithMessageBeforeDeletion()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            messageBoxServiceMock.Verify(
                msgservice => msgservice.ShowDialog(It.Is<string>(s => s == Messages.DELETE_CONTAINING_CHARACTERS_FROM_CROWD_PROMPT_MESSAGE),
                    It.Is<string>(s => s == Messages.DELETE_CROWD_CAPTION), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once);
        }
        /// <summary>
        /// If user chooses to remove characters contained in a crowd as well as deleting the crowd, the crowd and only the crowd specific characters (not member of any other crowd)
        /// should get removed
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdAndCrowdSpecificCharactersIfUserChoosesToRemoveContainedCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Yes); // Pre-configuring message box to decide to delete characters
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Gotham City");
            Assert.IsTrue(this.numberOfItemsFound == 0); // Gotham City Destroyed...
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Batman"); 
            Assert.IsTrue(this.numberOfItemsFound == 0); // And no sign of Batman :(
        }
        /// <summary>
        /// If user chooses to keep characters contained in a crowd, the characters remain under All Characters while the crowd gets deleted
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_RemovesCrowdButKeepsCharactersIfUserChoosesToKeepCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.No); // Pre-configuring message box to decide not to delete characters
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Gotham City");
            Assert.IsTrue(this.numberOfItemsFound == 0); // Gotham City Destroyed...
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Batman");
            Assert.IsTrue(this.numberOfItemsFound > 0); // But Batman still lives
        }
        /// <summary>
        /// No removal of character or crowd happens if user cancels the request after being prompted
        /// </summary>
        [TestMethod]
        public void DeleteCrowd_DoesNothingIfUserCancelsDeleteRequest()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Cancel); // Pre-configuring message box to cancel delete request
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1]; // Selecting Gotham City to delete it
            characterExplorerViewModel.SelectedCrowdParent = null;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            var crowd = crowdList.Where(cr => cr.Name == "Gotham City").FirstOrDefault();
            Assert.IsNotNull(crowd); // Crowd has not been deleted
        }

        #endregion

        #region Delete Character Tests
        /// <summary>
        /// Character should be deleted from all instances of the containing crowd
        /// </summary>
        [TestMethod]
        public void DeleteCharacter_RemovesCharacterFromCrowd() 
        {
            InitializeDefaultList(true);
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdMember = (characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel).CrowdMemberCollection[0] as CrowdMember; // Selecting Scarecrow to delete
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[1].CrowdMemberCollection[1] as CrowdModel; // The Narrows is the selected crowd
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            List<CrowdModel> crowdList = characterExplorerViewModel.CrowdCollection.ToList();
            IEnumerable<ICrowdMember> baseCrowdList = crowdList;
            CountNumberOfCrowdMembersByName(baseCrowdList.ToList(), "Scarecrow");
            Assert.IsTrue(this.numberOfItemsFound == 1); // There is one occurrance of Scarecrow
            var existingChar = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection.Where(cm => cm.Name == "Scarecrow").FirstOrDefault();
            Assert.IsNotNull(existingChar); // The only one occurrance is in All Characters crowd
        }
        /// <summary>
        /// User should be prompted before deleting a character from All Characters
        /// </summary>
        [TestMethod]
        public void DeleteCharacter_PromptsUserBeforeRemovingFromAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMember; // Selecting Batman to delete
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0] as CrowdModel;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            messageBoxServiceMock.Verify(
                msgservice => msgservice.ShowDialog(It.Is<string>(s => s == Messages.DELETE_CHARACTER_FROM_ALL_CHARACTERS_CONFIRMATION_MESSAGE),
                    It.Is<string>(s => s == Messages.DELETE_CHARACTER_CAPTION), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once);
        }
        /// <summary>
        /// If deleting from All Characters, the character should be removed from repository
        /// </summary>
        [TestMethod]
        public void DeleteCharacter_RemovesCharacterFromRepositoryIfDeletingFromAllCharacters()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            InitializeMessageBoxService(MessageBoxResult.Yes); // Pre-configuring message box to confirm delete request
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMember; // Selecting Batman to delete
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]as CrowdModel;
            characterExplorerViewModel.DeleteCharacterCrowdCommand.Execute(null);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList => 
                        cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c=>c.Name == "Batman").FirstOrDefault() == null)));
        }

        #endregion

        #region Filter Character Tests
        public void FilterCharacter_ReturnsFilteredListOfCrowdMemberAndCrowds() { }

        #endregion

        #region Rename Character/Crowd Tests
        /// <summary>
        /// Repository should be updated properly after each rename
        /// </summary>
        [TestMethod]
        public void RenameCharacterCrowd_UpdatesRepoCorrectly() 
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMember; // Selecting Batman to Rename
            characterExplorerViewModel.EnterEditModeCommand.Execute(null);
            System.Windows.Controls.TextBox txtBox = new System.Windows.Controls.TextBox();
            txtBox.Text = "Bat";
            characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c => c.Name == "Batman").FirstOrDefault() == null)));
            crowdRepositoryMock.Verify(
                repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                    It.Is<List<CrowdModel>>(cmList =>
                        cmList.Where(cm => cm.Name == Constants.ALL_CHARACTER_CROWD_NAME).First().CrowdMemberCollection.Where(c => c.Name == "Bat").FirstOrDefault() != null)));
        }
        /// <summary>
        /// Crowd or character should not be renamed to another crowd or character that already exists
        /// </summary>
        [TestMethod]
        public void RenameCharacterCrowd_PreventsDuplication()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdMember = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection[0] as CrowdMember; // Selecting Batman to Rename
            characterExplorerViewModel.EnterEditModeCommand.Execute(null);
            System.Windows.Controls.TextBox txtBox = new System.Windows.Controls.TextBox();
            txtBox.Text = "Robin"; // Trying to set a name that already exists
            characterExplorerViewModel.SubmitCharacterCrowdRenameCommand.Execute(txtBox);
            messageBoxServiceMock.Verify(
                msgservice => msgservice.ShowDialog(It.Is<string>(s => s == Messages.DUPLICATE_NAME_MESSAGE),
                    It.Is<string>(s => s == Messages.DUPLICATE_NAME_CAPTION), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once); // Check if user was prompted
            var characters = characterExplorerViewModel.CrowdCollection[0].CrowdMemberCollection.Where(c => c.Name == "Robin");
            Assert.IsTrue(characters.Count() == 1); // There should be only one character with name Robin
            crowdRepositoryMock.Verify(
               repo => repo.SaveCrowdCollection(It.IsAny<Action>(),
                   It.IsAny<List<CrowdModel>>()), Times.Never); // Repository should not be called as the rename is cancelled
        }
        /// <summary>
        /// All Characters crowd cannot be renamed
        /// </summary>
        [TestMethod]
        public void RenameCharacterCrowd_PreventsAllCharactersRename()
        {
            InitializeCrowdRepositoryMockWithDefaultList();
            characterExplorerViewModel = new CharacterExplorerViewModel(busyServiceMock.Object, unityContainerMock.Object, messageBoxServiceMock.Object, crowdRepositoryMock.Object, eventAggregatorMock.Object);
            characterExplorerViewModel.SelectedCrowdModel = characterExplorerViewModel.CrowdCollection[0]; // Selecting All Characters to rename
            characterExplorerViewModel.SelectedCrowdMember = null;
            bool canRename = characterExplorerViewModel.EnterEditModeCommand.CanExecute(null);
            Assert.IsFalse(canRename);
        }
        #endregion

        #region Spawn Character In Crowd Tests
        public void SpawnCharacterInCrowd_AssignsLabelWithBothCharacterAndCrowdName() { }

        #endregion

        #region Save Placement of Character Tests
        public void SavePlacementOfCharacter_AssignsLocationToCrowdmembershipBasedOnCurrentPositionAndSavesCrowdmembershipToCrowdRepo() { }

        #endregion

        #region Place Character Tests
        public void PlaceCharacter_MovesCharacterToPositionBasedOnSavedLocation() { }

        #endregion

        #region Link and Paste Character Tests
        public void LinkAndPasteCharacterAcrossCharacters_AddsNewCrowdMemberWithCopiedCharacterToPastedCrowd() { }

        #endregion

        #region Command Delegation in Characters from Crowd Tests
        public void ExecutingSpawnOrSaveOrPlaceOrRemoveOnACrowd_ActivatesTheCommandOnAllCrowdmembersInCrowd() { }

        #endregion
    }
#endregion
}
