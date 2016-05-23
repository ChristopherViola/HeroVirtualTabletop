﻿using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.Shared;
using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public interface IAnimationElement
    {
        Character Owner { get; set; }
        int Order { get; set; }

        string Play(bool completeEvent = true);
    }

    public class AnimationElement : NotifyPropertyChanged, IAnimationElement
    {
        public AnimationElement(int order = 1, Character owner = null)
        {
            this.Order = order;
            this.Owner = owner;
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

        public virtual string Play(bool completeEvent = true)
        {
            return "Playing " + this.Order + " for " + this.Owner.Name;
        }
    }

    public class PauseElement : AnimationElement
    {
        public PauseElement(int time, int order = 1, Character owner = null)
            : base(order, owner)
        {
            this.Time = time;
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

        public override string Play(bool completeEvent = true)
        {
            System.Threading.Thread.Sleep(Time);
            return string.Empty;
        }
    }

    public class SoundElement : AnimationElement
    {
        public SoundElement(string soundFile, int order = 1, Character owner = null)
            : base(order, owner)
        {
            this.SoundFile = soundFile;
        }

        private string soundFile;
        public string SoundFile
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

        public override string Play(bool completeEvent = true)
        {
            SoundPlayer player = new SoundPlayer(SoundFile);
            player.PlaySync();
            return base.Play(completeEvent);
        }
    }

    public class MOVElement : AnimationElement
    {
        public MOVElement(string MOVResource, int order = 1, Character owner = null)
            : base(order, owner)
        {
            this.MOVResource = MOVResource;
        }

        private string movResource;
        public string MOVResource
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

        public override string Play(bool completeEvent = true)
        {
            KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
            string keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, MOVResource);
            if (completeEvent)
            {
                keybind = keyBindsGenerator.CompleteEvent();
            }
            return keybind;
        }
    }

    public class FXEffectElement : AnimationElement
    {

        public FXEffectElement(string effect, bool persistent = false, bool playWithNext = false, int order = 1, Character owner = null)
            : base(order, owner)
        {
            this.Effect = effect;
            this.Persistent = persistent;
            this.PlayWithNext = playWithNext;
            this.Colors = new Color[4];
            this.Colors.Initialize();
        }

        private Color[] colors;
        public Color[] Colors
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

        private string effect;
        public string Effect
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

        public string CostumeText
        {
            get
            {
                string name = Owner.Name;
                string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
                string file = name + Constants.GAME_COSTUMES_EXT;
                string newFolder = Path.Combine(location, name);
                string FXName = ParseFXName(Effect);
                string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", newFolder, FXName, Constants.GAME_COSTUMES_EXT));
                return File.ReadAllText(newFile);
            }
        }

        public override string Play(bool completeEvent = true)
        {
            string keybind = string.Empty;
            string name = Owner.Name;
            string location = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME);
            string file = name + Constants.GAME_COSTUMES_EXT;
            string origFile = Path.Combine(location, file);
            string newFolder = Path.Combine(location, name);
            string FXName = ParseFXName(Effect);
            string newFile = Path.Combine(newFolder, string.Format("{0}_{1}{2}", newFolder, FXName, Constants.GAME_COSTUMES_EXT));

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
                KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();
                keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.LoadCostume, fxCostume);
                if (PlayWithNext == false && completeEvent)
                {
                    keybind = keyBindsGenerator.CompleteEvent();
                }
            }
            if (Persistent)
            {
                archiveOriginalCostumeFileAndSwapWithModifiedFile(name, newFile);
            }
            return keybind;
        }

        private void archiveOriginalCostumeFileAndSwapWithModifiedFile(string name, string newFile)
        {
            string origFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + Constants.GAME_COSTUMES_EXT);
            string archFile = Path.Combine(
                Settings.Default.CityOfHeroesGameDirectory,
                Constants.GAME_COSTUMES_FOLDERNAME,
                name + "_original" + Constants.GAME_COSTUMES_EXT);
            if (File.Exists(archFile))
            {
                File.Copy(origFile, archFile, true);
            }
            File.Copy(newFile, origFile, true);

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
            string output = re.Replace(fileStr, fxNew, 1);
            int fxPos = output.IndexOf(fxNew);
            int colorStart = output.IndexOf("Color1", fxPos);
            int colorEnd = output.IndexOf("}", fxPos);
            string outputStart = output.Substring(0, colorStart - 1);
            string outputEnd = output.Substring(colorEnd);
            string outputColors =
                string.Format("Color1 {0}, {1}, {2}\n" +
                    "\tColor2 {3}, {4}, {5}\n" +
                    "\tColor3 {6}, {7}, {8}\n" +
                    "\tColor4 {9}, {10}, {11}\n",
                    Colors[0].R, Colors[0].G, Colors[0].B,
                    Colors[1].R, Colors[1].G, Colors[1].B,
                    Colors[2].R, Colors[2].G, Colors[2].B,
                    Colors[3].R, Colors[3].G, Colors[3].B
                    );
            output = outputStart + outputColors + outputEnd;
            File.AppendAllText(newFile, output);
        }
    }
    
    public class NestedAnimationElement : AnimationElement
    {
        public NestedAnimationElement(AnimationSequenceType seqType = AnimationSequenceType.And, int order = 1, Character owner = null)
            : base(order, owner)
        {
            this.SequenceType = seqType;
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

        private SortableObservableCollection<IAnimationElement, int> animationElements;
        public SortableObservableCollection<IAnimationElement, int> AnimationElements
        {
            get
            {
                return animationElements;
            }
            set
            {
                animationElements = value;
                OnPropertyChanged("AnimationElements");
            }
        }

        public override string Play(bool completeEvent = true)
        {
            if (SequenceType == AnimationSequenceType.And)
            {
                AnimationElements.Sort(System.ComponentModel.ListSortDirection.Ascending, x => x.Order);
                string retVal = string.Empty;
                foreach (IAnimationElement item in AnimationElements)
                {
                    retVal += item.Play(completeEvent);
                }
                return retVal;
            }
            else
            {
                var rnd = new Random();
                int chosen = rnd.Next(AnimationElements.Count - 1);
                return AnimationElements[chosen].Play(completeEvent);
            }
        }
    }
}
