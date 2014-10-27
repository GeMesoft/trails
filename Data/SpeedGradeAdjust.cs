﻿/*
Copyright (C) 2012 Gerhard Olsson

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;

namespace TrailsPlugin.Data
{
    //Some hints here: http://mymarathonpace.com/Other_Info.html

    public enum RunningGradeAdjustMethodEnum { None, MervynDavies, GregMaclin, MervynDaviesSpeed, Kay, JackDaniels, AlbertoMinetti, ACSM, Pandolf, Last };
    public static class RunningGradeAdjustMethodClass
    {
        public static float getGradeFactor(float g/*grade*/, float time, float prevTime, float dist, float prevDist)
        {

            float q;
            switch (TrailsPlugin.Data.Settings.RunningGradeAdjustMethod)
            {
                case RunningGradeAdjustMethodEnum.MervynDaviesSpeed:
                case RunningGradeAdjustMethodEnum.MervynDavies:
                case RunningGradeAdjustMethodEnum.GregMaclin:
                    q = getMervynDavies(g, time, prevTime, dist, prevDist);
                    break;

                case RunningGradeAdjustMethodEnum.Kay:
                //case RunningGradeAdjustMethodEnum.Kay2:
                    q = getKay(g, time, prevTime, dist, prevDist);
                    break;

                case RunningGradeAdjustMethodEnum.JackDaniels:
                    q = getJackDaniels(g, time, prevTime, dist, prevDist);
                    break;

                case RunningGradeAdjustMethodEnum.AlbertoMinetti:
                    q = getAlbertoMinetti(g, time, prevTime, dist, prevDist);
                    break;

                case RunningGradeAdjustMethodEnum.ACSM:
                    q = getACSM(g, time, prevTime, dist, prevDist);
                    break;

                case RunningGradeAdjustMethodEnum.Pandolf:
                    q = getPandolf(g, time, prevTime, dist, prevDist);
                    break;

                case RunningGradeAdjustMethodEnum.None:
                case RunningGradeAdjustMethodEnum.Last:
                default:
                    Debug.Assert(true);
                    q = 1;
                    break;
            }
            if (float.IsNaN(q) || q <= 0)
            {
                Debug.Assert(true);
                q = 0;
            }

            return q;
        }

        /***************************************************************************************************/
        private static float getMervynDavies(float g/*grade*/, float time, float prevTime, float dist, float prevDist)
        {
            //Mervyn Davies, Greg Maclin http://runningtimes.com/Article.aspx?ArticleID=10507 
            /* First, I tried to find out if anyone had done any serious research into the subject and ran across this article from Running Times: http://runningtimes.com/Article.aspx?ArticleID=10507 
             * This article mentions a researcher named Mervyn Davies who did extensive studies in the early 1980's of the effect of hills on the energy expenditure of runners. 
             * (Tim Noakes quotes Davies' research in his book the "Lore of Running".) Mervyn Davies' basic forumla for how much energy runners expend on hills is as follows: 
             * Every 1% of upgrade slows your pace 3.3% Every 1% of downgrade speeds your pace by 1.8% After playing around with this formula on elevation data for various marathons, 
             * I felt it needed to be "tweaked" a little for the much longer distance. Here is what I came up with: Miles 1 - 16: Every 1% of upgrade slows your pace 3.3% 
             * Every 1% of downgrade speeds your pace by 1.8% Miles 16 - 21: Every 1% of upgrade slows your pace 3.8% Every 1% of downgrade speeds your pace by 1.8% 
             * Miles 21 - 26.2: Every 1% of upgrade slows your pace 4.3% Every 1% of downgrade speeds your pace by 1.8% (The logic behind this is that uphills located late in 
             * the race will be more difficult than uphills located closer to the start.)
             */
            float q;
            //Formula adjusts pace, first implementation adjusted speed - works a little differently.
            bool speedAdjust = (Settings.RunningGradeAdjustMethod == RunningGradeAdjustMethodEnum.MervynDaviesSpeed);
            if (g > 0)
            {
                float q_md = 3.3f;
                float g0 = 0.1627f;
                float k0 = 0.0739f;
                if (Settings.RunningGradeAdjustMethod == RunningGradeAdjustMethodEnum.GregMaclin)
                {
                    if (dist > 21 * 1609)
                    {
                        q_md = 4.3f;
                        g0 = 0.142f;
                        k0 = 0.2259f;
                    }
                    else if (dist > 16 * 1609)
                    {
                        q_md = 3.8f;
                        g0 = 0.151f;
                        k0 = 0.1524f;
                    }
                }

                if (speedAdjust)
                {
                    //MD-speed will not work well when steep, by default giving infinite speed over 30%
                    //Use Kay formula instead for steep. This formula is normally above 31% but extrapolated and used from 16% (14%)
                    //Kay is always bigger than MD, the value is adjusted to be (almost) continous
                    if (g > g0)
                    {
                        q = getKay(g, time, prevTime, dist, prevDist, KayForce.MaxDown) - k0;
                    }
                    else
                    {
                        q = 1 - q_md * g;
                    }
                }
                else
                {
                    q = 1 / (1 + q_md * g);
                }
            }
            else
            {
                //downhill
                float q_md = 1.8f;
                float g0 = -0.08f;
                if (speedAdjust)
                {
                    g0 = -0.095f;
                }
                if (Settings.RunningGradeAdjustMethod == RunningGradeAdjustMethodEnum.GregMaclin && dist > 21 * 1609)
                {
                    q_md = 1.7f;
                }

                if (g < g0)
                {
                    //normal Kay when steep, formulas cross here (about fast downhill)
                    q = getKay(g, time, prevTime, dist, prevDist, KayForce.Normal);
                }
                else
                {
                    if (speedAdjust)
                    {
                        q = 1 - q_md * g;
                    }
                    else
                    {
                        q = 1 / (1 + q_md * g);
                    }
                }
            }

            return q;
        }

        /***************************************************************************************************/
        private enum KayForce { Normal, MaxUp, MaxDown };
        private static float getKay(float g, float time, float prevTime, float dist, float prevDist)
        {
            return getKay(g, time, prevTime, dist, prevDist, KayForce.Normal);
        }

        private static float getKay(float g, float time, float prevTime, float dist, float prevDist, KayForce force)
        {
            float q;
            //http://www.lboro.ac.uk/microsites/maths/research/preprints/papers11/11-38.pdf
            //http://www.zonefivesoftware.com/sporttracks/forums/viewtopic.php?p=85774&sid=cac957fef0d213becd6b06f6140cda0d#p85774

            double p0; //race record pace predict
            if (g > 0.3152f || force == KayForce.MaxUp)
            {
                //max uphill
                p0 = 1.9538 * g;
                //Alternate quartic function, very small difference
                //if (Settings.RunningGradeAdjustMethod == RunningGradeAdjustMethodEnum.Kay2)
                //{
                //    p0 = 0.0314 + 1.7544 * g + 0.3162 * g * g;
                //}
            }
            else if (g < -0.2617f || force == KayForce.MaxDown)
            {
                //Max downhill
                p0 = -0.8732 * g;
                //if (Settings.RunningGradeAdjustMethod == RunningGradeAdjustMethodEnum.Kay2)
                //{
                //    //Alternate quartic function, very small difference
                //    p0 = 0.1151 + 0.0061 * g + 1.6802 * g * g;
                //}
            }
            else
            {
                //Normal interval, formula
                p0 = 0.1707 + 0.5656 * g + 3.2209 * Math.Pow(g, 2) - 0.3211 * Math.Pow(g, 3) - 4.3635 * Math.Pow(g, 4);
            }

            //Normalize - we want relative speed factor, not race record pace
            q = (float)(0.1707 / p0);

            //Formula adjust for total (activity) time - only applicable to predict time, not for adjustment
            //(the formula should be similar to Performance Predictor formulas, like WAVA/DaveCameron/PeteRiegel)
            //q *= (float)(1/(1 - totTime * 0.00004446f));

            return q;
        }

        /***************************************************************************************************/
        private static float getJackDaniels(float g, float time, float prevTime, float dist, float prevDist)
        {
            float q;
            //Jack Daniels (jtupper)
            //http://www.letsrun.com/forum/flat_read.php?thread=197366&page=0#ixzz23qCNy3uo
            /* I also have formulas that make both up and downhill conversions in some programmable calculator somewhere around here. Seems that each % up hill slows you about 15 sec per mile 
             * and each % down gives you about 8 seconds per mile benefit, provided that you maintain the same energy expenditure. Another calculation I had showed about 12 sec per mile lost per % up. 
             * I've been tryng to get some runners to do another study on this. It isn't hard to do -- just run some repeated 5-min runs at different grades and calculate how big an increase you get 
             * in VO2 with each % grade. When we did this before I remember that different people respond differently -- some handle hills better than others (who may handle speed increases better 
             * than the hill people). A problem is that the up grade increases the cost so much that it is hard to run very fast, because the VO2 will go above max real quickly. So yu end up 
             * extrapolating from slower speeds and hope it applies at faster ones. I have done faster ones using Rate of Perceived Exertion and that can be done beyond max, but not ver exact
             * */
            bool speedAdjust = false; // (Settings.RunningGradeAdjustMethod == RunningGradeAdjustMethodEnum.JackDanielsSpeed);
            double p_jd = 0;
            double speed = (dist - prevDist) / (time - prevTime);

            //Steep adjust, see discussion for MervynDavies
            //Assuming 4m/s
            if (speedAdjust && g > 0.153f)
            {
                q = getKay(g, time, prevTime, dist, prevDist, KayForce.MaxUp) - 0.1416f;
            }
            else if (!speedAdjust && g < -0.08f)
            {
                //normal Kay, formulas cross here (about fast downhill)
                q = getKay(g, time, prevTime, dist, prevDist);
            }
            else
            {
                if (g > 0)
                {
                    p_jd = 15 * speed * g * 100 / 1609;
                }
                else
                {
                    p_jd = 8 * speed * g * 100 / 1609;
                }

                if (speedAdjust)
                {
                    q = (float)(1 - p_jd);
                }
                else
                {
                    q = (float)(1 / (1 + p_jd));
                }
            }

            return q;
        }

        /***************************************************************************************************/
        private static float getAlbertoMinetti(float g, float time, float prevTime, float dist, float prevDist)
        {
            //Energy cost of walking and running at extreme uphill and downhill slopes
            //Alberto E. Minetti, Christian Moia1, Giulio S. Roi, Davide Susta1 and Guido Ferretti
            //http://jap.physiology.org/content/93/3/1039.full
            //http://web.stanford.edu/~clint/Run_Walk2004a.rtf
            float vdotp = (float)(1 + (g * (19.5 + g * (46.3 + g * (-43.3 + g * (-30.4 + g * 155.4))))) / 3.6);
            float q = energyTimeAdjust(1 / vdotp);

            return q;
        }

        /***************************************************************************************************/
        private static float getACSM(float g, float time, float prevTime, float dist, float prevDist)
        {
            //http://www.edulife.com.br/dados%5CArtigos%5CEducacao%20Fisica%5CFisiologia%20do%20Exercicio%5CEnergy%20expenditure%20of%20walking%20and%20running%20comparison%20with%20prrediction%20equations.pdf
            //http://blue.utb.edu/mbailey/handouts/MetCalEq.htm
            //Running. V˙ O2 (mL·/kg /min) = 0.2v +  0.9vg*100+  3.5
            //vflat=v*(1+4.5*g)
            double sp = (dist - prevDist) / (time - prevTime);
            float vdotp = (float)((0.2 * 60 * sp + 3.5) / (0.2 * 60 * sp + 0.9 * g * 60 * sp + 3.5));
            float q = energyTimeAdjust(1 / vdotp);

            return q;
        }

        /***************************************************************************************************/
        private static float getPandolf(float g, float time, float prevTime, float dist, float prevDist)
        {
            //Pandolf, adjusted for running by Epstein (has no impact without load)
            //http://ftp.rta.nato.int/public//PubFullText/RTO/TR/RTO-TR-HFM-080///TR-HFM-080-03.pdf
            //http://www.springerlink.com/content/x372781w776h3367/
            //Mw=1.5 W + 2.0 (W + L)(L/W)^2 + T(W + L)[1.5V^2 + 0.35VG] = W(1.5 +[1.5V^2 + 0.35VG]) = 1.5W(1+V^2+V*G*7/30)
            //Mr = Mw - 0.5 • (1-0.01 • L) • (Mw -15 • L - 850) = Mw-0.5*(Mw-850)
            //Symbols: Mw= metabolic cost of walking (watts); 
            //Mr= metabolic cost of running (watts);
            //W = body mass (kg); 
            //L = load mass (kg); (0 assumed)
            //T = terrain factor; (1.0 used) 
            //V = velocity or walk rate (m/s);
            //G = slope or grade (%)
            //Terrain factors : 1.0 = black topping road; 1.1 = dirt road; 1.2 = light brush; 1.5 = heavy brush; 1.8 = swampy bog; 2.1 = loose sand; 2.5 = soft snow 15 cm; 3.3 = soft snow 25 cm; 4.1 = soft snow 35 cm

            //A somehow simplified formula....
            //float q_p = 1 / (float)Math.Sqrt(1 + 0.7 / 3 * (elap - prevElap) * g / (dist - prevDist));
            float v = (dist - prevDist) / (time - prevTime);
            float vdotp = 1 + 60f * 7f / 30f * g / v;
            if (vdotp > 0)
            {
                vdotp = (float)Math.Sqrt(vdotp);
            }
            float q = energyTimeAdjust(1 / vdotp);

            return q;
        }

        /***************************************************************************************************/
        //Adjust the energy/vdot/v2max to time adjustment
        //Using Jack Daniels tables to convert (formula unknown)
        //This is the same as in PerformancePredictor PredictTime.getTimeFactorFromAdjVdot() (except that 1/vdot here)
        private static float energyTimeAdjust(float q)
        {
            return (float)Math.Pow(q, 0.83);
        }
    }
}
