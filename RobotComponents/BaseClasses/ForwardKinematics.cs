﻿using System;
using System.Collections.Generic;

using Rhino.Geometry;

namespace RobotComponents.BaseClasses
{
    /// <summary>
    /// Forward Kinematics class
    /// </summary>
    public class ForwardKinematics
    {
        #region fields
        RobotInfo _robotInfo; // Robot Info

        Plane _basePlane; // Robot Base Plane
        Plane[] _internalAxisPlanes; // Internal Axis Planes 
        List<double> _internalAxisValues = new List<double>(); // Internal Axis Values in Degrees
        double[] _internalAxisRads; // Internal Axis Values in Radiants
        List<Interval> _internalAxisLimits; // Internal Axis Value Limits
        List<bool> _internalAxisInLimit = new List<bool>(); // Internal Axis in Limit?: Bool List

        Plane[] _externalAxisPlanes; // External Axis Planes 
        List<double> _externalAxisValues = new List<double>(); // External Axis Values in Degrees
        double[] _externalAxisRads; // External Axis Values in Radiants
        List<Interval> _externalAxisLimits; // External Axis Value Limits
        List<bool> _externalAxisInLimit = new List<bool>(); // External Axis in Limit?: Bool List

        List<string> _errorText = new List<string>(); // Error text

        List<Mesh> _meshes; // Robot Meshes
        List<Mesh> _posedMeshes; // Posed Robot Meshes
        List<Mesh> _posedAxisMeshes; //Posed Axis Meshes
        Plane _endPlane; // End Plane placed on TargetPlane
        Plane _tcpPlane; // TCP Plane of effector
        #endregion

        #region constructors
        /// <summary>
        /// Defines a empty ForwardKinematic Object.
        /// </summary>
        public ForwardKinematics()
        {
        }

        /// <summary>
        /// Defines a Forward Kinematic object.
        /// </summary>
        /// <param name="robotInfo"> Robot Information the FK should be calculated for. </param>
        public ForwardKinematics(RobotInfo robotInfo)
        {
        _robotInfo = robotInfo;
        }

        /// <summary>
        /// Defines a Forward Kinematic Object for certain axis values.
        /// </summary>
        /// <param name="robotInfo">Robot Information the FK should be calculated for.</param>
        /// <param name="internalAxisValues">List of internal axis values. The length of the list should be equal to 6.</param>
        /// <param name="externalAxisValues">List of external axis values. The length of the list should be (for now) equal to 1.</param>
        public ForwardKinematics(RobotInfo robotInfo, List<double> internalAxisValues, List<double> externalAxisValues)
        {
            _robotInfo = robotInfo;
            Update(internalAxisValues, externalAxisValues);
        }

        /// <summary>
        /// A method to duplicate the Forward Kinematics object.
        /// </summary>
        /// <returns> Returns a deep copy of the Forward Kinematics object. </returns>
        public ForwardKinematics Duplicate()
        {
            ForwardKinematics dup = new ForwardKinematics(RobotInfo, InternalAxisValues, ExternalAxisValues); //TODO: Make an constructor that really makes an copy of the data.. 
            return dup;
        }

        #endregion

