﻿/*  Auditory Training Games in Silverlight
    Copyright (C) 2008-2012 Nicolas Van Labeke (LSRI, Nottingham University)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Windows;
namespace Kindohm.KSynth.Library
{
    public class AttenuatorBase : DependencyObject
    {
        const int attentuationConstant = 65536;
        double attenuation = 0;        // in db
        int attenuationMultiplier = attentuationConstant;

        public double Attenuation
        {
            set
            {
                attenuation = value;
                attenuationMultiplier = (int)(attentuationConstant * Math.Pow(10, attenuation / 20.0));
            }
            get
            {
                return attenuation;
            }
        }

        protected StereoSample Attenuate(StereoSample sample)
        {
            sample.LeftSample = (short)((sample.LeftSample * attenuationMultiplier) >> 16);
            sample.RightSample = (short)((sample.RightSample * attenuationMultiplier) >> 16);
            return sample;
        }

    }
}
