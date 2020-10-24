﻿// This file is part of RobotComponents. RobotComponents is licensed 
// under the terms of GNU General Public License as published by the 
// Free Software Foundation. For more information and the LICENSE file, 
// see <https://github.com/RobotComponents/RobotComponents>.

// System Libs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
// Rhino Libs
using Rhino.Geometry;
// RobotComponents Libs
using RobotComponents.Actions;
using RobotComponents.Kinematics;
using RobotComponents.Utils;

namespace RobotComponents.Definitions
{
    /// <summary>
    /// Represents a 6-axis spherical Robot.
    /// </summary>
    [Serializable()]
    public class Robot : ISerializable
    {
        #region fields
        private string _name; // The name of the robot
        private readonly List<Mesh> _meshes; // The robot mesh
        private List<Plane> _internalAxisPlanes; // The internal axis planes
        private List<Interval> _internalAxisLimits; // The internal axis limits
        private Plane _basePlane; // The base plane (position) of the robot
        private Plane _mountingFrame; // The tool mounting frame
        private RobotTool _tool; // The attached robot tool
        private Plane _toolPlane; // The TCP plane
        private List<ExternalAxis> _externalAxes; // The attached external axes
        private readonly InverseKinematics _inverseKinematics; // Robot inverse kinematics
        private readonly ForwardKinematics _forwardKinematics; // Robot forward kinematics
        private readonly List<Plane> _externalAxesPlanes; // The external axis planes
        private readonly List<Interval> _externalAxesLimits; // The external axis limit
        #endregion

        #region (de)serialization
        /// <summary>
        /// Protected constructor needed for deserialization of the object.  
        /// </summary>
        /// <param name="info"> The SerializationInfo to extract the data from. </param>
        /// <param name="context"> The context of this deserialization. </param>
        protected Robot(SerializationInfo info, StreamingContext context)
        {
            _name = (string)info.GetValue("Name", typeof(string));
            _meshes = (List<Mesh>)info.GetValue("Meshes", typeof(List<Mesh>));
            _internalAxisPlanes = (List<Plane>)info.GetValue("Internal Axis Planes", typeof(List<Plane>));
            _internalAxisLimits = (List<Interval>)info.GetValue("Internal Axis Limits", typeof(List<Interval>));
            _basePlane = (Plane)info.GetValue("Base Plane", typeof(Plane));
            _mountingFrame = (Plane)info.GetValue("Mounting Frame", typeof(Plane));
            _tool = (RobotTool)info.GetValue("RobotTool", typeof(RobotTool));
            _toolPlane = (Plane)info.GetValue("Tool Plane", typeof(Plane));
            _externalAxes = (List<ExternalAxis>)info.GetValue("External Axis", typeof(List<ExternalAxis>));
            _externalAxesPlanes = (List<Plane>)info.GetValue("External Axis Planes", typeof(List<Plane>));
            _externalAxesLimits = (List<Interval>)info.GetValue("External Axis Limits", typeof(List<Interval>));

            _inverseKinematics = new InverseKinematics(new RobotTarget("init", Plane.WorldXY), this);
            _forwardKinematics = new ForwardKinematics(this);
        }

        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the object.
        /// </summary>
        /// <param name="info"> The SerializationInfo to populate with data. </param>
        /// <param name="context"> The destination for this serialization. </param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // int version = (int)info.GetValue("Version", typeof(int)); // <-- use this if the (de)serialization changes
            info.AddValue("Version", VersionNumbering.CurrentVersionAsInt, typeof(int));
            info.AddValue("Name", _name, typeof(string));
            info.AddValue("Meshes", _meshes, typeof(List<Mesh>));
            info.AddValue("Internal Axis Planes", _internalAxisPlanes, typeof(List<Plane>));
            info.AddValue("Internal Axis Limits", _internalAxisLimits, typeof(List<Interval>));
            info.AddValue("Base Plane", _basePlane, typeof(Plane));
            info.AddValue("Mounting Frame", _mountingFrame, typeof(Plane));
            info.AddValue("RobotTool", _tool, typeof(RobotTool));
            info.AddValue("Tool Plane", _toolPlane, typeof(Plane));
            info.AddValue("External Axis", _externalAxes, typeof(List<ExternalAxis>));
            info.AddValue("External Axis Planes", _externalAxesPlanes, typeof(List<Plane>));
            info.AddValue("External Axis Limits", _externalAxesLimits, typeof(List<Interval>));
        }
        #endregion

