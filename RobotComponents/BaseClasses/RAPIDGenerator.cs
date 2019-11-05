﻿using System;
using System.IO;
using System.Collections.Generic;

using Rhino.Geometry;
using RobotComponents.Components;

namespace RobotComponents.BaseClasses
{
    /// <summary>
    /// RAPID Generator class, creates RAPID Code from Actions.
    /// </summary>
    /// 

    public class RAPIDGenerator
    {
        #region fields
        private RobotInfo _robotInfo;
        private List<Action> _actions = new List<Action>();
        private string _filePath;
        private bool _saveToFile;
        private string _RAPIDCode;
        private string _BASECode;
        private string _ModuleName;
        private Guid _documentGUID;
        private ObjectManager _objectManager;
        private bool _firstMovementIsMoveAbs;
        #endregion

        #region constructors
        public RAPIDGenerator()
        {
        }

        public RAPIDGenerator(string moduleName,  List<Action> actions, string filePath, bool saveToFile, RobotInfo robotInfo, Guid documentGUID)
        {
            this._ModuleName = moduleName;
            this._robotInfo = robotInfo;
            this._actions = actions;

            this._filePath = filePath;
            this._saveToFile = saveToFile;

            this._documentGUID = documentGUID;

            // Checks if ObjectManager for this document already exists. If not it creates a new one
            if (!DocumentManager.ObjectManagers.ContainsKey(_documentGUID))
            {
                DocumentManager.ObjectManagers.Add(_documentGUID, new ObjectManager());
            }

            // Gets ObjectManager of this document
            _objectManager = DocumentManager.ObjectManagers[_documentGUID];

            if (_saveToFile == true)
            {
                WriteRAPIDCodeToFile();
                WriteBASECodeToFile();
            }
          
        }

        public RAPIDGenerator Duplicate()
        {
            RAPIDGenerator dup = new RAPIDGenerator(ModuleName, Actions, FilePath, SaveToFile, RobotInfo, DocumentGUID);
            return dup;
        }
        #endregion

        #region method
        public string CreateRAPIDCode()
        {
            //Set's default RobotTool
             _objectManager.CurrentTool = _robotInfo.Tool.Name;

            // Creates Main Module
            string RAPIDCode = "MODULE " + _ModuleName+"@";

            // Creates Vars
            for (int i = 0; i != _actions.Count; i++)
            {

                string tempCode = _actions[i].InitRAPIDVar(_robotInfo, RAPIDCode);

                // Checks if Var is already in Code
                RAPIDCode += tempCode;
            }

            // Create Program
            RAPIDCode += "@@" + "\t" + "PROC main()";

            bool foundFirstMovement = false;
            // Creates Movement Instruction and other Functions
            for (int i = 0; i != _actions.Count; i++)
            {
                string rapidStr = _actions[i].ToRAPIDFunction();

                // Checks if first movement is MoveAbsJ
                if(foundFirstMovement == false)
                {
                    if(_actions[i] is Movement)
                    {
                       
                        if(((Movement)_actions[i]).IsLinear == true)
                        {
                            _firstMovementIsMoveAbs = false;
                        }
                        else
                        {
                            _firstMovementIsMoveAbs = true;
                        }

                        foundFirstMovement = true;
                    }
                }
                RAPIDCode += rapidStr;
            }

            // Closes Program
            RAPIDCode += "@" + "\t" + "ENDPROC";
            // Closes Module
            RAPIDCode += "@@" + "ENDMODULE";

            // Replace defeaultTool with RobotTool
            //RAPIDCode = RAPIDCode.Replace("tool0", robotinfo.Tool.name);

            // Replaces@ with newLines
            RAPIDCode = RAPIDCode.Replace("@", System.Environment.NewLine);

            _RAPIDCode = RAPIDCode;
            return RAPIDCode;
        }