        #region methods
        /// <summary>
        /// Calculates Forward Kinematics based on the internal and external axis values.
        /// </summary>
        /// <param name="hideMesh"> Parameter set if the Mesh pose gets calculated. </param>
        public void Calculate(bool hideMesh = false)
        {
            _posedMeshes = _meshes.ConvertAll(mesh => mesh.DuplicateMesh());
            PosedAxisMeshes = new List<Mesh>();
            _tcpPlane = _robotInfo.ToolPlane;

            // Calculates external axes
            for (int i = 0; i < _robotInfo.ExternalAxis.Count; i++)
            {
                if (_robotInfo.ExternalAxis[i] is ExternalLinearAxis)
                {
                    ExternalLinearAxis externalLinearAxis = _robotInfo.ExternalAxis[i] as ExternalLinearAxis;
                    _basePlane.Origin += externalLinearAxis.AxisPlane.ZAxis * _externalAxisValues[0]; //External Axis Offset: Use "CalculatePositionSave()" ?
                    
                    if (!hideMesh)
                    {
                        externalLinearAxis.PoseMeshes(_externalAxisValues[0]); // Should the 0 here not be i? and should _robotInfo.ExternalAxis.Count == _externalAxisValues.Count

                        for (int j = 0; j < externalLinearAxis.PosedMeshes.Count; j++)
                        {
                            PosedAxisMeshes.Add(externalLinearAxis.PosedMeshes[j]);
                        }
                    }
                }
            }
            
            // Calculates interal axes
            // First caculate all tansformations (rotations)
            // Axis 1
            Transform rot1;
            rot1 = Transform.Rotation(_internalAxisRads[0], _internalAxisPlanes[0].ZAxis, _internalAxisPlanes[0].Origin);
            // Axis 2
            Transform rot2;
            Plane planeAxis2 = new Plane(_internalAxisPlanes[1]);
            planeAxis2.Transform(rot1);
            rot2 = Transform.Rotation(_internalAxisRads[1], planeAxis2.ZAxis, planeAxis2.Origin);
            // Axis 3
            Transform rot3;
            Plane planeAxis3 = new Plane(_internalAxisPlanes[2]);
            planeAxis3.Transform(rot2 * rot1);
            rot3 = Transform.Rotation(_internalAxisRads[2], planeAxis3.ZAxis, planeAxis3.Origin);
            // Axis 4
            Transform rot4;
            Plane planeAxis4 = new Plane(_internalAxisPlanes[3]);
            planeAxis4.Transform(rot3 * rot2 * rot1);
            rot4 = Transform.Rotation(_internalAxisRads[3], planeAxis4.ZAxis, planeAxis4.Origin);
            // Axis 5
            Transform rot5;
            Plane planeAxis5 = new Plane(_internalAxisPlanes[4]);
            planeAxis5.Transform(rot4 * rot3 * rot2 * rot1);
            rot5 = Transform.Rotation(_internalAxisRads[4], planeAxis5.ZAxis, planeAxis5.Origin);
            // Axis 6
            Transform rot6;
            Plane planeAxis6 = new Plane(_internalAxisPlanes[5]);
            planeAxis6.Transform(rot5 * rot4 * rot3 * rot2 * rot1);
            rot6 = Transform.Rotation(_internalAxisRads[5], planeAxis6.ZAxis, planeAxis6.Origin);

            // Move relative to base
            Transform transNow;
            transNow = Transform.ChangeBasis(_basePlane, Plane.WorldXY);

            // Apply transformations
            // TCP plane transform
            _tcpPlane.Transform(transNow * rot6 * rot5 * rot4 * rot3 * rot2 * rot1);
            if (!hideMesh)
            {
                // Base link transform
                _posedMeshes[0].Transform(transNow);
                // Link_1 tranform 
                _posedMeshes[1].Transform(transNow * rot1);
                // Link_2 tranform
                _posedMeshes[2].Transform(transNow * rot2 * rot1);
                // Link_3 tranform
                _posedMeshes[3].Transform(transNow * rot3 * rot2 * rot1);
                // Link_4 tranform
                _posedMeshes[4].Transform(transNow * rot4 * rot3 * rot2 * rot1);
                // Link_5 tranform
                _posedMeshes[5].Transform(transNow * rot5 * rot4 * rot3 * rot2 * rot1);
                // Link_6 tranform
                _posedMeshes[6].Transform(transNow * rot6 * rot5 * rot4 * rot3 * rot2 * rot1);
                // End-effector transform
                _posedMeshes[7].Transform(transNow * rot6 * rot5 * rot4 * rot3 * rot2 * rot1);
            }
        }