        #region constructors
        /// <summary>
        /// Initializes an empty instance of the Robot class.
        /// </summary>
        public Robot()
        {
            _name = "Empty Robot";
        }

        /// <summary>
        /// Initializes a new instance of the Robot class without attached external axes.
        /// </summary>
        /// <param name="name"> The name. </param>
        /// <param name="meshes"> The base and links meshes defined in the world coorindate space. </param>
        /// <param name="internalAxisPlanes"> The internal axes planes defined in the world coorindate space. </param>
        /// <param name="internalAxisLimits"> The internal axes limit. </param>
        /// <param name="basePlane"> The position and orientation of the robot base in the world coordinate space. </param>
        /// <param name="mountingFrame"> The tool mounting frame definied in the world coordinate space. </param>
        /// <param name="tool"> The Robot Tool. </param>
        public Robot(string name, List<Mesh> meshes, List<Plane> internalAxisPlanes, List<Interval> internalAxisLimits, Plane basePlane, Plane mountingFrame, RobotTool tool)
        {
            // Update robot related fields
            _name = name;
            _meshes = meshes;
            _internalAxisPlanes = internalAxisPlanes;
            _internalAxisLimits = internalAxisLimits;
            _basePlane = basePlane;
            _mountingFrame = mountingFrame;

            // Update tool related fields
            _tool = tool.Duplicate(); // Make a deep copy since we transform it later
            _meshes.Add(GetAttachedToolMesh(_tool));
            _toolPlane = GetAttachedToolPlane(_tool);

            // Update external axis related fields
            _externalAxes = new List<ExternalAxis>();
            _externalAxesPlanes = Enumerable.Repeat(Plane.Unset, 6).ToList();
            _externalAxesLimits = Enumerable.Repeat(new Interval(), 6).ToList();

            // Transform Robot Tool to Mounting Frame
            Transform trans = Transform.PlaneToPlane(_tool.AttachmentPlane, _mountingFrame);
            _tool.Transform(trans);

            // Set kinematics
            _inverseKinematics = new InverseKinematics(new RobotTarget("init", Plane.WorldXY), this);
            _forwardKinematics = new ForwardKinematics(this);
        }

        /// <summary>
        /// Initializes a new instance of the Robot class with attached external axes.
        /// </summary>
        /// <param name="name"> The name. </param>
        /// <param name="meshes"> The base and links meshes defined in the world coorindate space. </param>
        /// <param name="internalAxisPlanes"> The internal axes planes defined in the world coorindate space. </param>
        /// <param name="internalAxisLimits"> The internal axes limit. </param>
        /// <param name="basePlane"> The position and orientation of the robot base in the world coordinate space. </param>
        /// <param name="mountingFrame"> The tool mounting frame definied in the world coordinate space. </param>
        /// <param name="tool"> The Robot Tool. </param>
        /// <param name="externalAxes"> The attached external axes. </param>
        public Robot(string name, List<Mesh> meshes, List<Plane> internalAxisPlanes, List<Interval> internalAxisLimits, Plane basePlane, Plane mountingFrame, RobotTool tool, List<ExternalAxis> externalAxes)
        {
            // Robot related fields
            _name = name;
            _meshes = meshes;
            _internalAxisPlanes = internalAxisPlanes;
            _internalAxisLimits = internalAxisLimits;
            _basePlane = basePlane;
            _mountingFrame = mountingFrame;

            // Tool related fields
            _tool = tool.Duplicate(); // Make a deep copy since we transform it later
            _meshes.Add(GetAttachedToolMesh(_tool));
            _toolPlane = GetAttachedToolPlane(_tool);

            // External axis related fields
            _externalAxes = externalAxes;
            _externalAxesPlanes = new List<Plane>();
            _externalAxesLimits = new List<Interval>();
            UpdateExternalAxisFields();

            // Transform Robot Tool to Mounting Frame
            Transform trans = Transform.PlaneToPlane(_tool.AttachmentPlane, _mountingFrame);
            _tool.Transform(trans);

            // Set kinematics
            _inverseKinematics = new InverseKinematics(new RobotTarget("init", Plane.WorldXY), this);
            _forwardKinematics = new ForwardKinematics(this);
        }

