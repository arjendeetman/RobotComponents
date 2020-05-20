﻿// This file is part of RobotComponents. RobotComponents is licensed 
// under the terms of GNU General Public License as published by the 
// Free Software Foundation. For more information and the LICENSE file, 
// see <https://github.com/EDEK-UniKassel/RobotComponents>.

// System Libs
using System;
// RobotComponents Libs
using RobotComponents.BaseClasses.Definitions;

namespace RobotComponents.BaseClasses.Actions
{
    /// <summary>
    /// Timer class, defines waiting time between two actions. This command is used to wait a given amount of time.
    /// </summary>
    public class Timer : Action
    {
        #region fields
        private double _duration; // the time expressed in seconds
        #endregion

        #region constructors
        /// <summary>
        /// Defines an empty Timer object. 
        /// </summary>
        public Timer()
        {
        }

        /// <summary>
        /// Constructor to create a wait time object. 
        /// </summary>
        /// <param name="Duration"> The time, expressed in seconds, that program execution is to wait. </param>
        public Timer(double Duration)
        {
            this._duration = Duration;
        }

        /// <summary>
        /// Creates a new timer by duplicating an existing timer. 
        /// This creates a deep copy of the existing timer. 
        /// </summary>
        /// <param name="timer"> The timer that should be duplicated. </param>
        public Timer(Timer timer)
        {
            _duration = timer.Duration;
        }

        /// <summary>
        /// Method to duplicate the Timer object.
        /// </summary>
        /// <returns> Returns a deep copy of the Timer object.</returns>
        public Timer Duplicate()
        {
            return new Timer(this);
        }

        /// <summary>
        /// A method to duplicate the Timer object to an Action object. 
        /// </summary>
        /// <returns> Returns a deep copy of the Timer object as an Action object. </returns>
        public override Action DuplicateAction()
        {
            return new Timer(this) as Action;
        }
        #endregion

        #region method
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns> A string that represents the current object. </returns>
        public override string ToString()
        {
            if (!this.IsValid)
            {
                return "Invalid Timer";
            }
            else
            {
                return "Timer (" + this.Duration.ToString("0.##") + " sec.)";
            }
        }

        /// <summary>
        /// Used to create variable definition code of this action. 
        /// </summary>
        /// <param name="robotInfo"> Defines the Robot Info were the code is generated for. </param>
        /// <returns> Returns the RAPID code line as a string. </returns>
        public override string InitRAPIDVar(RobotInfo robotInfo)
        {
            return string.Empty;
        }

        /// <summary>
        /// Used to create action instruction code line. 
        /// </summary>
        /// <param name="robotInfo"> Defines the Robot Info were the code is generated for. </param>
        /// <returns> Returns the RAPID code line as a string. </returns>
        public override string ToRAPIDFunction(RobotInfo robotInfo)
        {
            return "WaitTime " + _duration + ";";
        }

        /// <summary>
        /// Used to create variable definitions in the RAPID Code. It is typically called inside the CreateRAPIDCode() method of the RAPIDGenerator class.
        /// </summary>
        /// <param name="RAPIDGenerator"> Defines the RAPIDGenerator. </param>
        public override void InitRAPIDVar(RAPIDGenerator RAPIDGenerator)
        {
        }

        /// <summary>
        /// Used to create action instructions in the RAPID Code. It is typically called inside the CreateRAPIDCode() method of the RAPIDGenerator class.
        /// </summary>
        /// <param name="RAPIDGenerator"> Defines the RAPIDGenerator. </param>
        public override void ToRAPIDFunction(RAPIDGenerator RAPIDGenerator)
        {
            RAPIDGenerator.StringBuilder.Append(Environment.NewLine + "\t\t" + this.ToRAPIDFunction(RAPIDGenerator.RobotInfo)); 
        }
        #endregion

        #region properties
        /// <summary>
        /// A boolean that indicates if the Timer object is valid.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                if (Duration < 0) { return false; }
                else { return true; }
            }
        }

        /// <summary>
        /// The time, expressed in seconds, that program execution is to wait. Min. value 0 seconds. Max. value no limit. Resolution 0.001 seconds.
        /// </summary>
        public double Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }
        #endregion
    }

}
