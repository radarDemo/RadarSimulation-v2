using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace radarsystem
{
    public enum TabOptionEnum
    {
        SCEN=0,
        FEATURE=1
    }

    public enum NoiseEnum
    {
        NoNoise = -1,
        GUASSIAN=0,
        POISSON=1,
        UNIFORM=2
    }
    public enum Coordinate
    {
        X=0,
        Y=1
    }
    public enum Scene
    {
        DOPPLER = 0,
        MUTLIBASE = 1,
        BVR = 2,
        ACT_SONAR = 3,
        PAS_SONAR = 4,
        ELEC_VS = 5,
        COMMAND = 6
    }
}
