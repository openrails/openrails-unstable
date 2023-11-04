﻿// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class TrainCarOperationsViewerWindow : Window
    {
        const int CarListPadding = 2;
        internal static Texture2D BattAlwaysOn32;
        internal static Texture2D BattOn32;
        internal static Texture2D BattOff32;
        internal static Texture2D BleedOffValveOpened;
        internal static Texture2D BleedOffValveClosed;
        internal static Texture2D BleedOffValveNotAvailable;
        internal static Texture2D BrakeHoseCon;
        internal static Texture2D BrakeHoseDis;
        internal static Texture2D BrakeHoseFirstDis;
        internal static Texture2D BrakeHoseLastDis;
        internal static Texture2D Coupler;
        internal static Texture2D CouplerFront;
        internal static Texture2D CouplerRear;
        internal static Texture2D Empty;
        internal static Texture2D ETSconnected32;
        internal static Texture2D ETSdisconnected32;
        internal static Texture2D FrontAngleCockOpened;
        internal static Texture2D FrontAngleCockClosed;
        internal static Texture2D HandBrakeSet;
        internal static Texture2D HandBrakeNotSet;
        internal static Texture2D HandBrakeNotAvailable;
        internal static Texture2D LocoSymbol;
        internal static Texture2D LocoSymbolGreen;
        internal static Texture2D LocoSymbolRed;
        internal static Texture2D MUconnected;
        internal static Texture2D MUdisconnected;
        internal static Texture2D PowerOn;
        internal static Texture2D PowerOff;
        internal static Texture2D PowerChanging;
        internal static Texture2D RearAngleCockOpened;
        internal static Texture2D RearAngleCockClosed;

        public int WindowHeightMin;
        public int WindowHeightMax;
        public int WindowWidthMin;
        public int WindowWidthMax;
        public string PowerSupplyStatus;
        public string BatteryStatus;
        string CircuitBreakerState;
        public int SpacerRowCount;
        public int SymbolsRowCount;
        public int LocoRowCount;
        public int RowsCount;
        const int SymbolWidth = 32;
        public static bool FontChanged;
        public static bool FontToBold;
        public int windowHeight { get; set; } //required by TrainCarWindow
        public int CarPosition
        {
            set;
            get;
        }
        public int NewCarPosition
        {
            set;
            get;
        }
        public bool TrainCarOperationsChanged
        {
            set;
            get;
        }
        public bool FrontBrakeHoseChanged
        {
            set;
            get;
        }
        public bool RearBrakeHoseChanged
        {
            set;
            get;
        }
        public bool CouplerChanged
        {
            set;
            get;
        } = false;
        public struct ListLabel
        {
            public string CarID;
            public int CarIDWidth;
        }
        public List<ListLabel> Labels = new List<ListLabel>();

        Train PlayerTrain;
        int LastPlayerTrainCars;
        bool LastPlayerLocomotiveFlippedState;
        int OldCarPosition;
        
        public TrainCarOperationsViewerWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + CarListPadding + ((owner.TextFontDefault.Height + 12) * 20), Window.DecorationSize.Y + ((owner.TextFontDefault.Height + 12) * 2), Viewer.Catalog.GetString("Train Operations Viewer"))
        {
            WindowHeightMin = Location.Height;
            WindowHeightMax = Location.Height + (owner.TextFontDefault.Height * 20);
            WindowWidthMin = Location.Width;
            WindowWidthMax = Location.Width + (owner.TextFontDefault.Height * 20);
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(Location.X);
            outf.Write(Location.Y);
            outf.Write(Location.Width);
            outf.Write(Location.Height);
        }
        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            Rectangle LocationRestore;
            LocationRestore.X = inf.ReadInt32();
            LocationRestore.Y = inf.ReadInt32();
            LocationRestore.Width = inf.ReadInt32();
            LocationRestore.Height = inf.ReadInt32();

            // Display window
            SizeTo(LocationRestore.Width, LocationRestore.Height);
            MoveTo(LocationRestore.X, LocationRestore.Y);
        }
        protected internal override void Initialize()
        {
            base.Initialize();
            // Reset window size
            if (Visible) UpdateWindowSize();

            if (Coupler == null)
            {
                // TODO: This should happen on the loader thread.
                BattAlwaysOn32 = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBattAlwaysOn32.png"));
                BattOn32 = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBattOn32.png"));
                BattOff32 = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBattOff32.png"));

                BleedOffValveClosed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBleedOffValveClosed32.png"));
                BleedOffValveOpened = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBleedOffValveOpened32.png"));
                BleedOffValveNotAvailable = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBleedOffValveNotAvailable32.png"));

                BrakeHoseCon = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseCon32.png"));
                BrakeHoseDis = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseDis32.png"));
                BrakeHoseFirstDis = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseFirstDis32.png"));
                BrakeHoseLastDis = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsBrakeHoseLastDis32.png"));

                Coupler = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCoupler32.png"));
                CouplerFront = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCouplerFront32.png"));
                CouplerRear = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCouplerRear32.png"));

                Empty = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsEmpty32.png"));

                FrontAngleCockClosed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsFrontAngleCockClosed32.png"));
                FrontAngleCockOpened = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsFrontAngleCockOpened32.png"));

                HandBrakeNotAvailable = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsHandBrakeNotAvailable32.png"));
                HandBrakeNotSet = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsHandBrakeNoSet32.png"));
                HandBrakeSet = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsHandBrakeSet32.png"));

                LocoSymbol = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsLoco32.png"));
                LocoSymbolGreen = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsLocoGreen32.png"));
                LocoSymbolRed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsLocoRed32.png"));

                MUconnected = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsMUconnected32.png"));
                MUdisconnected = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsMUdisconnected32.png"));

                ETSconnected32 = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsETSconnected32.png"));
                ETSdisconnected32 = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsETSdisconnected32.png"));

                PowerOn = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsPowerOn32.png"));
                PowerOff = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsPowerOff32.png"));
                PowerChanging = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsPowerChanging32.png"));

                RearAngleCockClosed = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsRearAngleCockClosed32.png"));
                RearAngleCockOpened = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsRearAngleCockOpened32.png"));
            }

            UpdateWindowSize();
        }
        private void UpdateWindowSize()
        {
            ModifyWindowSize();
        }

        /// <summary>
        /// Modify window size
        /// </summary>
        private void ModifyWindowSize()
        {
            if (SymbolsRowCount > 0)
            {
                var desiredHeight = FontToBold ? Owner.TextFontDefaultBold.Height * RowsCount
                    : (Owner.TextFontDefault.Height * RowsCount) + SymbolWidth;
                var desiredWidth = (SymbolsRowCount * SymbolWidth) + (SpacerRowCount * (SymbolWidth / 2)) + (LocoRowCount * (SymbolWidth * 2));

                var newHeight = (int)MathHelper.Clamp(desiredHeight, 80, WindowHeightMax);
                var newWidth = (int)MathHelper.Clamp(desiredWidth, 100, WindowWidthMax);

                // Move the dialog up if we're expanding it, or down if not; this keeps the center in the same place.
                var newTop = Location.Y + ((Location.Height - newHeight) / 2);

                // Display window
                SizeTo(newWidth, newHeight);
                MoveTo(Location.X, newTop);
            }
        }
        public ControlLayoutVertical Vbox;
        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonClose;
            var textHeight = Owner.TextFontDefault.Height;
            textHeight = MathHelper.Clamp(textHeight, SymbolWidth, Owner.TextFontDefault.Height);
            Vbox = base.Layout(layout).AddLayoutVertical();

            if (PlayerTrain != null && PlayerTrain.Cars.Count() > CarPosition)
            {
                TrainCar trainCar = PlayerTrain.Cars[CarPosition];
                BrakeSystem brakeSystem = (trainCar as MSTSWagon).BrakeSystem;
                MSTSLocomotive locomotive = trainCar as MSTSLocomotive;
                MSTSWagon wagon = trainCar as MSTSWagon;

                bool isElectricDieselLocomotive = (trainCar is MSTSElectricLocomotive) || (trainCar is MSTSDieselLocomotive);

                {
                    var isDiesel = trainCar is MSTSDieselLocomotive;
                    var isElectric = trainCar is MSTSElectricLocomotive;
                    var isSteam = trainCar is MSTSSteamLocomotive;
                    var isEngine = isDiesel || isElectric || isSteam;
                    var wagonType = isEngine ? $"  {Viewer.Catalog.GetString(locomotive.WagonType.ToString())}" + $":{Viewer.Catalog.GetString(locomotive.EngineType.ToString())}"
                        : $"  {Viewer.Catalog.GetString(wagon.WagonType.ToString())}";

                    Vbox.Add(buttonClose = new Label(Vbox.RemainingWidth, Owner.TextFontDefault.Height, $"{Viewer.Catalog.GetString("Car ID")} {(CarPosition >= PlayerTrain.Cars.Count ? " " : PlayerTrain.Cars[CarPosition].CarID + wagonType)}", LabelAlignment.Center));
                    buttonClose.Click += new Action<Control, Point>(buttonClose_Click);
                    buttonClose.Color = Owner.Viewer.TrainCarOperationsWindow.WarningCarPosition.Find(x => x == true) ? Color.Cyan : Color.White;
                    Vbox.AddHorizontalSeparator();
                }

                SpacerRowCount = SymbolsRowCount = 0;

                var line = Vbox.AddLayoutHorizontal(Vbox.RemainingHeight);
                var addspace = 0;
                void AddSpace(bool full)
                {
                    line.AddSpace(textHeight / (full ? 1 : 2), line.RemainingHeight);
                    addspace++;
                }

                {
                    var car = PlayerTrain.Cars[CarPosition];
                    //Front brake hose
                    if (car != PlayerTrain.Cars.First())
                    {
                        AddSpace(false);
                        line.Add(new buttonFrontBrakeHose(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        line.Add(new buttonFrontAngleCock(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        AddSpace(false);
                        line.Add(new buttonCouplerFront(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    }
                    else
                    {
                        line.Add(new buttonFrontBrakeHose(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        line.Add(new buttonFrontAngleCock(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        line.Add(new buttonCouplerFront(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                    }

                    line.Add(new buttonLoco(0, 0, textHeight, Owner.Viewer, car));

                    if (car != PlayerTrain.Cars.Last())
                    {
                        line.Add(new buttonCouplerRear(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        AddSpace(false);
                        line.Add(new buttonRearAngleCock(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        line.Add(new buttonRearBrakeHose(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        AddSpace(false);
                        line.Add(new buttonHandBrake(0, 0, textHeight, Owner.Viewer, CarPosition));
                    }
                    else
                    {
                        line.Add(new buttonCouplerRear(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        AddSpace(false);
                        line.Add(new buttonRearAngleCock(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        line.Add(new buttonRearBrakeHose(0, 0, textHeight, Owner.Viewer, car, CarPosition));
                        AddSpace(false);
                        line.Add(new buttonHandBrake(0, 0, textHeight, Owner.Viewer, CarPosition));
                    }
                    AddSpace(false);
                    line.Add(new buttonBleedOffValve(0, 0, textHeight, Owner.Viewer, CarPosition));
                    AddSpace(false);

                    // Electric Train Supply Connection
                    if (PlayerTrain.Cars.Count() > 1 && wagon.PowerSupply != null)
                    {
                        line.Add(new ToggleElectricTrainSupplyCable(0, 0, textHeight, Owner.Viewer, CarPosition));
                        AddSpace(false);
                    }
                    if (isElectricDieselLocomotive)
                    {
                        if (locomotive.GetMultipleUnitsConfiguration() != null)
                        {
                            line.Add(new buttonToggleMU(0, 0, textHeight, Owner.Viewer, CarPosition));
                            AddSpace(false);
                        }
                        line.Add(new buttonTogglePower(0, 0, textHeight, Owner.Viewer, CarPosition));
                        AddSpace(false);
                        if ((wagon != null) && (wagon.PowerSupply is IPowerSupply))
                        {
                            line.Add(new ToggleBatterySwitch(0, 0, textHeight, Owner.Viewer, CarPosition));
                            AddSpace(false);
                        }
                    }
                    buttonClose.Color = Owner.Viewer.TrainCarOperationsWindow.WarningCarPosition.Find(x => x == true) ? Color.Cyan : Color.White;

                    RowsCount = Vbox.Controls.Count();
                    SpacerRowCount = line.Controls.Where(c => c is Orts.Viewer3D.Popups.Spacer).Count();
                    LocoRowCount = line.Controls.Where(c => c is Orts.Viewer3D.Popups.TrainCarOperationsViewerWindow.buttonLoco).Count() + 1;
                    SymbolsRowCount = line.Controls.Count() - SpacerRowCount - LocoRowCount;
                }
            }
            return Vbox;
        }
        void buttonClose_Click(Control arg1, Point arg2)
        {
            OldCarPosition = CarPosition;
            Visible = false;
        }
        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var carOperations = Owner.Viewer.CarOperationsWindow;
                var trainCarOperations = Owner.Viewer.TrainCarOperationsWindow;                

                if (CouplerChanged || PlayerTrain != Owner.Viewer.PlayerTrain || Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars || (Owner.Viewer.PlayerLocomotive != null &&
                LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped))
                {
                    CouplerChanged = false;
                    PlayerTrain = Owner.Viewer.PlayerTrain;

                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    CarPosition = CarPosition >= LastPlayerTrainCars? LastPlayerTrainCars - 1: CarPosition;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;
 
                    Layout();
                    UpdateWindowSize();
                }

                TrainCar trainCar = Owner.Viewer.PlayerTrain.Cars[CarPosition];
                bool isElectricDieselLocomotive = (trainCar is MSTSElectricLocomotive) || (trainCar is MSTSDieselLocomotive);
                
                if (OldCarPosition != CarPosition || TrainCarOperationsChanged || carOperations.CarOperationChanged
                    || trainCarOperations.CarIdClicked || carOperations.RearBrakeHoseChanged || carOperations.FrontBrakeHoseChanged)
                {
                    // Updates CarPosition
                    CarPosition = CouplerChanged ? NewCarPosition : CarPosition;
                    
                    if (OldCarPosition != CarPosition || (trainCarOperations.CarIdClicked && CarPosition == 0))
                    {
                        Owner.Viewer.FrontCamera.Activate();
                    }
                    OldCarPosition = CarPosition;
                    Layout();
                    UpdateWindowSize();
                    TrainCarOperationsChanged = false;

                    // Avoids bug
                    carOperations.CarOperationChanged = carOperations.Visible && carOperations.CarOperationChanged;
                }
                // Updates power supply status
                else if (isElectricDieselLocomotive &&
                     (PowerSupplyStatus != null && PowerSupplyStatus != Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus()
                      || (BatteryStatus != null && BatteryStatus != Owner.Viewer.PlayerTrain.Cars[CarPosition].GetStatus())
                      || (CircuitBreakerState != null && CircuitBreakerState != (trainCar as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.State.ToString())))
                {
                    Layout();
                    UpdateWindowSize();
                    TrainCarOperationsChanged = true;
                }
                //required by traincarwindow to ModifyWindowSize()
                windowHeight = Vbox != null ? Vbox.Position.Height : 0;
            }
        }
        
        class buttonLoco : Image
        {
            readonly Viewer Viewer;
            public buttonLoco(int x, int y, int size, Viewer viewer, TrainCar car)
               : base(x, y, size * 2, size)
            {
                Viewer = viewer;
                Texture = (car == Viewer.PlayerTrain.LeadLocomotive || car is MSTSLocomotive) ? LocoSymbolGreen
                    : car.BrakesStuck || ((car is MSTSLocomotive) && (car as MSTSLocomotive).PowerReduction > 0) ? LocoSymbolRed
                    : LocoSymbol;
                Source = new Rectangle(0, 0, size * 2, size);
            }
        }
        class buttonCouplerFront : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool First;

            public buttonCouplerFront(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                First = car == Viewer.PlayerTrain.Cars.First();
                Texture = First ? CouplerFront : Coupler;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(TrainCarOperationsCouplerFront_Click);
            }

            void TrainCarOperationsCouplerFront_Click(Control arg1, Point arg2)
            {
                if (First) return;

                if (Viewer.Simulator.TimetableMode)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
                }
                else
                {
                    new UncoupleCommand(Viewer.Log, CarPosition - 1);
                    TrainCarViewer.CouplerChanged = true;
                    TrainCarViewer.NewCarPosition = CarPosition - 1;
                    if (Viewer.CarOperationsWindow.CarPosition > CarPosition - 1)
                        Viewer.CarOperationsWindow.Visible = false;
                }
            }
        }
        class buttonCouplerRear : Image
        {
            readonly Viewer Viewer;
            readonly int CarPosition;
            readonly bool Last;
            public buttonCouplerRear(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                CarPosition = carPosition;
                Last = car == Viewer.PlayerTrain.Cars.Last();
                Texture = Last ? CouplerRear : Coupler;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(TrainCarOperationsCouplerRear_Click);
            }

            void TrainCarOperationsCouplerRear_Click(Control arg1, Point arg2)
            {
                if (Last) return;

                if (Viewer.Simulator.TimetableMode)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
                }
                else
                {
                    new UncoupleCommand(Viewer.Log, CarPosition);
                    if (Viewer.CarOperationsWindow.CarPosition > CarPosition)
                        Viewer.CarOperationsWindow.Visible = false;
                }
            }
        }
        class buttonLabel : Label
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;

            public buttonLabel(int x, int y, Viewer viewer, TrainCar car, LabelAlignment alignment)
                : base(x, y, "", alignment)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                Text = car.CarID;
                Click += new Action<Control, Point>(TrainCarOperationsLabel_Click);
            }

            void TrainCarOperationsLabel_Click(Control arg1, Point arg2)
            {
                TrainCarViewer.Visible = false;
            }
        }
        class buttonHandBrake : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonHandBrake(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).HandBrakePresent ? (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).GetTrainHandbrakeStatus() ? HandBrakeSet : HandBrakeNotSet : HandBrakeNotAvailable;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonHandBrake_Click);
            }
            void buttonHandBrake_Click(Control arg1, Point arg2)
            {
                if (Viewer.Simulator.TimetableMode)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
                }
                else
                {
                    if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).HandBrakePresent)
                    {
                        new WagonHandbrakeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus());
                        if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus())
                        {
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake set"));
                            Texture = HandBrakeSet;
                        }
                        else
                        {
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake off"));
                            Texture = HandBrakeNotSet;
                        }
                        TrainCarViewer.TrainCarOperationsChanged = true;
                    }
                }
            }
        }
        class buttonFrontBrakeHose : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly TrainCarOperationsWindow TrainCar;
            readonly CarOperationsWindow CarOperations;

            readonly int CarPosition;
            readonly bool First;

            public buttonFrontBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                TrainCar = Viewer.TrainCarOperationsWindow;
                CarOperations = Viewer.CarOperationsWindow;
                CarPosition = carPosition;
                First = car == viewer.PlayerTrain.Cars.First();
                Texture = First ? BrakeHoseFirstDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;
                // Allows compatibility with CarOperationWindow
                var brakeHoseChanged = CarOperations.FrontBrakeHoseChanged || CarOperations.RearBrakeHoseChanged;
                if (brakeHoseChanged && CarOperations.Visible && CarOperations.CarPosition >= 1 && CarOperations.CarPosition == CarPosition)
                {
                    var rearBrakeHose = CarOperations.RearBrakeHoseChanged;
                    if (rearBrakeHose)
                    {
                        new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarOperations.CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarOperations.CarPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                        CarOperations.RearBrakeHoseChanged = false;
                    }
                    else
                    {
                        new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarOperations.CarPosition - 1] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarOperations.CarPosition - 1] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                        CarOperations.FrontBrakeHoseChanged = false;
                    }
                    TrainCar.ModifiedSetting = true;
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }

                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonFrontBrakeHose_Click);
            }

            void buttonFrontBrakeHose_Click(Control arg1, Point arg2)
            {
                if (First) return;

                new WagonBrakeHoseConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose connected"));
                    Texture = BrakeHoseCon;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose disconnected"));
                    Texture = BrakeHoseDis;
                }
                TrainCarViewer.FrontBrakeHoseChanged = true;
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonRearBrakeHose : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool Last;
            public buttonRearBrakeHose(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Last = car == viewer.PlayerTrain.Cars.Last();
                Texture = Last ? BrakeHoseLastDis : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected ? BrakeHoseCon : BrakeHoseDis;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonRearBrakeHose_Click);
            }

            void buttonRearBrakeHose_Click(Control arg1, Point arg2)
            {
                if (Last) return;

                new WagonBrakeHoseRearConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RearBrakeHoseConnected)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear brake hose connected"));
                    Texture = BrakeHoseCon;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear brake hose disconnected"));
                    Texture = BrakeHoseDis;
                }
                TrainCarViewer.RearBrakeHoseChanged = true;
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonFrontAngleCock : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool First;

            public buttonFrontAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                First = car == Viewer.PlayerTrain.Cars.First();
                Texture = First ? FrontAngleCockClosed : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen ? FrontAngleCockOpened : FrontAngleCockClosed;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonFrontAngleCock_Click);
            }

            void buttonFrontAngleCock_Click(Control arg1, Point arg2)
            {
                if (First) return;

                new ToggleAngleCockACommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock opened"));
                    Texture = FrontAngleCockOpened;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock closed"));
                    Texture = FrontAngleCockClosed;
                }
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonRearAngleCock : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;
            readonly bool Last;

            public buttonRearAngleCock(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                Last = car == Viewer.PlayerTrain.Cars.Last();
                Texture = Last ? RearAngleCockClosed : (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen ? RearAngleCockOpened : RearAngleCockClosed;
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonRearAngleCock_Click);
            }

            void buttonRearAngleCock_Click(Control arg1, Point arg2)
            {
                if (Last) return;

                new ToggleAngleCockBCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock opened"));
                    Texture = RearAngleCockOpened;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock closed"));
                    Texture = RearAngleCockClosed;
                }
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class buttonBleedOffValve : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonBleedOffValve(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe
                    || Viewer.PlayerTrain.Cars.Count() == 1)
                {
                    Texture = BleedOffValveNotAvailable;
                }
                else
                {
                    Texture = (viewer.PlayerTrain.Cars[carPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen ? BleedOffValveOpened : BleedOffValveClosed;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonBleedOffValve_Click);
            }

            void buttonBleedOffValve_Click(Control arg1, Point arg2)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe
                    || Viewer.PlayerTrain.Cars.Count() == 1)
                {
                    Texture = BleedOffValveNotAvailable;
                    return;
                }
                new ToggleBleedOffValveCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve opened"));
                    Texture = BleedOffValveOpened;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve closed"));
                    Texture = BleedOffValveClosed;
                }
                TrainCarViewer.TrainCarOperationsChanged = true;
            }
        }
        class ToggleElectricTrainSupplyCable : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public ToggleElectricTrainSupplyCable(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                MSTSWagon wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

                if (wagon.PowerSupply != null && Viewer.PlayerTrain.Cars.Count() > 1)
                {
                    Texture = wagon.PowerSupply.FrontElectricTrainSupplyCableConnected ? ETSconnected32 : ETSdisconnected32;
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(ToggleElectricTrainSupplyCable_Click);
            }
            void ToggleElectricTrainSupplyCable_Click(Control arg1, Point arg2)
            {
                if (Viewer.PlayerTrain.Cars.Count() == 1)
                    return;

                MSTSWagon wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

                if (wagon.PowerSupply != null)
                {
                    new ConnectElectricTrainSupplyCableCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !wagon.PowerSupply.FrontElectricTrainSupplyCableConnected);
                    if (wagon.PowerSupply.FrontElectricTrainSupplyCableConnected)
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable connected"));
                    else
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable disconnected"));

                    Texture = wagon.PowerSupply.FrontElectricTrainSupplyCableConnected ? ETSconnected32 : ETSdisconnected32;
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("This car doesn't have an ETS system"));
                }
            }
        }
        class buttonToggleMU : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonToggleMU(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                var multipleUnitsConfiguration = Viewer.PlayerLocomotive.GetMultipleUnitsConfiguration();
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive && multipleUnitsConfiguration != null)
                {
                    Texture = Viewer.TrainCarOperationsWindow.ModifiedSetting || ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).RemoteControlGroup == 0 && multipleUnitsConfiguration != "1")? MUconnected : MUdisconnected;
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonToggleMU_Click);
            }
            void buttonToggleMU_Click(Control arg1, Point arg2)
            {
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive)
                {
                    MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

                    new ToggleMUCommand(Viewer.Log, locomotive, locomotive.RemoteControlGroup < 0);
                    if (locomotive.RemoteControlGroup == 0)
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal connected"));
                    else
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal disconnected"));

                    Texture = locomotive.RemoteControlGroup == 0 ? MUconnected : MUdisconnected;
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                else
                    Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No MU command for this type of car!"));
            }
        }
        class buttonTogglePower : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public buttonTogglePower(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive) || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
                {
                    Texture = locomotiveStatus1(CarPosition);
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(buttonTogglePower_Click);
            }
            void buttonTogglePower_Click(Control arg1, Point arg2)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive)
                    || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
                {
                    MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

                    new PowerCommand(Viewer.Log, locomotive, !locomotive.LocomotivePowerSupply.MainPowerSupplyOn);
                    var mainPowerSupplyOn = locomotive.LocomotivePowerSupply.MainPowerSupplyOn;
                    if (mainPowerSupplyOn)
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
                    else
                        Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));

                    Texture = locomotiveStatus1(CarPosition);
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
                else
                    Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No power command for this type of car!"));
            }
            public Texture2D locomotiveStatus1(int CarPosition)
            {
                string locomotiveStatus = Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                foreach (string data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
                {
                    string[] parts = data.Split(new string[] { " = " }, 2, StringSplitOptions.None);
                    string keyPart = parts[0];
                    string valuePart = parts?[1];
                    if (keyPart.Contains(Viewer.Catalog.GetString("Engine")))
                    {
                        TrainCarViewer.PowerSupplyStatus = locomotiveStatus;
                        Texture = valuePart.Contains(Viewer.Catalog.GetString("Running")) ? PowerOn
                           : valuePart.Contains(Viewer.Catalog.GetString("Stopped")) ? PowerOff
                           : PowerChanging;
                        break;
                    }

                    MSTSElectricLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSElectricLocomotive;
                    switch (locomotive.ElectricPowerSupply.CircuitBreaker.State)
                    {
                        case ORTS.Scripting.Api.CircuitBreakerState.Closed:
                            Texture = PowerOn;
                            break;
                        case ORTS.Scripting.Api.CircuitBreakerState.Closing:
                            Texture = PowerChanging;
                            break;
                        case ORTS.Scripting.Api.CircuitBreakerState.Open:
                            Texture = PowerOff;
                            break;
                    }
                    TrainCarViewer.CircuitBreakerState = locomotive.ElectricPowerSupply.CircuitBreaker.State.ToString();
                }
                return Texture;
            }
        }
        class ToggleBatterySwitch : Image
        {
            readonly Viewer Viewer;
            readonly TrainCarOperationsViewerWindow TrainCarViewer;
            readonly int CarPosition;

            public ToggleBatterySwitch(int x, int y, int size, Viewer viewer, int carPosition)
                : base(x, y, size, size)
            {
                Viewer = viewer;
                TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
                CarPosition = carPosition;

                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSWagon wagon
                    && wagon.PowerSupply is IPowerSupply)
                {
                    if (wagon.PowerSupply.BatterySwitch.Mode == BatterySwitch.ModeType.AlwaysOn)
                    {
                        Texture = BattAlwaysOn32;
                    }
                    else
                    {
                        Texture = locomotiveStatus(CarPosition);
                    }
                }
                else
                {
                    Texture = Empty;
                }
                Source = new Rectangle(0, 0, size, size);
                Click += new Action<Control, Point>(ToggleBatterySwitch_Click);
            }
            void ToggleBatterySwitch_Click(Control arg1, Point arg2)
            {
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSWagon wagon
                    && wagon.PowerSupply is IPowerSupply)
                {
                    if (wagon.PowerSupply.BatterySwitch.Mode == BatterySwitch.ModeType.AlwaysOn)
                    {
                        return;
                    }
                    else
                    {
                        new ToggleBatterySwitchCommand(Viewer.Log, wagon, !wagon.PowerSupply.BatterySwitch.On);

                        if (wagon.PowerSupply.BatterySwitch.On)
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch off battery command sent"));
                        else
                            Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch on battery command sent"));

                        Texture = locomotiveStatus(CarPosition);
                    }
                    TrainCarViewer.TrainCarOperationsChanged = true;
                }
            }
            public Texture2D locomotiveStatus(int CarPosition)
            {
                string locomotiveStatus = Viewer.PlayerTrain.Cars[CarPosition].GetStatus();
                foreach (string data in locomotiveStatus.Split('\n').Where((string d) => !string.IsNullOrWhiteSpace(d)))
                {
                    string[] parts = data.Split(new string[] { " = " }, 2, StringSplitOptions.None);
                    string keyPart = parts[0];
                    string valuePart = parts?[1];
                    if (keyPart.Contains(Viewer.Catalog.GetString("Battery")))
                    {
                        TrainCarViewer.BatteryStatus = locomotiveStatus;
                        Texture = valuePart.Contains(Viewer.Catalog.GetString("On")) ? BattOn32 : BattOff32;
                        break;
                    }
                }
                return Texture;
            }
        }
    }
}
