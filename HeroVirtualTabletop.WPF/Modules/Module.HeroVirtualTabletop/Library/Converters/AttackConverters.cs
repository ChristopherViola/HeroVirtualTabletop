﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using System.Windows;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    /*
         fa-ambulance [&#xf0f9;] - unconcious
         fa-bed [&#xf236;] - dying/dead
         fa-bullseye [&#xf140;] - defend?
         fa-frown-o [&#xf119;] - stunned
         fa-bolt [&#xf0e7;] - attack
         fa-bullseye [&#xf140;] - defend
    */
    public class ActiveAttackEffectToAnimationIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            AttackEffectOption activeAttackEffectOption = (AttackEffectOption)value;
            switch (activeAttackEffectOption)
            {
                case Enumerations.AttackEffectOption.Stunned:
                    iconText = "\uf119";
                    break;
                case Enumerations.AttackEffectOption.Unconcious:
                    iconText = "\uf0f9";
                    break;
                case Enumerations.AttackEffectOption.Dying:
                case Enumerations.AttackEffectOption.Dead:
                    iconText = "\uf236";
                    break;
            }
            return iconText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /*
         fa-bolt [&#xf0e7;] - attack
         fa-bullseye [&#xf140;] - defend
    */
    public class ActiveAttackModeToAnimationIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            AttackMode activeAttackMode = (AttackMode)value;
            switch (activeAttackMode)
            {
                case Enumerations.AttackMode.Attack:
                    iconText = "\uf0e7";
                    break;
                //case Enumerations.AttackMode.Defend:
                //    iconText = "\uf140";
                //    break;
            }
            return iconText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ActiveAttackModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = Visibility.Collapsed;
            AttackMode activeAttackMode = (AttackMode)value;
            if (activeAttackMode != Enumerations.AttackMode.None)
                visibility = Visibility.Visible;
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