        public string CreateBaseCode()
        {
            // Creates Main Module
            string BASECode = "MODULE BASE (SYSMODULE, NOSTEPIN, VIEWONLY)@@";

            // Creates Comments
            BASECode += " ! System module with basic predefined system data@";
            BASECode += " !************************************************@@";
            BASECode += " ! System data tool0, wobj0 and load0@";
            BASECode += " ! Do not translate or delete tool0, wobj0, load0@";

            // Creates Predefined System Data
            BASECode += " PERS tooldata tool0 := [TRUE, [[0, 0, 0], [1, 0, 0, 0]],@";
            BASECode += "\t" + "\t" + "\t" + "\t" + "\t" + "\t" + "[0.001, [0, 0, 0.001],[1, 0, 0, 0], 0, 0, 0]];@@";

            BASECode += " PERS wobjdata wobj0 := [FALSE, TRUE, \"\" , [[0, 0, 0],[1, 0, 0, 0]],@";
            BASECode += "\t" + "\t" + "\t" + "\t" + "\t" + "\t" + "[[0, 0, 0],[1, 0, 0, 0]]];@@";

            BASECode += " PERS loaddata load0 := [0.001, [0, 0, 0.001],[1, 0, 0, 0], 0, 0, 0];@@";

            // Creates all available Tool Data 
            foreach (KeyValuePair<Guid, RobotToolFromDataEulerComponent> entry in _objectManager.ToolsEulerByGuid)
            {
                string toolData = "";
                double posX = entry.Value.robTool.AttachmentPlane.Origin.X + entry.Value.robTool.ToolPlane.Origin.X;
                double posY = entry.Value.robTool.AttachmentPlane.Origin.Y + entry.Value.robTool.ToolPlane.Origin.Y;
                double posZ = entry.Value.robTool.AttachmentPlane.Origin.Z + entry.Value.robTool.ToolPlane.Origin.Z;
                Point3d position = new Point3d(posX, posY, posZ);
                Quaternion orientation = entry.Value.robTool.Orientation;
                string name = entry.Value.robTool.Name;
                toolData += " PERS tooldata " + name + " := [TRUE, [[" + position.X.ToString("0.##") + "," + position.Y.ToString("0.##") + "," + position.Z.ToString("0.##") + "], [" + 
                    orientation.A.ToString("0.######") + "," + orientation.B.ToString("0.######") + "," + orientation.C.ToString("0.######") + "," + orientation.D.ToString("0.######") + "]],@";
                toolData += "\t" + "\t" + "\t" + "\t" + "\t" + "\t" + "[0.001, [0, 0, 0.001],[1, 0, 0, 0], 0, 0, 0]];@@";

                BASECode += toolData;
            }

            foreach (KeyValuePair<Guid, RobotToolFromPlanesComponent> entry in _objectManager.ToolsPlanesByGuid)
            {
                string toolData = "";
                double posX = entry.Value.robTool.AttachmentPlane.Origin.X + entry.Value.robTool.ToolPlane.Origin.X;
                double posY = entry.Value.robTool.AttachmentPlane.Origin.Y + entry.Value.robTool.ToolPlane.Origin.Y;
                double posZ = entry.Value.robTool.AttachmentPlane.Origin.Z + entry.Value.robTool.ToolPlane.Origin.Z;
                Point3d position = new Point3d(posX, posY, posZ);
                Quaternion orientation = entry.Value.robTool.Orientation;
                string name = entry.Value.robTool.Name;
                toolData += " PERS tooldata " + name + " := [TRUE, [[" + position.X.ToString("0.##") + "," + position.Y.ToString("0.##") + "," + position.Z.ToString("0.##") + "], [" +
                    orientation.A.ToString("0.######") + ", " + orientation.B.ToString("0.######") + "," + orientation.C.ToString("0.######") + "," + orientation.D.ToString("0.######") + "]],@";
                toolData += "\t" + "\t" + "\t" + "\t" + "\t" + "\t" + "[0.001, [0, 0, 0.001],[1, 0, 0, 0], 0, 0, 0]];@@";

                BASECode += toolData;
            }

            // End Module
            BASECode += "ENDMODULE";

            // Replaces@ with newLines
            BASECode = BASECode.Replace("@", System.Environment.NewLine);

            _BASECode = BASECode;
            return BASECode;
        }

        //writes RAPID Code to File on Harddrive
        public void WriteRAPIDCodeToFile()
        {
            using (StreamWriter writer = new StreamWriter(FilePath + "\\main_T.mod", false))
            {
                writer.WriteLine(_RAPIDCode);
            }
        }

        //writes Base Code to File on Harddrive
        public void WriteBASECodeToFile()
        {
            using (StreamWriter writer = new StreamWriter(FilePath + "\\BASE.sys", false))
            {
                writer.WriteLine(_BASECode);
            }
        }
        #endregion

        #region properties
        public bool IsValid
        {
            get
            {
                if (Actions == null) { return false; }
                return true;
            }
        }
        public List<Action> Actions
        {
            get { return _actions; }
            set { _actions = value; }
        }
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }
        public bool SaveToFile
        {
            get { return _saveToFile; }
            set { _saveToFile = value; }
        }
        public string RAPIDCode
        {
            get { return _RAPIDCode; }
            set { _RAPIDCode = value; }
        }
        public string BASECode
        {
            get { return _BASECode; }
            set { _BASECode = value; }
        }
        public RobotInfo RobotInfo
        {
            get { return _robotInfo; }
            set { _robotInfo = value; }
        }

        public Guid DocumentGUID { get => _documentGUID; set => _documentGUID = value; }
        public ObjectManager ObjectManager { get => _objectManager; set => _objectManager = value; }
        public string ModuleName { get => _ModuleName; set => _ModuleName = value; }
        public bool FirstMovementIsMoveAbs { get => _firstMovementIsMoveAbs;}
        #endregion
    }

}