        /// <summary>
        /// Updates the Internal and external axis values for the Forward Kinematic.
        /// </summary>
        /// <param name="internalAxisValues">List of internal axis values in degree. The length of the list should be equal to 6.</param>
        /// <param name="externalAxisValues">List of external axis values in meter. The length of the list should be (for now) equal to 1.</param>
        public void Update(List<double> internalAxisValues, List<double> externalAxisValues)
        {
            _internalAxisValues.Clear();
            _internalAxisInLimit.Clear();
            _externalAxisValues.Clear();
            _externalAxisInLimit.Clear();
            _errorText.Clear();

            _basePlane = _robotInfo.BasePlane;

            _internalAxisPlanes = _robotInfo.InternalAxisPlanes.ToArray();
            _internalAxisLimits = _robotInfo.InternalAxisLimits;
            _internalAxisValues = internalAxisValues;

            _externalAxisPlanes = new Plane[_robotInfo.ExternalAxis.Count];

            _externalAxisLimits = _robotInfo.ExternalAxisLimits;
            _externalAxisValues = externalAxisValues;

            // "Deep copy" mesh to new object
            _meshes = _robotInfo.Meshes.ConvertAll(mesh => mesh.DuplicateMesh());
            _posedMeshes = _robotInfo.Meshes.ConvertAll(mesh => mesh.DuplicateMesh());

            _tcpPlane = _robotInfo.ToolPlane;

            // Internal axis values in degrees converted to axis values in radians
            UpdateInternalAxisValuesRadians();

            // Check axis limits
            CheckForInternalAxisLimits();
            CheckForExternalAxisLimits();

            // Get the current location of the attachment plane of the external axes
            for (int i = 0; i < _robotInfo.ExternalAxis.Count; i++)
            {
                _externalAxisPlanes[i] = _robotInfo.ExternalAxis[i].CalculatePositionSave(_externalAxisValues[i]);
            }
        }

        /// <summary>
        /// Updates the list with internal axis values in degrees based from the array with internal axis values in radians. 
        /// </summary>
        private void UpdateInternalAxisValuesDegrees()
        {
            _internalAxisValues = new List<double>() { 0, 0, 0, 0, 0, 0};
            _internalAxisValues[0] = (_internalAxisRads[0] * 180) / Math.PI;
            _internalAxisValues[1] = (_internalAxisRads[1] * 180) / Math.PI;
            _internalAxisValues[2] = (_internalAxisRads[2] * 180) / Math.PI;
            _internalAxisValues[3] = (_internalAxisRads[3] * 180) / Math.PI;
            _internalAxisValues[4] = (_internalAxisRads[4] * 180) / Math.PI;
            _internalAxisValues[5] = (_internalAxisRads[5] * 180) / Math.PI;
        }

        /// <summary>
        /// Updates the array with internal axis values in radians based from the list withi internal axis values in degrees. 
        /// </summary>
        private void UpdateInternalAxisValuesRadians()
        {
            _internalAxisRads = new double[_internalAxisValues.Count];
            _internalAxisRads[0] = (_internalAxisValues[0] / 180) * Math.PI;
            _internalAxisRads[1] = (_internalAxisValues[1] / 180) * Math.PI;
            _internalAxisRads[2] = (_internalAxisValues[2] / 180) * Math.PI;
            _internalAxisRads[3] = (_internalAxisValues[3] / 180) * Math.PI;
            _internalAxisRads[4] = (_internalAxisValues[4] / 180) * Math.PI;
            _internalAxisRads[5] = (_internalAxisValues[5] / 180) * Math.PI;
        }

        /// <summary>
        /// Checks if the interal axis values are outside its limits.
        /// </summary>
        public void CheckForInternalAxisLimits()
        {
            _errorText.Clear();
            _internalAxisInLimit.Clear();

            for (int i = 0; i < _internalAxisValues.Count; i++)
            {
                if (_internalAxisLimits[i].IncludesParameter(_internalAxisValues[i]))
                {
                    _internalAxisInLimit.Add(true);
                }
                else
                {
                    _errorText.Add("Internal Axis Value " + i + " is not in Range.");
                    _internalAxisInLimit.Add(false);
                }

            }
        }

        /// <summary>
        /// Checks if the external axis values are outside its limits.
        /// </summary>
        public void CheckForExternalAxisLimits()
        {
            _errorText.Clear();
            _externalAxisInLimit.Clear();

            for (int i = 0; i < _externalAxisValues.Count; i++)
            {
                if (_externalAxisLimits[i].IncludesParameter(_externalAxisValues[i]))
                {
                    _externalAxisInLimit.Add(true);
                }
                else
                {
                    _errorText.Add("External Axis Value " + i + " is not in Range.");
                    _externalAxisInLimit.Add(false);
                }

            }
        }
        #endregion

