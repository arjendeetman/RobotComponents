﻿using System.Collections.Generic;

using Rhino.Geometry;

namespace RobotComponents.BaseClasses
{
    public class ExternalRotationalAxis : ExternalAxis
    {
        #region fields
        private Plane _attachmentPlane;
        private Plane _axisPlane; // Todo: now only the attachment plane is copied
        private double _startDegree;
        private Interval _axisLimits;
        private Curve _axisCurve;
        private bool _isLinear;
        private List<Mesh> _meshes;
        private int? _axisNumber;
        List<Mesh> _posedMeshes;
        #endregion

        #region constructors
        public ExternalRotationalAxis()
        {
            _posedMeshes = new List<Mesh>();
        }

        public ExternalRotationalAxis(Plane plane, double startDegree, Interval axisLimits)
        {
            _attachmentPlane = plane;
            _startDegree = startDegree;
            _axisLimits = axisLimits;
            _axisNumber = null; // Todo
            _isLinear = false;
            _posedMeshes = new List<Mesh>();
            Initilize();
        }

        public ExternalRotationalAxis Duplicate()
        {
            ExternalRotationalAxis dup = new ExternalRotationalAxis(AttachmentPlane, StartDegree, AxisLimits);
            return dup;
        }
        #endregion

        #region methods
        public bool IsValid
        {
            get
            {
                if (AttachmentPlane == null) { return false; }
                if (AxisLimits == null) { return false; }
                return true;
            }
        }

        public void GetAxisCurve()
        {
            Line line = new Line(_attachmentPlane.Origin, _attachmentPlane.Origin + _attachmentPlane.ZAxis*10);
            _axisCurve = line.ToNurbsCurve();
            _axisCurve.Domain = new Interval(0, 1);

        }
        public override Plane CalculatePosition(double axisValue, out bool inLimits)
        {
            bool isInLimits;

            // Check if value is within axis limits
            if (axisValue < _axisLimits.Min)
            {
                isInLimits = false;
            }
            else if (axisValue > _axisLimits.Max)
            {
                isInLimits = false;
            }
            else
            {
                isInLimits = true;
            }

            // Transform
            double radians = Rhino.RhinoMath.ToRadians(axisValue);
            Transform orientNow = Transform.Rotation(radians, _axisPlane.ZAxis, _axisPlane.Origin);
            Plane position = _attachmentPlane; // deep copy?
            position.Transform(orientNow);

            inLimits = isInLimits;
            return position;
        }

        public override Plane CalculatePositionSave(double axisValue)
        {
            double value;

            // Check if value is within axis limits
            if (axisValue < _axisLimits.Min)
            {
                value = _axisLimits.Min;
            }
            else if (axisValue > _axisLimits.Max)
            {
                value = _axisLimits.Max;
            }
            else
            {
                value = axisValue;
            }

            // Transform
            double radians = Rhino.RhinoMath.ToRadians(value);
            Transform orientNow = Transform.Rotation(radians, _axisPlane.ZAxis, _axisPlane.Origin);
            Plane position = _attachmentPlane; // deep copy?
            position.Transform(orientNow);

            return position;
        }

        override public void PoseMeshes(double axisValue)
        {
            _posedMeshes.Clear();
            double radians = Rhino.RhinoMath.ToRadians(axisValue);
            Transform orientNow = Transform.Rotation(radians, _axisPlane.ZAxis, _axisPlane.Origin);
            //_posedMeshes.Add(_baseMesh.DuplicateMesh());
            //_posedMeshes.Add(_linkMesh.DuplicateMesh());
            //_posedMeshes[1].Transform(translateNow);
        }



        public void GetAxisPlane()
        {
            // To do
            _axisPlane = _attachmentPlane;
        }
        
        public override void Initilize()
        {
            GetAxisCurve();
            GetAxisPlane();
        }
        public override void ReInitilize()
        {
            Initilize();
        }
        #endregion

        #region properties
        public override Interval AxisLimits { get => _axisLimits; set => _axisLimits = value; }
        public override AxisType AxisType { get => AxisType.ROTATIONAL; }
        public override Plane AttachmentPlane { get => _attachmentPlane; set => _attachmentPlane = value; }
        public override Plane AxisPlane { get => _axisPlane; set => _axisPlane = value; }
        public override int? AxisNumber { get => _axisNumber; set => _axisNumber = value; }
        public double StartDegree { get => _startDegree; set => _startDegree = value; }
        public Curve AxisCurve { get => _axisCurve; set => _axisCurve = value; }
        public bool IsLinear { get => _isLinear; set => _isLinear = value; }
        public List<Mesh> Meshes { get => _meshes; set => _meshes = value; }
        override public List<Mesh> PosedMeshes { get => _posedMeshes; set => _posedMeshes = value; }
        #endregion
    }
}