        /// <summary>
        /// Initializes a new instance of the Robot class by duplicating an existing Robot instance. 
        /// </summary>
        /// <param name="robot"> The Robot instance to duplicate. </param>
        public Robot(Robot robot)
        {
            // Robot related fields
            _name = robot.Name;
            _meshes = robot.Meshes.ConvertAll(mesh => mesh.DuplicateMesh()); // This includes the tool mesh

            _internalAxisPlanes = new List<Plane>(robot.InternalAxisPlanes);
            _internalAxisLimits = new List<Interval>(robot.InternalAxisLimits);
            _basePlane = new Plane(robot.BasePlane);
            _mountingFrame = new Plane(robot.MountingFrame);

            // Tool related fields
            _tool = robot.Tool.Duplicate();
            _toolPlane = new Plane(robot.ToolPlane);

            // External axis related fields
            _externalAxes = new List<ExternalAxis>(robot.ExternalAxes); //TODO: make deep copy
            _externalAxesPlanes = new List<Plane>(robot.ExternalAxisPlanes);
            _externalAxesLimits = new List<Interval>(robot.ExternalAxisLimits);

            // Kinematics
            _inverseKinematics = new InverseKinematics(robot.InverseKinematics.Movement.Duplicate(), this);
            _forwardKinematics = new ForwardKinematics(this, robot.ForwardKinematics.HideMesh);
        }

        /// <summary>
        /// Returns an exact duplicate of this Robot instance.
        /// </summary>
        /// <returns> A deep copy of the Robot instance. </returns>
        public Robot Duplicate()
        {
            return new Robot(this);
        }
        #endregion

        #region methods
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns> A string that represents the current object. </returns>
        public override string ToString()
        {
            if (!this.IsValid)
            {
                return "Invalid Robot";
            }
            else
            {
                return "Robot (" + this.Name + ")";
            }
        }

        /// <summary>
        /// Re-initializes the fields related to the list with external axes.
        /// </summary>
        private void UpdateExternalAxisFields()
        {
            // Check the number of external axes
            if (_externalAxes.Count > 6)
            {
                throw new ArgumentException("More than six external axes are defined. A maximum of 6 external axes can be attached to a Robot.");
            }

            // Check list with external axes: maximum of one external linear axis is allowed at the moment
            if (_externalAxes.Count(item => item.MovesRobot is true) > 1)
            {
                throw new ArgumentException("At the moment Robot Components supports a maximum of one external linear axis.");
            }

            for (int i = 0; i < _externalAxes.Count; i++)
            {
                if (_externalAxes[i].AxisNumber == -1)
                {
                    _externalAxes[i].AxisNumber = i;
                }
                else if (!_externalAxes[i].IsValid)
                {
                    throw new ArgumentException(String.Format("External Axis {0} ({1}): The set attached External Axis is not valid.", _externalAxes[i].AxisLogic, _externalAxes[i].Name));
                }
            }

            // Clear the lists
            _externalAxesPlanes.Clear();
            _externalAxesLimits.Clear();

            // Fill the lists again
            for (int i = 0; i < 6; i++)
            {
                if (_externalAxes.Count > i && _externalAxes[i] != null)
                {
                    _externalAxesLimits.Add(_externalAxes[i].AxisLimits);
                    _externalAxesPlanes.Add(_externalAxes[i].AxisPlane);
                }
                else
                {
                    _externalAxesLimits.Add(new Interval());
                    _externalAxesPlanes.Add(Plane.Unset);
                }
            }
        }

        /// <summary>
        /// Defines the attached tool mesh in the robot coordinate space.
        /// </summary>
        /// <param name="tool"> Ther robot tool to take the mesh from. </param>
        /// <returns> The tool mesh in the robot coordinate space. </returns>
        public Mesh GetAttachedToolMesh(RobotTool tool)
        {
            Mesh toolMesh = tool.Mesh.DuplicateMesh();
            Transform trans = Transform.PlaneToPlane(tool.AttachmentPlane, _mountingFrame);
            toolMesh.Transform(trans);
            return toolMesh;
        }

        /// <summary>
        /// Defines the TCP plane of the attached robot tool in the robot coordinate space.
        /// </summary>
        /// <param name="tool"> Ther robot tool to take the mesh from. </param>
        /// <returns> The attached tool mesh in the robot coordinate space. </returns>
        public Plane GetAttachedToolPlane(RobotTool tool)
        {
            Plane toolPlane = new Plane(tool.ToolPlane);
            Transform trans = Transform.PlaneToPlane(tool.AttachmentPlane, _mountingFrame);
            toolPlane.Transform(trans);
            return toolPlane;
        }

