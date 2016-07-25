﻿using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Media;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Identities;
using Framework.WPF.Extensions;
using System.Threading;
using Module.HeroVirtualTabletop.OptionGroups;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System.Runtime.Serialization;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public interface IAnimationElement
    {
        string Name { get; set; }
        Character Owner { get; set; }
        int Order { get; set; }
        AnimationType Type { get; set; }
        bool PlayWithNext { get; set; }
        AnimationResource Resource { get; set; }
        bool IsActive { get; }
        bool Persistent { get; }

        void Play(bool persistent = false, Character Target = null, bool forcePlay = false);
        void Stop(Character Target = null);
    }

    public class AnimationElement : CharacterOption, IAnimationElement
    {
        [JsonConstructor]
        private AnimationElement() { }

        public AnimationElement(string name, bool persistent = false, int order = 1, Character owner = null)
        {
            this.Name = name;
            this.Order = order;
            this.Owner = owner;
            this.Persistent = persistent;
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

        private Character owner;
        public Character Owner
        {
            get
            {
                return owner;
            }

            set
            {
                owner = value;
                OnPropertyChanged("Owner");
            }
        }

        private AnimationType type;
        public AnimationType Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        private bool playWithNext;
        public bool PlayWithNext
        {
            get
            {
                return playWithNext;
            }
            set
            {
                playWithNext = value;
                OnPropertyChanged("PlayWithNext");
            }
        }

        private bool isActive;
        [JsonIgnore]
        public virtual bool IsActive
        {
            get
            {
                return isActive;
            }
            protected set
            {
                isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        private string displayName;
        public virtual string DisplayName
        {
            get
            {
                return displayName;
            }

            set
            {
                displayName = value;
                OnPropertyChanged("DisplayName");
            }
        }

        private bool playOnTargeted;
        public bool PlayOnTargeted
        {
            get
            {
                return playOnTargeted;
            }
            set
            {
                playOnTargeted = value;
                OnPropertyChanged("PlayOnTargeted");
            }
        }

        private bool persistent;
        public bool Persistent
        {
            get
            {
                return persistent;
            }
            set
            {
                persistent = value;
                OnPropertyChanged("Persistent");
            }
        }

        //private AnimationResource resource;
        public AnimationResource Resource
        {
            get
            {
                //return resource;
                return GetResource();
            }
            set
            {
                //resource = value;
                SetResource(value);
                OnPropertyChanged("Resource");
            }
        }

        public virtual void Play(bool persistent = false, Character Target = null, bool forcePlay = false)
        {

        }

        public virtual void Stop(Character Target = null)
        {

        }

        protected virtual AnimationResource GetResource()
        {
            return string.Empty;
        }

        protected virtual void SetResource(AnimationResource value)
        {

        }

        public virtual AnimationElement Clone()
        {
            AnimationElement clonedElement = GetNewAnimationElement();
            clonedElement.DisplayName = this.DisplayName;
            //clonedElement.Resource = this.Resource;
            clonedElement.Type = this.Type;
            return clonedElement;
        }

        public virtual AnimationElement GetNewAnimationElement()
        {
            return new AnimationElement(this.Name, this.Persistent);
        }

    }

    public class PauseElement : AnimationElement
    {
        [JsonConstructor]
        private PauseElement() : base(string.Empty) { }

        public PauseElement(string name, int time, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Time = time;
            this.Type = AnimationType.Pause;
        }

        private int time;
        public int Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
                OnPropertyChanged("Time");
            }
        }

        public override void Play(bool persistent = false, Character Target = null, bool forcePlay = false)
        {
            IsActive = true;
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = Time;
            bool done = false;
            timer.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e) { done = true; };
            timer.Start();
            while (!done)
            {
                continue;
            }
            IsActive = false;
        }

        protected override AnimationResource GetResource()
        {
            return Time.ToString();
        }

        protected override void SetResource(AnimationResource value)
        {
            int x;
            if (int.TryParse(value, out x))
                Time = x;
        }
        public override AnimationElement GetNewAnimationElement()
        {
            return new PauseElement(this.Name, this.Time, this.Persistent);
        }
    }

    public class SoundElement : AnimationElement
    {
        [JsonConstructor]
        private SoundElement() : base(string.Empty) { }

        public SoundElement(string name, AnimationResource soundFile, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.SoundFile = soundFile;
            this.Type = AnimationType.Sound;
        }

        private AnimationResource soundFile;
        [JsonIgnore]
        public AnimationResource SoundFile
        {
            get
            {
                return soundFile;
            }
            set
            {
                soundFile = value;
                OnPropertyChanged("SoundFile");
            }
        }

        private SoundEffect soundEffect;
        private SoundEffectInstance soundEffectInstance;
        private Task setVolumeTask;
        private CancellationTokenSource tokenSrc;

        public override void Play(bool persistent = false, Character Target = null, bool forcePlay = false)
        {
            Character target = Target ?? this.Owner;
            Stop(target);
            if (((string)SoundFile) == null) //|| !File.Exists(SoundFile)
            {
                return;
            }
            if (soundEffect == null)
            {
                using (FileStream s = File.OpenRead(SoundFile))
                    soundEffect = SoundEffect.FromStream(s);//gm.Content.Load<SoundEffect>(SoundFile);
                soundEffectInstance = soundEffect.CreateInstance();
            }
            tokenSrc = new CancellationTokenSource();

            float dist = 0;
            float volume = 1.0f;
            target.Position.IsWithin(0, Camera.Position, out dist);
            if (dist > 100)
                volume = 0;
            else
                volume = (100 - dist) / 100;
            if (float.IsNaN(volume))
                volume = 1.0f;

            soundEffectInstance.Volume = volume;

            //setVolumeTask = Task.Run(delegate ()
            //{
            //    PauseElement pause = new PauseElement("pasue", time: 500);
            //    while (true)
            //    {
            //        float dist = 0;
            //        float volume = 1.0f;
            //        target.Position.IsWithin(0, Camera.Position, out dist);
            //        if (dist > 100)
            //            volume = 0;
            //        else
            //            volume = (100 - dist) / 100;
            //        if (soundEffectInstance == null || float.IsNaN(volume))
            //            break;
            //        soundEffectInstance.Volume = volume;
            //        pause.Play();
            //    }
            //}, tokenSrc.Token);

            if (this.Persistent || persistent)
            {
                soundEffectInstance.IsLooped = true;
                IsActive = true;
            }
            Task.Factory.StartNew(() => { soundEffectInstance.Play(); }, tokenSrc.Token);
        }

        public override void Stop(Character Target = null)
        {
            Character target = Target ?? this.Owner;
            if (IsActive)
            {
                IsActive = false;
            }

            if (soundEffectInstance != null)
            {
                tokenSrc.Cancel();
                if (soundEffectInstance.State == SoundState.Playing)
                    soundEffectInstance.Stop();
                soundEffect.Dispose();
                soundEffectInstance.Dispose();
                soundEffect = null;
                soundEffectInstance = null;
            }
        }

        protected override AnimationResource GetResource()
        {
            return SoundFile;
        }

        protected override void SetResource(AnimationResource value)
        {
            SoundFile = value;
        }

        public override AnimationElement GetNewAnimationElement()
        {
            return new SoundElement(this.Name, this.SoundFile, this.Persistent);
        }
    }

    public class MOVElement : AnimationElement
    {
        [JsonConstructor]
        private MOVElement() : base(string.Empty) { }

        public MOVElement(string name, AnimationResource MOVResource, bool persistent = false, bool playWithNext = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.MOVResource = MOVResource;
            this.Type = AnimationType.Movement;
        }

        private AnimationResource movResource;
        [JsonIgnore]
        public AnimationResource MOVResource
        {
            get
            {
                return movResource;
            }
            set
            {
                movResource = value;
                OnPropertyChanged("MOVResource");
            }
        }

        public override void Play(bool persistent = true, Character Target = null, bool forcePlay = false)
        {
            Stop(Target);
            Character target = Target ?? this.Owner;
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            target.Target(false);
            string keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, MOVResource);
            IsActive = true;
            if (PlayWithNext == false || forcePlay)
                keybind = keyBindsGenerator.CompleteEvent();
        }

        public override void Stop(Character Target = null)
        {
            if (IsActive)
            {
                Character target = Target ?? this.Owner;
                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                target.Target(false);
                keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                IsActive = false;
                keyBindsGenerator.CompleteEvent();
            }
        }

        protected override AnimationResource GetResource()
        {
            return MOVResource;
        }

        protected override void SetResource(AnimationResource value)
        {
            MOVResource = value;
        }

        public override AnimationElement GetNewAnimationElement()
        {
            return new MOVElement(this.Name, this.MOVResource, this.Persistent);
        }
    }

    public class FXEffectElement : AnimationElement
    {
        [JsonConstructor]
        private FXEffectElement() : base(string.Empty) { }

        public FXEffectElement(string name, AnimationResource effect, bool persistent = false, bool playWithNext = false,
            int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Effect = effect;
            this.Type = AnimationType.FX;
            this.PlayWithNext = playWithNext;
            System.Windows.Media.Color black = System.Windows.Media.Color.FromRgb(0, 0, 0);
            this.colors = new ObservableCollection<System.Windows.Media.Color>() { black, black, black, black };
        }

        private ObservableCollection<System.Windows.Media.Color> colors;
        public ObservableCollection<System.Windows.Media.Color> Colors
        {
            get
            {
                return colors;
            }
            set
            {
                colors = value;
                OnPropertyChanged("Colors");
            }
        }

        private AnimationResource effect;
        [JsonIgnore]
        public AnimationResource Effect
        {
            get
            {
                return effect;
            }
            set
            {
                effect = value;
                OnPropertyChanged("Effect");
            }
        }

        [JsonIgnore]
        public AttackDirection AttackDirection
        {
            get;
            set;
        }

        [JsonIgnore]
        public string CostumeText
        {
            get
            {
                if (Owner == null)
                {
                    return string.Empty;
                }
                string name = Owner.Name;
                string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
                string file = name + Constants.GAME_COSTUMES_EXT;
                string newFolder = Path.Combine(location, name);
                string FXName = ParseFXName(Effect);
                string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", name, FXName, Constants.GAME_COSTUMES_EXT));
                if (File.Exists(newFile))
                    return File.ReadAllText(newFile);
                return string.Empty;
            }
        }

        private ReaderWriterLockSlim fileLock = new ReaderWriterLockSlim();
        public override void Play(bool persistent = false, Character Target = null, bool forcePlay = false)
        {
            Character target = Target ?? this.Owner;
            if (target.ActiveIdentity.Type != IdentityType.Costume)
                return;
            Stop(Target);
            target.Target(false);
            string name = target.ActiveIdentity.Surface;
            string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            string file = name + Constants.GAME_COSTUMES_EXT;
            string origFile = Path.Combine(location, file);
            string newFolder = Path.Combine(location, name);
            string FXName = ParseFXName(Effect);
            string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", name, FXName, Constants.GAME_COSTUMES_EXT));
            fileLock.EnterWriteLock();
            try
            {
                if (!Directory.Exists(newFolder))
                {
                    Directory.CreateDirectory(newFolder);
                }
                if (File.Exists(newFile))
                {
                    File.Delete(newFile);
                }

                if (File.Exists(origFile))
                {
                    insertFXIntoCharacterCostumeFile(origFile, newFile);
                    string fxCostume = Path.Combine(name, string.Format("{0}_{1}", name, FXName));
                    string fireCoOrdinates = null;
                    if (this.AttackDirection != null)
                    {
                        fireCoOrdinates = string.Format("x={0} y={1} z={2}", this.AttackDirection.AttackDirectionX, this.AttackDirection.AttackDirectionY, this.AttackDirection.AttackDirectionZ);
                    }
                    KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                    keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, fxCostume, fireCoOrdinates);
                    if (PlayWithNext == false || forcePlay)
                    {
                        keyBindsGenerator.CompleteEvent();
                    }
                    IsActive = true;
                }
                if (Persistent || persistent)
                {
                    archiveOriginalCostumeFileAndSwapWithModifiedFile(name, newFile);
                }
            }
            finally
            {
                fileLock.ExitWriteLock();
            }
        }

        public override void Stop(Character Target = null)
        {
            Character target = Target ?? this.Owner;
            if (IsActive)
            {
                fileLock.EnterWriteLock();
                try
                {
                    reloadOriginalCostumeFile(target.ActiveIdentity.Surface);
                }
                finally
                {
                    fileLock.ExitWriteLock();
                }
                IsActive = false;
            }
        }

        private void archiveOriginalCostumeFile(string name)
        {
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + Constants.GAME_COSTUMES_EXT);
            string archFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + "_original" + Constants.GAME_COSTUMES_EXT);
            if (!File.Exists(archFile))
            {
                File.Copy(origFile, archFile, true);
            }
        }

        private void archiveOriginalCostumeFileAndSwapWithModifiedFile(string name, string newFile)
        {
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + Constants.GAME_COSTUMES_EXT);

            archiveOriginalCostumeFile(name);
            File.Copy(newFile, origFile, true);
        }

        private void reloadOriginalCostumeFile(string name)
        {
            string archFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + "_original" + Constants.GAME_COSTUMES_EXT);
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + Constants.GAME_COSTUMES_EXT);
            if (File.Exists(archFile))
            {
                File.Copy(archFile, origFile, true);
            }
            File.AppendAllText(origFile, "\n");
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, name);
            keyBindsGenerator.CompleteEvent();
        }

        private string ParseFXName(string effect)
        {
            Regex re = new Regex(@"\w+.fx");
            return re.Match(effect).Value;
        }

        private void insertFXIntoCharacterCostumeFile(string origFile, string newFile)
        {
            string fileStr = File.ReadAllText(origFile);
            string fxNone = "Fx none";
            string fxNew = "Fx " + Effect;
            Regex re = new Regex(Regex.Escape(fxNone));
            if (!re.IsMatch(fileStr))
            {
                fileStr +=
@"CostumePart """"
{
    Fx none
    Geometry none
    Texture1 none
    Texture2 none
    Color1  0,  0,  0
    Color2  0,  0,  0
    Color3  0,  0,  0
    Color4  0,  0,  0
}";
            }
            string output = re.Replace(fileStr, fxNew, 1);
            int fxPos = output.IndexOf(fxNew);
            int colorStart = output.IndexOf("Color1", fxPos);
            int colorEnd = output.IndexOf("}", fxPos);
            string outputStart = output.Substring(0, colorStart - 1);
            string outputEnd = output.Substring(colorEnd);
            string outputColors =
                string.Format("\tColor1 {0}, {1}, {2}" + Environment.NewLine +
                    "\tColor2 {3}, {4}, {5}" + Environment.NewLine +
                    "\tColor3 {6}, {7}, {8}" + Environment.NewLine +
                    "\tColor4 {9}, {10}, {11}" + Environment.NewLine,
                    Colors[0].R, Colors[0].G, Colors[0].B,
                    Colors[1].R, Colors[1].G, Colors[1].B,
                    Colors[2].R, Colors[2].G, Colors[2].B,
                    Colors[3].R, Colors[3].G, Colors[3].B
                    );
            output = outputStart + outputColors + outputEnd;
            File.AppendAllText(newFile, output);
        }

        protected override AnimationResource GetResource()
        {
            return Effect;
        }

        protected override void SetResource(AnimationResource value)
        {
            Effect = value;
        }

        public override AnimationElement Clone()
        {
            FXEffectElement clonedElement = new FXEffectElement(this.Name, this.Resource, this.Persistent, this.PlayWithNext);
            clonedElement.DisplayName = this.DisplayName;
            clonedElement.Type = this.Type;
            clonedElement.Colors = this.Colors.DeepClone() as ObservableCollection<System.Windows.Media.Color>;
            return clonedElement;
        }
    }

    public class SequenceElement : AnimationElement
    {
        [JsonConstructor]
        private SequenceElement() : base(string.Empty)
        {
            Initialize();
        }

        public SequenceElement(string name, AnimationSequenceType seqType = AnimationSequenceType.And, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            Initialize();
            this.SequenceType = seqType;
            this.Type = AnimationType.Sequence;
        }

        private void Initialize()
        {
            this.animationElements = new HashedObservableCollection<AnimationElement, string>(x => x.Name, x => x.Order);
            this.AnimationElements = new ReadOnlyHashedObservableCollection<AnimationElement, string>(animationElements);
        }

        private AnimationSequenceType sequenceType;
        public AnimationSequenceType SequenceType
        {
            get
            {
                return sequenceType;
            }
            set
            {
                sequenceType = value;
                OnPropertyChanged("SequenceType");
            }
        }

        [JsonProperty(PropertyName = "AnimationElements")]
        protected HashedObservableCollection<AnimationElement, string> animationElements;
        [JsonIgnore]
        public ReadOnlyHashedObservableCollection<AnimationElement, string> AnimationElements { get; protected set; }

        public int LastOrder
        {
            get
            {
                if (animationElements.Count > 0)
                    return animationElements.Max(x => x.Order);
                else
                    return 0;
            }
        }

        public void AddAnimationElement(AnimationElement element, int order = 0)
        {
            element.Owner = this.Owner;
            if (order == 0)
                element.Order = this.LastOrder + 1;
            else
            {
                foreach (var elem in this.AnimationElements.Where(a => a.Order >= order))
                    elem.Order += 1;
                element.Order = order;
            }
            this.animationElements.Add(element);
            element.PropertyChanged += Element_PropertyChanged;
            this.animationElements.Sort();
            this.FixPlayWithNextForElements(element);
        }

        private void Element_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private void FixPlayWithNextForElements(AnimationElement element)
        {
            if (element != null && !(element is MOVElement || element is FXEffectElement))
            {
                // the previous element cannot be played with next
                int position = this.animationElements.IndexOf(element);
                if (position != 0)
                {
                    this.animationElements[position - 1].PlayWithNext = false;
                }
            }
            if (this.animationElements.Count > 0)
                this.animationElements[this.animationElements.Count - 1].PlayWithNext = false;
        }

        public void RemoveAnimationElement(AnimationElement element)
        {
            int position = this.animationElements.IndexOf(element);
            foreach (IAnimationElement elem in animationElements.Where(a => a.Order > element.Order))
                elem.Order -= 1;
            animationElements.Remove(element);
            element.PropertyChanged -= Element_PropertyChanged;
            if (this.animationElements.Count > position)
                FixPlayWithNextForElements(this.animationElements[position]);
            else
                FixPlayWithNextForElements(null);
        }

        [OnDeserialized]
        private void AfterDeserialized(StreamingContext stream)
        {
            foreach (AnimationElement element in this.animationElements)
            {
                element.PropertyChanged += Element_PropertyChanged;
            }
        }

        public void RemoveAnimationElement(string name)
        {
            AnimationElement element = animationElements[name];
            RemoveAnimationElement(element);
        }

        public override bool IsActive
        {
            get
            {
                return AnimationElements.Any(x => { return x.IsActive == true; });
            }
        }

        public override void Play(bool persistent = false, Character Target = null, bool forcePlay = false)
        {
            Stop(Target ?? this.Owner);
            //if (this.Persistent || persistent)
            //    IsActive = true;
            if (SequenceType == AnimationSequenceType.And)
            {
                //animationElements.Sort(System.ComponentModel.ListSortDirection.Ascending, x => x.Order);
                foreach (IAnimationElement item in AnimationElements.OrderBy(x => x.Order))
                {
                    item.Play(this.Persistent || persistent, Target ?? this.Owner);
                }
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(0, AnimationElements.Count);
                AnimationElements[chosen].Play(this.Persistent || persistent, Target ?? this.Owner);
            }
            OnPropertyChanged("IsActive");
        }

        public override void Stop(Character Target = null)
        {
            Character target = Target ?? this.Owner;
            //if (IsActive)
            //    IsActive = false;
            foreach (IAnimationElement item in AnimationElements.Where(x => x.IsActive))
            {
                item.Stop(target);
            }
            OnPropertyChanged("IsActive");
        }

        public override AnimationElement Clone()
        {
            SequenceElement seqClone = new SequenceElement(this.Name, this.SequenceType, this.Persistent);
            seqClone.DisplayName = this.DisplayName;
            foreach (var element in this.AnimationElements)
            {
                var clonedElement = (element as AnimationElement).Clone() as AnimationElement;
                seqClone.AddAnimationElement(clonedElement);
            }
            seqClone.animationElements = new HashedObservableCollection<AnimationElement, string>(seqClone.AnimationElements, x => x.Name, x => x.Order);
            seqClone.AnimationElements = new ReadOnlyHashedObservableCollection<AnimationElement, string>(seqClone.animationElements);
            return seqClone;
        }
    }

    public class ReferenceAbility : AnimationElement
    {
        public ReferenceAbility(string name, AnimatedAbility reference, bool persistent = false, int order = 1, Character owner = null)
            : base(name, persistent, order, owner)
        {
            this.Reference = reference;
            this.Type = AnimationType.Reference;
            this.ReferenceType = ReferenceType.Link;
        }

        private ReferenceType referenceType;
        public ReferenceType ReferenceType
        {
            get
            {
                return referenceType;
            }
            set
            {
                referenceType = value;
                OnPropertyChanged("ReferenceType");
            }
        }

        private AnimatedAbility reference;
        public AnimatedAbility Reference
        {
            get
            {
                return reference;
            }
            set
            {
                reference = value;
                OnPropertyChanged("Reference");
            }
        }

        public override bool IsActive
        {
            get
            {
                return Reference != null ? Reference.IsActive : false;
            }
        }

        public override void Play(bool persistent = false, Character Target = null, bool forcePlay = false)
        {
            string retVal = string.Empty;
            if (this.Reference != null)
            {
                this.Reference.Play(this.Persistent || persistent, Target ?? this.Owner);
            }
            OnPropertyChanged("IsActive");
        }

        public override void Stop(Character Target = null)
        {
            if (this.Reference != null)
                this.Reference.Stop(Target ?? this.Owner);
            OnPropertyChanged("IsActive");
        }

        protected override AnimationResource GetResource()
        {
            return new AnimationResource(this.reference, this.reference != null ? this.reference.Name : "");
        }

        protected override void SetResource(AnimationResource value)
        {
            this.Reference = value;
        }
        public override AnimationElement Clone()
        {
            ReferenceAbility clonedAbility = new ReferenceAbility(this.Name, this.Reference, this.Persistent, this.Order, this.Owner);
            clonedAbility.DisplayName = this.DisplayName;
            clonedAbility.PlayOnTargeted = this.PlayOnTargeted;
            clonedAbility.ReferenceType = ReferenceType.Link;
            clonedAbility.Type = AnimationType.Reference;
            return clonedAbility;
        }
    }



}