        #region properties
        /// <summary>
        /// A boolean that indicates if the Forward Kinematics object is valid. 
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (InternalAxisPlanes == null) { return false; }
                if (InternalAxisLimits == null) { return false; }
                if (InternalAxisValues == null) { return false; }
                return true;
            }
        }

        /// <summary>
        /// RobotInformation the FK should be calculated for.
        /// </summary>
        public RobotInfo RobotInfo
        {
            get { return _robotInfo; }
            set { _robotInfo = value; }
        }

        /// <summary>
        /// A List of meshes of the robot.
        /// </summary>
        public List<Mesh> Meshes
        {
            get { return _meshes; }
            set { _meshes = value; }
        }

        /// <summary>
        /// A List of meshes in pose for internal axises.
        /// </summary>
        public List<Mesh> PosedMeshes
        {
            get { return _posedMeshes; }
            set { _posedMeshes = value; }
        }

        /// <summary>
        /// A Array of internal Axis planes.
        /// </summary>
        public Plane[] InternalAxisPlanes
        {
            get { return _internalAxisPlanes; }
            set { _internalAxisPlanes = value; }
        }

        /// <summary>
        /// End Plane placed on TargetPlane
        /// </summary>
        public Plane EndPlane
        {
            get { return _endPlane; }
            set { _endPlane = value; }
        }

        /// <summary>
        /// TCP Plane of endeffector
        /// </summary>
        public Plane TCPPlane
        {
            get { return _tcpPlane; }
            set { _tcpPlane = value; }
        }

        /// <summary>
        /// List of intervals defining the axis limits.
        /// </summary>
        public List<Interval> InternalAxisLimits
        {
            get { return _internalAxisLimits; }
            set { _internalAxisLimits = value; }
        }

        /// <summary>
        /// List of boolean defining whether or not the robot is outside of there axis limits.
        /// </summary>
        public List<bool> InternalAxisInLimit
        {
            get { return _internalAxisInLimit; }
            set { _internalAxisInLimit = value; }
        }

        /// <summary>
        /// List of strings collecting error messages which can be displayed in a grasshopper component.
        /// </summary>
        public List<string> ErrorText
        {
            get { return _errorText; }
            set { _errorText = value; }
        }

        /// <summary>
        /// List of internal axis values in degree.
        /// </summary>
        public List<double> InternalAxisValues
        {
            get 
            { 
                return _internalAxisValues; 
            }
            set 
            { 
                _internalAxisValues = value;
                UpdateInternalAxisValuesRadians();
            }
        }

        /// <summary>
        /// Array of internal axis values in radians.
        /// </summary>
        public Double[] InternalAxisRads
        {
            get 
            { 
                return _internalAxisRads; 
            }
            set 
            { 
                _internalAxisRads = value;
                UpdateInternalAxisValuesDegrees();
            }
        }

        /// <summary>
        /// Array of external axis palnes.
        /// </summary>
        public Plane[] ExternalAxisPlanes 
        {
            get { return _externalAxisPlanes; }
            set { _externalAxisPlanes = value; }
        }

        /// <summary>
        /// List of external axis values in ?. A external axis can be meter or degree
        /// </summary>
        public List<double> ExternalAxisValues 
        {
            get { return _externalAxisValues; }
            set { _externalAxisValues = value; }
        }

        /// <summary>
        /// List of external axis values in radians. A external axis can be rotational od linear.
        /// </summary>
        public double[] ExternalAxisRads 
        {
            get { return _externalAxisRads; }
            set { _externalAxisRads = value; }
        }

        public List<Interval> ExternalAxisLimits 
        {
            get { return _externalAxisLimits; }
            set { _externalAxisLimits = value; }
        }

        /// <summary>
        ///  List of intervals defining the axis limits.
        /// </summary>
        public List<bool> ExternalAxisInLimit 
        {
            get { return _externalAxisInLimit; }
            set { _externalAxisInLimit = value; }
        }

        /// <summary>
        /// Defines the BasePlane
        /// </summary>
        public Plane BasePlane 
        {
            get { return _basePlane; }
            set { _basePlane = value; }
        }

        /// <summary>
        /// A List of meshes in pose for the external axis.
        /// </summary>
        public List<Mesh> PosedAxisMeshes 
        {
            get { return _posedAxisMeshes; }
            set { _posedAxisMeshes = value; }
        }
        #endregion
    }

}