        /// <summary>
        /// Transforms the robot spatial properties (planes and meshes.
        /// The attached external axes will not be transformed. 
        /// </summary>
        /// <param name="xform"> Spatial deform. </param>
        public void Transfom(Transform xform)
        {
            _basePlane.Transform(xform);
            _mountingFrame.Transform(xform);
            _tool.Transform(xform);

            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].Transform(xform);
            }

            for (int i = 0; i < _internalAxisPlanes.Count; i++)
            {
                Plane transformedPlane = new Plane(_internalAxisPlanes[i]);
                transformedPlane.Transform(xform);
                _internalAxisPlanes[i] = new Plane(transformedPlane);
            }

            _toolPlane = GetAttachedToolPlane(_tool);

        }
        #endregion

        #region properties
        /// <summary>
        /// Gets a value indicating whether or not the object is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (InternalAxisPlanes == null) { return false; }
                if (InternalAxisLimits == null) { return false; }
                if (BasePlane == null) { return false; }
                if (BasePlane == Plane.Unset) { return false; }
                if (MountingFrame == null) { return false; }
                if (MountingFrame == Plane.Unset) { return false; }
                if (InternalAxisPlanes.Count != 6) { return false; }
                if (Meshes.Count != 8) { return false; }
                if (Tool == null) { return false; }
                if (Tool.IsValid == false) { return false; }
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the name of the Robot.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets the Robot meshes including the mesh of the attached tool.
        /// </summary>
        public List<Mesh> Meshes
        {
            get { return _meshes; }
        }

        /// <summary>
        /// Gets or sets the internal axis planes.
        /// The Z-axes of the planes define the rotation centers. 
        /// </summary>
        public List<Plane> InternalAxisPlanes
        {
            get { return _internalAxisPlanes; }
            set { _internalAxisPlanes = value; }
        }

        /// <summary>
        /// Gets or sets the axis limits in degrees.
        /// </summary>
        public List<Interval> InternalAxisLimits
        {
            get { return _internalAxisLimits; }
            set { _internalAxisLimits = value; }
        }

        /// <summary>
        /// Gets or sets the position and orientation of the robot in world coordinate space. 
        /// </summary>
        public Plane BasePlane
        {
            get { return _basePlane; }
            set { _basePlane = value; }
        }

        /// <summary>
        /// Gets or sets the tool mounting frame in world coordinate space.
        /// </summary>
        public Plane MountingFrame
        {
            get
            {
                return _mountingFrame;
            }
            set
            {
                _mountingFrame = value;
                _toolPlane = GetAttachedToolPlane(_tool);
            }
        }

        /// <summary>
        /// Gets the TCP plane in world coordinate space.
        /// </summary>
        public Plane ToolPlane
        {
            get { return _toolPlane; }
        }

        /// <summary>
        /// Gets or sets the Robot Tool.
        /// </summary>
        public RobotTool Tool
        {
            get
            {
                return _tool;
            }
            set
            {
                _tool = value;
                _toolPlane = GetAttachedToolPlane(_tool);
            }
        }

        /// <summary>
        /// Gets or sets the attached external axes.
        /// </summary>
        public List<ExternalAxis> ExternalAxes
        {
            get
            {
                return _externalAxes;
            }
            set
            {
                _externalAxes = value;
                UpdateExternalAxisFields();
            }
        }

        /// <summary>
        /// Gets the Inverse Kinematics of this Robot. 
        /// </summary>
        public InverseKinematics InverseKinematics
        {
            get { return _inverseKinematics; }
        }

        /// <summary>
        /// Gets the Forward Kinimatics of this Robot.
        /// </summary>
        public ForwardKinematics ForwardKinematics
        {
            get { return _forwardKinematics; }
        }

        /// <summary>
        /// Gets the external axis planes.
        /// </summary>
        public List<Plane> ExternalAxisPlanes
        {
            get { return _externalAxesPlanes; }
        }

        /// <summary>
        /// Gets the external axis limits.
        /// </summary>
        public List<Interval> ExternalAxisLimits
        {
            get { return _externalAxesLimits; }
        }
        #endregion
    }

}
