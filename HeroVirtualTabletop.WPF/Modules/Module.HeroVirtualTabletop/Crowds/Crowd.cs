﻿using Framework.WPF.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framework.WPF.Extensions;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Characters;
using Module.Shared;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowd : ICrowdMember
    {
        ReadOnlyHashedObservableCollection<ICrowdMember, string> CrowdMemberCollection { get; }
    }

    public class Crowd : NotifyPropertyChanged, ICrowd
    {
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                OldName = name;
                name = value;
                OnPropertyChanged("Name");
            }
        }

        [JsonIgnore]
        public string OldName { get; private set; }

        private Crowd rosterCrowd;
        [JsonIgnore]
        public Crowd RosterCrowd
        {
            get
            {
                return rosterCrowd;
            }

            set
            {
                rosterCrowd = value;
                OnPropertyChanged("RosterCrowd");
            }
        }

        private HashedObservableCollection<ICrowdMember, string> crowdMemberCollection;
        public ReadOnlyHashedObservableCollection<ICrowdMember, string> CrowdMemberCollection { get; private set; }
        
        public Crowd()
        {
            this.crowdMemberCollection = new HashedObservableCollection<ICrowdMember, string>(x => x.Name);
            this.CrowdMemberCollection = new ReadOnlyHashedObservableCollection<ICrowdMember, string>(crowdMemberCollection);
        }

        public Crowd(string name) : this()
        {
            this.Name = name;
        }
        
        public virtual ICrowdMember Clone()
        {
            Crowd crowd = this.DeepClone() as Crowd;
            return crowd;
        }

        public virtual void SavePosition()
        {
            foreach (ICrowdMember crowdMember in this.CrowdMemberCollection)
                crowdMember.SavePosition();
        }

        public virtual void SavePosition(ICrowdMember c)
        { 
        
        }

        public virtual void Place(IMemoryElementPosition position)
        {

        }

        public virtual void Place(ICrowdMember crowdMember)
        {

        }
    }

    public class CrowdModel : Crowd, ICrowdMemberModel
    {
        [JsonProperty(PropertyName = "Members")]
        private HashedObservableCollection<ICrowdMemberModel, string> crowdMemberCollection;
        public new ReadOnlyHashedObservableCollection<ICrowdMemberModel, string> CrowdMemberCollection { get; private set; }
        
        public void Add(ICrowdMemberModel member)
        {
            this.crowdMemberCollection.Add(member);
            this.crowdMemberCollection.Sort();
            member.PropertyChanged += Member_PropertyChanged;
        }

        public void Add(IEnumerable<ICrowdMemberModel> members)
        {
            foreach (ICrowdMemberModel member in members)
            {
                Add(member);
            }
        }

        private void Member_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                crowdMemberCollection.UpdateKey((sender as ICrowdMember).OldName, (sender as ICrowdMember).Name);
            }
            if (e.PropertyName == "Name" || e.PropertyName == "Order")
            {
                crowdMemberCollection.Sort();
            }
        }

        [OnDeserialized]
        private void AfterDeserializationCallback(StreamingContext context)
        {
            foreach (ICrowdMemberModel member in crowdMemberCollection)
            {
                member.PropertyChanged += Member_PropertyChanged;
            }
        }

        public void Remove(ICrowdMemberModel member)
        {
            if (member == null)
                return;
            this.crowdMemberCollection.Remove(member);
            member.PropertyChanged -= Member_PropertyChanged;
        }

        private Dictionary<string, IMemoryElementPosition> savedPositions;
        public Dictionary<string, IMemoryElementPosition> SavedPositions
        {
            get
            {
                if (savedPositions == null)
                    savedPositions = new Dictionary<string, IMemoryElementPosition>();
                return savedPositions;
            }
            set
            {
                savedPositions = value;
            }
        }

        private bool isExpanded;
        [JsonIgnore]
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }

        private bool isMatched = true;
        [JsonIgnore]
        public bool IsMatched
        {
            get
            {
                return isMatched;
            }
            set
            {
                isMatched = value;
                OnPropertyChanged("IsMatched");
            }
        }

        private int order;
        public int Order
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
                OnPropertyChanged("Order");
            }
        }

        public void ApplyFilter(string filter)
        {
            if (alreadyFiltered == true && isMatched == true)
            {
                return;
            }
            if (string.IsNullOrEmpty(filter))
            {
                IsMatched = true;
            }
            else
            {
                Regex re = new Regex(filter, RegexOptions.IgnoreCase);
                IsMatched = re.IsMatch(Name);
            }
            if (IsMatched)
            {
                foreach (ICrowdMemberModel cm in CrowdMemberCollection)
                {
                    cm.ApplyFilter(string.Empty);
                }
            }
            else
            {
                foreach (ICrowdMemberModel cm in CrowdMemberCollection)
                {
                    cm.ApplyFilter(filter);
                }
                if (CrowdMemberCollection.Any(cm => { return (cm as ICrowdMemberModel).IsMatched; }))
                {
                    IsMatched = true;
                }
            }
            
            IsExpanded = IsMatched;
            alreadyFiltered = true;
        }

        private bool alreadyFiltered = false;
        public void ResetFilter()
        {
            alreadyFiltered = false;
            foreach (ICrowdMemberModel cm in CrowdMemberCollection)
            {
                cm.ResetFilter();
            }
        }

        public override ICrowdMember Clone()
        {
            CrowdModel crowdModel = this.DeepClone() as CrowdModel;
            return crowdModel;
        }
        public override void SavePosition()
        {
            foreach (ICrowdMember crowdMember in this.CrowdMemberCollection)
            {
                if (crowdMember is CrowdModel)
                    crowdMember.SavePosition();
                else
                {
                    this.SavePosition(crowdMember);
                }
            }
        }
        public override void SavePosition(ICrowdMember c)
        {
            var position = (c as Character).Position.Clone(false);
            if (this.SavedPositions.ContainsKey(c.Name))
                this.SavedPositions[c.Name] = position;
            else
                this.SavedPositions.Add(c.Name, (c as Character).Position.Clone(false));
        }
        public override void Place(IMemoryElementPosition position)
        {
            foreach (ICrowdMember crowdMember in this.CrowdMemberCollection)
            {
                if (crowdMember is Crowd)
                {
                    crowdMember.Place(null);
                }
                else
                {
                    crowdMember.Place(this.SavedPositions[crowdMember.Name]);
                }
            }
        }

        public override void Place(ICrowdMember crowdMember)
        {
            IMemoryElementPosition pos;
            if (this.SavedPositions.TryGetValue(crowdMember.Name, out pos))
            {
                CrowdMemberModel model = crowdMember as CrowdMemberModel;
                model.Position = pos.Clone(false, (model.Position as MemoryInstance).GetTargetPointer());
            }
            else if(this.Name == Constants.ALL_CHARACTER_CROWD_NAME)
            {
                CrowdMemberModel model = crowdMember as CrowdMemberModel;
                if(model.SavedPosition != null)
                {
                    MemoryInstance memIns = (model.Position as MemoryInstance);
                    uint x = memIns.GetTargetPointer();
                    model.Position = model.SavedPosition.Clone(false, x);
                }
                    
            }
        }
        public CrowdModel() : base()
        {
            this.crowdMemberCollection = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.Order, x => x.Name);
            this.CrowdMemberCollection = new ReadOnlyHashedObservableCollection<ICrowdMemberModel, string>(crowdMemberCollection);
        }
        public CrowdModel(string name, int order = 0) : base(name)
        {
            this.crowdMemberCollection = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.Order, x => x.Name);
            this.CrowdMemberCollection = new ReadOnlyHashedObservableCollection<ICrowdMemberModel, string>(crowdMemberCollection);
            this.order = order;
        }
    }
}
