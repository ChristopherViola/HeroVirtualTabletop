﻿using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.Shared.Enumerations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Identities
{
    public class Camera : Identity
    {
        public Camera(): base("V_Arachnos_Security_Camera", IdentityType.Model, "Camera")
        {
        }

        public string MoveToTarget(bool completeEvent = true)
        {
            string keybind = keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Follow, "");
            if (completeEvent)
            {
                return keyBindsGenerator.CompleteEvent();
            }
            return keybind;
        }

        public new string Render(bool completeEvent = true)
        {
            //We need to untarget everything before loading camera skin
            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.TargetEnemyNear);
            return base.Render(completeEvent);
        }

        protected internal static Position position = new Position(false, 1696336);
        private static Identity skin;

        private static string[] lastKeybinds;
        public string[] LastKeybinds
        {
            get
            {
                return lastKeybinds;
            }
        }

        private static Character maneuveredCharacter;
        public Character ManeuveredCharacter
        {
            get
            {
                return maneuveredCharacter;
            }
            set
            {
                string[] keybinds = new string[2];
                if (maneuveredCharacter != null)
                {
                    keybinds[0] = Render();
                    keybinds[1] = maneuveredCharacter.Spawn();
                    maneuveredCharacter = null;
                }
                if (value != null)
                {
                    maneuveredCharacter = value;
                    maneuveredCharacter.Target(false);
                    
                    keybinds[0] = MoveToTarget();

                    float dist = 13.23f, calculatedDistance, lastCalculatedDistance = 0f;
                    int maxRecalculationCount = 5; // We will allow the same distance to be calculated 5 times at max. After this we'll assume that the camera is stuck.
                    Hashtable distanceTable = new Hashtable();
                    while (position.IsWithin(dist, maneuveredCharacter.Position, out calculatedDistance) == false)
                    {
                        lastCalculatedDistance = calculatedDistance;
                        if(distanceTable.Contains(calculatedDistance))
                        {
                            int count = (int)distanceTable[calculatedDistance];
                            count++;
                            if (count > maxRecalculationCount)
                                break;
                            else
                                distanceTable[calculatedDistance] = count;
                        }
                        else
                        {
                            distanceTable.Add(calculatedDistance, 1);
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                    distanceTable.Clear();
                    maneuveredCharacter.ClearFromDesktop(true, true);
                    skin = value.ActiveIdentity;
                    keybinds[1] = skin.Render();
                }
                lastKeybinds = keybinds;
            }
        }
    }
}
