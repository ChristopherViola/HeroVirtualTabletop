﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Sevices
{
    public interface ITargetObserver
    {
        event EventHandler TargetChanged;
        uint CurrentTarget { get; }
    }
}
