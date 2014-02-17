using System;
using System.Windows.Forms;
using SharpFlame.Collections;
using SharpFlame.Controls;
using SharpFlame.FileIO;
using SharpFlame.Mapping;
using SharpFlame.Mapping.Objects;
using SharpFlame.Mapping.Script;
using SharpFlame.Mapping.Tools;
using SharpFlame.Maths;
using SharpFlame.Painters;
using SharpFlame.AppSettings;
using SharpFlame.Util;

namespace SharpFlame
{
    public class clsViewInfo
    {
        public clsMap Map;
        public MapViewControl MapViewControl;
        public sXYZ_int ViewPos;
        public Matrix3DMath.Matrix3D ViewAngleMatrix = new Matrix3DMath.Matrix3D ();
        public Matrix3DMath.Matrix3D ViewAngleMatrix_Inverted = new Matrix3DMath.Matrix3D ();
        public Angles.AngleRPY ViewAngleRPY;
        public double FOVMultiplier;
        public double FOVMultiplierExponent;
        public float FieldOfViewY;

        public clsViewInfo (clsMap map, MapViewControl mapViewControl)
        {
            this.Map = map;
            this.MapViewControl = mapViewControl;

            ViewPos = new sXYZ_int (0, 3072, 0);
            FOV_Multiplier_Set (SettingsManager.Settings.FOVDefault);
            ViewAngleSetToDefault ();
            LookAtPos (new sXY_int ((int)(map.Terrain.TileSize.X * App.TerrainGridSpacing / 2.0D),
                                  (int)(map.Terrain.TileSize.Y * App.TerrainGridSpacing / 2.0D)));
        }

        public void FOV_Scale_2E_Set (double power)
        {
            FOVMultiplierExponent = power;
            FOVMultiplier = Math.Pow (2.0D, FOVMultiplierExponent);

            FOV_Calc ();
        }

        public void FOV_Scale_2E_Change (double powerChange)
        {
            FOVMultiplierExponent += powerChange;
            FOVMultiplier = Math.Pow (2.0D, FOVMultiplierExponent);

            FOV_Calc ();
        }

        public void FOV_Set (double radians, MapViewControl mapViewControl)
        {
            FOVMultiplier = Math.Tan (radians / 2.0D) / mapViewControl.GLSize.Y * 2.0D;
            FOVMultiplierExponent = Math.Log (FOVMultiplier) / Math.Log (2.0D);

            FOV_Calc ();
        }

        public void FOV_Multiplier_Set (double value)
        {
            FOVMultiplier = value;
            FOVMultiplierExponent = Math.Log (FOVMultiplier) / Math.Log (2.0D);

            FOV_Calc ();
        }

        public void FOV_Calc ()
        {
            const float min = (float)(0.1d * MathUtil.RadOf1Deg);
            const float max = (float)(179.0d * MathUtil.RadOf1Deg);

            FieldOfViewY = (float)(Math.Atan (MapViewControl.GLSize.Y * FOVMultiplier / 2.0D) * 2.0D);
            if (FieldOfViewY < min) {
                FieldOfViewY = min;
                if (MapViewControl.GLSize.Y > 0) {
                    FOVMultiplier = 2.0D * Math.Tan (FieldOfViewY / 2.0D) / MapViewControl.GLSize.Y;
                    FOVMultiplierExponent = Math.Log (FOVMultiplier) / Math.Log (2.0D);
                }
            } else if (FieldOfViewY > max) {
                FieldOfViewY = max;
                if (MapViewControl.GLSize.Y > 0) {
                    FOVMultiplier = 2.0D * Math.Tan (FieldOfViewY / 2.0D) / MapViewControl.GLSize.Y;
                    FOVMultiplierExponent = Math.Log (FOVMultiplier) / Math.Log (2.0D);
                }
            }

            MapViewControl.DrawViewLater ();
        }

        public void ViewPosSet (sXYZ_int newViewPos)
        {
            ViewPos = newViewPos;
            ViewPosClamp ();

            MapViewControl.DrawViewLater ();
        }

        public void ViewPosChange (sXYZ_int displacement)
        {
            ViewPos.X += displacement.X;
            ViewPos.Z += displacement.Z;
            ViewPos.Y += displacement.Y;
            ViewPosClamp ();

            MapViewControl.DrawViewLater ();
        }

        private void ViewPosClamp ()
        {
            const int maxHeight = 1048576;
            const int maxDist = 1048576;

            ViewPos.X = MathUtil.Clamp_int (ViewPos.X, Convert.ToInt32 (- maxDist), Map.Terrain.TileSize.X * App.TerrainGridSpacing + maxDist);
            ViewPos.Z = MathUtil.Clamp_int (ViewPos.Z, - Map.Terrain.TileSize.Y * App.TerrainGridSpacing - maxDist, maxDist);
            ViewPos.Y = MathUtil.Clamp_int (ViewPos.Y, ((int)(Math.Ceiling (Map.GetTerrainHeight (new sXY_int (ViewPos.X, - ViewPos.Z))))) + 16, maxHeight);
        }

        public void ViewAngleSet (Matrix3DMath.Matrix3D newMatrix)
        {
            Matrix3DMath.MatrixCopy (newMatrix, ViewAngleMatrix);
            Matrix3DMath.MatrixNormalize (ViewAngleMatrix);
            Matrix3DMath.MatrixInvert (ViewAngleMatrix, ViewAngleMatrix_Inverted);
            Matrix3DMath.MatrixToRPY (ViewAngleMatrix, ref ViewAngleRPY);

            MapViewControl.DrawViewLater ();
        }

        public void ViewAngleSetToDefault ()
        {
            Matrix3DMath.Matrix3D matrixA = new Matrix3DMath.Matrix3D ();
            Matrix3DMath.MatrixSetToXAngle (matrixA, Math.Atan (2.0D));
            ViewAngleSet (matrixA);

            MapViewControl.DrawViewLater ();
        }

        public void ViewAngleSet_Rotate (Matrix3DMath.Matrix3D NewMatrix)
        {
            bool Flag = default(bool);
            Position.XYZ_dbl XYZ_dbl = default(Position.XYZ_dbl);
            Position.XYZ_dbl XYZ_dbl2 = default(Position.XYZ_dbl);
            //Dim XYZ_lng As sXYZ_lng
            Position.XY_dbl XY_dbl = default(Position.XY_dbl);

            if (App.ViewMoveType == enumView_Move_Type.RTS & App.RTSOrbit) {
                Flag = true;
                //If ScreenXY_Get_TerrainPos(CInt(Int(GLSize.X / 2.0#)), CInt(Int(GLSize.Y / 2.0#)), XYZ_lng) Then
                //    XYZ_dbl.X = XYZ_lng.X
                //    XYZ_dbl.Y = XYZ_lng.Y
                //    XYZ_dbl.Z = XYZ_lng.Z
                //Else
                if (ScreenXY_Get_ViewPlanePos_ForwardDownOnly ((int)((MapViewControl.GLSize.X / 2.0D)), (int)((MapViewControl.GLSize.Y / 2.0D)), 127.5D,
                                                               ref XY_dbl)) {
                    XYZ_dbl.X = XY_dbl.X;
                    XYZ_dbl.Y = 127.5D;
                    XYZ_dbl.Z = Convert.ToDouble (- XY_dbl.Y);
                } else {
                    Flag = false;
                }
                //End If
            } else {
                Flag = false;
            }

            Matrix3DMath.MatrixToRPY (NewMatrix, ref ViewAngleRPY);
            if (Flag) {
                if (ViewAngleRPY.Pitch < MathUtil.RadOf1Deg * 10.0D) {
                    ViewAngleRPY.Pitch = MathUtil.RadOf1Deg * 10.0D;
                }
            }
            Matrix3DMath.MatrixSetToRPY (ViewAngleMatrix, ViewAngleRPY);
            Matrix3DMath.MatrixInvert (ViewAngleMatrix, ViewAngleMatrix_Inverted);

            if (Flag) {
                XYZ_dbl2.X = ViewPos.X;
                XYZ_dbl2.Y = ViewPos.Y;
                XYZ_dbl2.Z = Convert.ToDouble (- ViewPos.Z);
                MoveToViewTerrainPosFromDistance (XYZ_dbl, Convert.ToDouble ((XYZ_dbl2 - XYZ_dbl).GetMagnitude ()));
            }

            MapViewControl.DrawViewLater ();
        }

        public void LookAtTile (sXY_int TileNum)
        {
            sXY_int Pos = new sXY_int ();

            Pos.X = (int)((TileNum.X + 0.5D) * App.TerrainGridSpacing);
            Pos.Y = (int)((TileNum.Y + 0.5D) * App.TerrainGridSpacing);
            LookAtPos (Pos);
        }

        public void LookAtPos (sXY_int Horizontal)
        {
            Position.XYZ_dbl XYZ_dbl = default(Position.XYZ_dbl);
            sXYZ_int XYZ_int = new sXYZ_int ();
            double dblTemp = 0;
            int A = 0;
            Matrix3DMath.Matrix3D matrixA = new Matrix3DMath.Matrix3D ();
            Angles.AnglePY AnglePY = default(Angles.AnglePY);

            Matrix3DMath.VectorForwardsRotationByMatrix (ViewAngleMatrix, ref XYZ_dbl);
            dblTemp = Map.GetTerrainHeight (Horizontal);
            A = ((int)(Math.Ceiling (dblTemp))) + 128;
            if (ViewPos.Y < A) {
                ViewPos.Y = A;
            }
            if (XYZ_dbl.Y > -0.33333333333333331D) {
                XYZ_dbl.Y = -0.33333333333333331D;
                Matrix3DMath.VectorToPY (XYZ_dbl, ref AnglePY);
                Matrix3DMath.MatrixSetToPY (matrixA, AnglePY);
                ViewAngleSet (matrixA);
            }
            dblTemp = (ViewPos.Y - dblTemp) / XYZ_dbl.Y;

            XYZ_int.X = (int)(Horizontal.X + dblTemp * XYZ_dbl.X);
            XYZ_int.Y = ViewPos.Y;
            XYZ_int.Z = (int)(- Horizontal.Y + dblTemp * XYZ_dbl.Z);

            ViewPosSet (XYZ_int);
        }

        public void MoveToViewTerrainPosFromDistance (Position.XYZ_dbl TerrainPos, double Distance)
        {
            Position.XYZ_dbl XYZ_dbl = default(Position.XYZ_dbl);
            sXYZ_int XYZ_int = new sXYZ_int ();

            Matrix3DMath.VectorForwardsRotationByMatrix (ViewAngleMatrix, ref XYZ_dbl);

            XYZ_int.X = (int)(TerrainPos.X - XYZ_dbl.X * Distance);
            XYZ_int.Y = (int)(TerrainPos.Y - XYZ_dbl.Y * Distance);
            XYZ_int.Z = (int)(- TerrainPos.Z - XYZ_dbl.Z * Distance);

            ViewPosSet (XYZ_int);
        }

        public bool Pos_Get_Screen_XY (Position.XYZ_dbl Pos, ref sXY_int Result)
        {
            if (Pos.Z <= 0.0D) {
                return false;
            }

            try {
                double RatioZ_px = 1.0D / (FOVMultiplier * Pos.Z);
                Result.X = (int)(MapViewControl.GLSize.X / 2.0D + (Pos.X * RatioZ_px));
                Result.Y = (int)(MapViewControl.GLSize.Y / 2.0D - (Pos.Y * RatioZ_px));
                return true;
            } catch {
            }

            return false;
        }

        public bool ScreenXY_Get_ViewPlanePos (sXY_int ScreenPos, double PlaneHeight, ref Position.XY_dbl ResultPos)
        {
            double dblTemp = 0;
            Position.XYZ_dbl XYZ_dbl = default(Position.XYZ_dbl);
            Position.XYZ_dbl XYZ_dbl2 = default(Position.XYZ_dbl);

            try {
                //convert screen pos to vector of one pos unit
                XYZ_dbl.X = (ScreenPos.X - MapViewControl.GLSize.X / 2.0D) * FOVMultiplier;
                XYZ_dbl.Y = (MapViewControl.GLSize.Y / 2.0D - ScreenPos.Y) * FOVMultiplier;
                XYZ_dbl.Z = 1.0D;
                //factor in the view angle
                Matrix3DMath.VectorRotationByMatrix (ViewAngleMatrix, XYZ_dbl, ref XYZ_dbl2);
                //get distance to cover the height
                dblTemp = (PlaneHeight - ViewPos.Y) / XYZ_dbl2.Y;
                ResultPos.X = ViewPos.X + XYZ_dbl2.X * dblTemp;
                ResultPos.Y = ViewPos.Z + XYZ_dbl2.Z * dblTemp;
            } catch {
                return false;
            }
            return true;
        }

        public bool ScreenXY_Get_TerrainPos (sXY_int screenPos, ref sWorldPos resultPos)
        {
            double dblTemp = 0;
            Position.XYZ_dbl xYZ_dbl = default(Position.XYZ_dbl);
            Position.XYZ_dbl terrainViewVector = default(Position.XYZ_dbl);
            int x = 0;
            int y = 0;
            Position.XY_dbl limitA = default(Position.XY_dbl);
            Position.XY_dbl limitB = default(Position.XY_dbl);
            sXY_int min = new sXY_int ();
            sXY_int max = new sXY_int ();
            double triGradientX = 0;
            double triGradientZ = 0;
            double triHeightOffset = 0;
            double dist = 0;
            Position.XYZ_dbl bestPos = default(Position.XYZ_dbl);
            double bestDist = 0;
            Position.XYZ_dbl dif = default(Position.XYZ_dbl);
            double inTileX = 0;
            double inTileZ = 0;
            Position.XY_dbl tilePos = default(Position.XY_dbl);
            Position.XYZ_dbl terrainViewPos = default(Position.XYZ_dbl);

            try {
                terrainViewPos.X = ViewPos.X;
                terrainViewPos.Y = ViewPos.Y;
                terrainViewPos.Z = Convert.ToDouble (- ViewPos.Z);

                //convert screen pos to vector of one pos unit
                xYZ_dbl.X = (screenPos.X - MapViewControl.GLSize.X / 2.0D) * FOVMultiplier;
                xYZ_dbl.Y = (MapViewControl.GLSize.Y / 2.0D - screenPos.Y) * FOVMultiplier;
                xYZ_dbl.Z = 1.0D;
                //rotate the vector so that it points forward and level
                Matrix3DMath.VectorRotationByMatrix (ViewAngleMatrix, xYZ_dbl, ref terrainViewVector);
                terrainViewVector.Y = Convert.ToDouble (- terrainViewVector.Y); //get the amount of looking down, not up
                terrainViewVector.Z = Convert.ToDouble (- terrainViewVector.Z); //convert to terrain coordinates from view coordinates
                //get range of possible tiles
                dblTemp = (terrainViewPos.Y - 255 * Map.HeightMultiplier) / terrainViewVector.Y;
                limitA.X = terrainViewPos.X + terrainViewVector.X * dblTemp;
                limitA.Y = terrainViewPos.Z + terrainViewVector.Z * dblTemp;
                dblTemp = terrainViewPos.Y / terrainViewVector.Y;
                limitB.X = terrainViewPos.X + terrainViewVector.X * dblTemp;
                limitB.Y = terrainViewPos.Z + terrainViewVector.Z * dblTemp;
                min.X = Math.Max (Convert.ToInt32 ((Math.Min (limitA.X, limitB.X) / App.TerrainGridSpacing)), 0);
                min.Y = Math.Max ((int)((Math.Min (limitA.Y, limitB.Y) / App.TerrainGridSpacing)), 0);
                max.X = Math.Min (Convert.ToInt32 ((Math.Max (limitA.X, limitB.X) / App.TerrainGridSpacing)), Map.Terrain.TileSize.X - 1);
                max.Y = Math.Min (Convert.ToInt32 ((Math.Max (limitA.Y, limitB.Y) / App.TerrainGridSpacing)), Map.Terrain.TileSize.Y - 1);
                //find the nearest valid tile to the view
                bestDist = double.MaxValue;
                bestPos.X = double.NaN;
                bestPos.Y = double.NaN;
                bestPos.Z = double.NaN;
                for (y = min.Y; y <= max.Y; y++) {
                    for (x = min.X; x <= max.X; x++) {
                        tilePos.X = x * App.TerrainGridSpacing;
                        tilePos.Y = y * App.TerrainGridSpacing;

                        if (Map.Terrain.Tiles [x, y].Tri) {
                            triHeightOffset = Convert.ToDouble (Map.Terrain.Vertices [x, y].Height * Map.HeightMultiplier);
                            triGradientX = Convert.ToDouble (Map.Terrain.Vertices [x + 1, y].Height * Map.HeightMultiplier - triHeightOffset);
                            triGradientZ = Convert.ToDouble (Map.Terrain.Vertices [x, y + 1].Height * Map.HeightMultiplier - triHeightOffset);
                            xYZ_dbl.Y = (triHeightOffset +
                                (triGradientX * (terrainViewPos.X - tilePos.X) + triGradientZ * (terrainViewPos.Z - tilePos.Y) +
                                (triGradientX * terrainViewVector.X + triGradientZ * terrainViewVector.Z) * terrainViewPos.Y / terrainViewVector.Y) /
                                App.TerrainGridSpacing) /
                                (1.0D +
                                (triGradientX * terrainViewVector.X + triGradientZ * terrainViewVector.Z) / (terrainViewVector.Y * App.TerrainGridSpacing));
                            xYZ_dbl.X = terrainViewPos.X + terrainViewVector.X * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            xYZ_dbl.Z = terrainViewPos.Z + terrainViewVector.Z * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            inTileX = xYZ_dbl.X / App.TerrainGridSpacing - x;
                            inTileZ = xYZ_dbl.Z / App.TerrainGridSpacing - y;
                            if (inTileZ <= 1.0D - inTileX & inTileX >= 0.0D & inTileZ >= 0.0D & inTileX <= 1.0D & inTileZ <= 1.0D) {
                                dif = xYZ_dbl - terrainViewPos;
                                dist = dif.GetMagnitude ();
                                if (dist < bestDist) {
                                    bestDist = dist;
                                    bestPos = xYZ_dbl;
                                }
                            }

                            triHeightOffset = Convert.ToDouble (Map.Terrain.Vertices [x + 1, y + 1].Height * Map.HeightMultiplier);
                            triGradientX = Convert.ToDouble (Map.Terrain.Vertices [x, y + 1].Height * Map.HeightMultiplier - triHeightOffset);
                            triGradientZ = Convert.ToDouble (Map.Terrain.Vertices [x + 1, y].Height * Map.HeightMultiplier - triHeightOffset);
                            xYZ_dbl.Y = (triHeightOffset + triGradientX + triGradientZ +
                                (triGradientX * (tilePos.X - terrainViewPos.X) + triGradientZ * (tilePos.Y - terrainViewPos.Z) -
                                (triGradientX * terrainViewVector.X + triGradientZ * terrainViewVector.Z) * terrainViewPos.Y / terrainViewVector.Y) /
                                App.TerrainGridSpacing) /
                                (1.0D -
                                (triGradientX * terrainViewVector.X + triGradientZ * terrainViewVector.Z) / (terrainViewVector.Y * App.TerrainGridSpacing));
                            xYZ_dbl.X = terrainViewPos.X + terrainViewVector.X * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            xYZ_dbl.Z = terrainViewPos.Z + terrainViewVector.Z * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            inTileX = xYZ_dbl.X / App.TerrainGridSpacing - x;
                            inTileZ = xYZ_dbl.Z / App.TerrainGridSpacing - y;
                            if (inTileZ >= 1.0D - inTileX & inTileX >= 0.0D & inTileZ >= 0.0D & inTileX <= 1.0D & inTileZ <= 1.0D) {
                                dif = xYZ_dbl - terrainViewPos;
                                dist = dif.GetMagnitude ();
                                if (dist < bestDist) {
                                    bestDist = dist;
                                    bestPos = xYZ_dbl;
                                }
                            }
                        } else {
                            triHeightOffset = Convert.ToDouble (Map.Terrain.Vertices [x + 1, y].Height * Map.HeightMultiplier);
                            triGradientX = Convert.ToDouble (Map.Terrain.Vertices [x, y].Height * Map.HeightMultiplier - triHeightOffset);
                            triGradientZ = Convert.ToDouble (Map.Terrain.Vertices [x + 1, y + 1].Height * Map.HeightMultiplier - triHeightOffset);
                            xYZ_dbl.Y = (triHeightOffset + triGradientX +
                                (triGradientX * (tilePos.X - terrainViewPos.X) + triGradientZ * (terrainViewPos.Z - tilePos.Y) -
                                (triGradientX * terrainViewVector.X - triGradientZ * terrainViewVector.Z) * terrainViewPos.Y / terrainViewVector.Y) /
                                App.TerrainGridSpacing) /
                                (1.0D -
                                (triGradientX * terrainViewVector.X - triGradientZ * terrainViewVector.Z) / (terrainViewVector.Y * App.TerrainGridSpacing));
                            xYZ_dbl.X = terrainViewPos.X + terrainViewVector.X * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            xYZ_dbl.Z = terrainViewPos.Z + terrainViewVector.Z * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            inTileX = xYZ_dbl.X / App.TerrainGridSpacing - x;
                            inTileZ = xYZ_dbl.Z / App.TerrainGridSpacing - y;
                            if (inTileZ <= inTileX & inTileX >= 0.0D & inTileZ >= 0.0D & inTileX <= 1.0D & inTileZ <= 1.0D) {
                                dif = xYZ_dbl - terrainViewPos;
                                dist = dif.GetMagnitude ();
                                if (dist < bestDist) {
                                    bestDist = dist;
                                    bestPos = xYZ_dbl;
                                }
                            }

                            triHeightOffset = Convert.ToDouble (Map.Terrain.Vertices [x, y + 1].Height * Map.HeightMultiplier);
                            triGradientX = Convert.ToDouble (Map.Terrain.Vertices [x + 1, y + 1].Height * Map.HeightMultiplier - triHeightOffset);
                            triGradientZ = Convert.ToDouble (Map.Terrain.Vertices [x, y].Height * Map.HeightMultiplier - triHeightOffset);
                            xYZ_dbl.Y = (triHeightOffset + triGradientZ +
                                (triGradientX * (terrainViewPos.X - tilePos.X) + triGradientZ * (tilePos.Y - terrainViewPos.Z) +
                                (triGradientX * terrainViewVector.X - triGradientZ * terrainViewVector.Z) * terrainViewPos.Y / terrainViewVector.Y) /
                                App.TerrainGridSpacing) /
                                (1.0D +
                                (triGradientX * terrainViewVector.X - triGradientZ * terrainViewVector.Z) / (terrainViewVector.Y * App.TerrainGridSpacing));
                            xYZ_dbl.X = terrainViewPos.X + terrainViewVector.X * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            xYZ_dbl.Z = terrainViewPos.Z + terrainViewVector.Z * (terrainViewPos.Y - xYZ_dbl.Y) / terrainViewVector.Y;
                            inTileX = xYZ_dbl.X / App.TerrainGridSpacing - x;
                            inTileZ = xYZ_dbl.Z / App.TerrainGridSpacing - y;
                            if (inTileZ >= inTileX & inTileX >= 0.0D & inTileZ >= 0.0D & inTileX <= 1.0D & inTileZ <= 1.0D) {
                                dif = xYZ_dbl - terrainViewPos;
                                dist = dif.GetMagnitude ();
                                if (dist < bestDist) {
                                    bestDist = dist;
                                    bestPos = xYZ_dbl;
                                }
                            }
                        }
                    }
                }

                if (bestPos.X == double.NaN) {
                    return false;
                }

                resultPos.Horizontal.X = (int)bestPos.X;
                resultPos.Altitude = (int)bestPos.Y;
                resultPos.Horizontal.Y = (int)bestPos.Z;
            } catch {
                return false;
            }
            return true;
        }

        public bool ScreenXY_Get_ViewPlanePos_ForwardDownOnly (int screenX, int screenY, double planeHeight, ref Position.XY_dbl resultPos)
        {
            double dblTemp = 0;
            Position.XYZ_dbl xYZ_dbl = default(Position.XYZ_dbl);
            Position.XYZ_dbl xYZ_dbl2 = default(Position.XYZ_dbl);
            double dblTemp2 = 0;

            if (ViewPos.Y < planeHeight) {
                return false;
            }

            try {
                //convert screen pos to vector of one pos unit
                dblTemp2 = FOVMultiplier;
                xYZ_dbl.X = (screenX - MapViewControl.GLSize.X / 2.0D) * dblTemp2;
                xYZ_dbl.Y = (MapViewControl.GLSize.Y / 2.0D - screenY) * dblTemp2;
                xYZ_dbl.Z = 1.0D;
                //factor in the view angle
                Matrix3DMath.VectorRotationByMatrix (ViewAngleMatrix, xYZ_dbl, ref xYZ_dbl2);
                //get distance to cover the height
                if (xYZ_dbl2.Y > 0.0D) {
                    return false;
                }
                dblTemp = (planeHeight - ViewPos.Y) / xYZ_dbl2.Y;
                resultPos.X = ViewPos.X + xYZ_dbl2.X * dblTemp;
                resultPos.Y = ViewPos.Z + xYZ_dbl2.Z * dblTemp;
            } catch {
                return false;
            }
            return true;
        }

        public double Tiles_Per_Minimap_Pixel;

        public void MouseOver_Pos_Calc ()
        {
            Position.XY_dbl xY_dbl = default(Position.XY_dbl);
            bool flag = default(bool);
            sXY_int footprint = new sXY_int ();
            clsMouseDown.clsOverMinimap mouseLeftDownOverMinimap = GetMouseLeftDownOverMinimap ();

            if (mouseLeftDownOverMinimap != null) {
                if (MouseOver == null) {
                } else if (IsViewPosOverMinimap (MouseOver.ScreenPos)) {
                    sXY_int Pos = new sXY_int ((int)(MouseOver.ScreenPos.X * Tiles_Per_Minimap_Pixel),
                                              (int)((MouseOver.ScreenPos.Y * Tiles_Per_Minimap_Pixel)));
                    Map.TileNumClampToMap (Pos);
                    LookAtTile (Pos);
                }
            } else {
                clsMouseOver.clsOverTerrain mouseOverTerrain = new clsMouseOver.clsOverTerrain ();
                flag = false;
                if (SettingsManager.Settings.DirectPointer) {
                    if (ScreenXY_Get_TerrainPos (MouseOver.ScreenPos, ref mouseOverTerrain.Pos)) {
                        if (Map.PosIsOnMap (mouseOverTerrain.Pos.Horizontal)) {
                            flag = true;
                        }
                    }
                } else {
                    mouseOverTerrain.Pos.Altitude = (int)(255.0D / 2.0D * Map.HeightMultiplier);
                    if (ScreenXY_Get_ViewPlanePos (MouseOver.ScreenPos, mouseOverTerrain.Pos.Altitude, ref xY_dbl)) {
                        mouseOverTerrain.Pos.Horizontal.X = (int)xY_dbl.X;
                        mouseOverTerrain.Pos.Horizontal.Y = Convert.ToInt32 (- xY_dbl.Y);
                        if (Map.PosIsOnMap (mouseOverTerrain.Pos.Horizontal)) {
                            mouseOverTerrain.Pos.Altitude = (int)(Map.GetTerrainHeight (mouseOverTerrain.Pos.Horizontal));
                            flag = true;
                        }
                    }
                }
                if (flag) {
                    MouseOver.OverTerrain = mouseOverTerrain;
                    mouseOverTerrain.Tile.Normal.X = (int)((double)mouseOverTerrain.Pos.Horizontal.X / App.TerrainGridSpacing);
                    mouseOverTerrain.Tile.Normal.Y = (int)(((double)mouseOverTerrain.Pos.Horizontal.Y / App.TerrainGridSpacing));
                    mouseOverTerrain.Vertex.Normal.X = (int)(Math.Round (((double)mouseOverTerrain.Pos.Horizontal.X / App.TerrainGridSpacing)));
                    mouseOverTerrain.Vertex.Normal.Y = (int)(Math.Round (((double)mouseOverTerrain.Pos.Horizontal.Y / App.TerrainGridSpacing)));
                    mouseOverTerrain.Tile.Alignment = mouseOverTerrain.Vertex.Normal;
                    mouseOverTerrain.Vertex.Alignment = new sXY_int (mouseOverTerrain.Tile.Normal.X + 1, mouseOverTerrain.Tile.Normal.Y + 1);
                    mouseOverTerrain.Triangle = Map.GetTerrainTri (mouseOverTerrain.Pos.Horizontal);
                    xY_dbl.X = mouseOverTerrain.Pos.Horizontal.X - mouseOverTerrain.Vertex.Normal.X * App.TerrainGridSpacing;
                    xY_dbl.Y = mouseOverTerrain.Pos.Horizontal.Y - mouseOverTerrain.Vertex.Normal.Y * App.TerrainGridSpacing;
                    if (Math.Abs (xY_dbl.Y) <= Math.Abs (xY_dbl.X)) {
                        mouseOverTerrain.Side_IsV = false;
                        mouseOverTerrain.Side_Num.X = mouseOverTerrain.Tile.Normal.X;
                        mouseOverTerrain.Side_Num.Y = mouseOverTerrain.Vertex.Normal.Y;
                    } else {
                        mouseOverTerrain.Side_IsV = true;
                        mouseOverTerrain.Side_Num.X = mouseOverTerrain.Vertex.Normal.X;
                        mouseOverTerrain.Side_Num.Y = mouseOverTerrain.Tile.Normal.Y;
                    }
                    sXY_int sectorNum = Map.GetPosSectorNum (mouseOverTerrain.Pos.Horizontal);
                    clsUnit unit = default(clsUnit);
                    clsUnitSectorConnection connection = default(clsUnitSectorConnection);
                    foreach (clsUnitSectorConnection tempLoopVar_Connection in Map.Sectors[sectorNum.X, sectorNum.Y].Units) {
                        connection = tempLoopVar_Connection;
                        unit = connection.Unit;
                        xY_dbl.X = unit.Pos.Horizontal.X - mouseOverTerrain.Pos.Horizontal.X;
                        xY_dbl.Y = unit.Pos.Horizontal.Y - mouseOverTerrain.Pos.Horizontal.Y;
                        footprint = unit.TypeBase.get_GetFootprintSelected (unit.Rotation);
                        if (Math.Abs (xY_dbl.X) <= Math.Max (footprint.X / 2.0D, 0.5D) * App.TerrainGridSpacing
                            && Math.Abs (xY_dbl.Y) <= Math.Max (footprint.Y / 2.0D, 0.5D) * App.TerrainGridSpacing) {
                            mouseOverTerrain.Units.Add (unit);
                        }
                    }

                    if (MouseLeftDown != null) {
                        if (modTools.Tool == modTools.Tools.TerrainBrush) {
                            Apply_Terrain ();
                            if (Program.frmMainInstance.cbxAutoTexSetHeight.Checked) {
                                Apply_Height_Set (App.TerrainBrush, Program.frmMainInstance.HeightSetPalette [Program.frmMainInstance.tabHeightSetL.SelectedIndex]);
                            }
                        } else if (modTools.Tool == modTools.Tools.HeightSetBrush) {
                            Apply_Height_Set (App.HeightBrush, Program.frmMainInstance.HeightSetPalette [Program.frmMainInstance.tabHeightSetL.SelectedIndex]);
                        } else if (modTools.Tool == modTools.Tools.TextureBrush) {
                            Apply_Texture ();
                        } else if (modTools.Tool == modTools.Tools.CliffTriangle) {
                            Apply_CliffTriangle (false);
                        } else if (modTools.Tool == modTools.Tools.CliffBrush) {
                            Apply_Cliff ();
                        } else if (modTools.Tool == modTools.Tools.CliffRemove) {
                            Apply_Cliff_Remove ();
                        } else if (modTools.Tool == modTools.Tools.RoadPlace) {
                            Apply_Road ();
                        } else if (modTools.Tool == modTools.Tools.RoadRemove) {
                            Apply_Road_Remove ();
                        }
                    }
                    if (MouseRightDown != null) {
                        if (modTools.Tool == modTools.Tools.HeightSetBrush) {
                            if (MouseLeftDown == null) {
                                Apply_Height_Set (App.HeightBrush, Program.frmMainInstance.HeightSetPalette [Program.frmMainInstance.tabHeightSetR.SelectedIndex]);
                            }
                        } else if (modTools.Tool == modTools.Tools.CliffTriangle) {
                            Apply_CliffTriangle (true);
                        }
                    }
                }
            }
            MapViewControl.Pos_Display_Update ();
            MapViewControl.DrawViewLater ();
        }

        public clsMouseOver.clsOverTerrain GetMouseOverTerrain ()
        {
            if (MouseOver == null) {
                return null;
            } else {
                return MouseOver.OverTerrain;
            }
        }

        public clsMouseDown.clsOverTerrain GetMouseLeftDownOverTerrain ()
        {
            if (MouseLeftDown == null) {
                return null;
            } else {
                return MouseLeftDown.OverTerrain;
            }
        }

        public clsMouseDown.clsOverTerrain GetMouseRightDownOverTerrain ()
        {
            if (MouseRightDown == null) {
                return null;
            } else {
                return MouseRightDown.OverTerrain;
            }
        }

        public clsMouseDown.clsOverMinimap GetMouseLeftDownOverMinimap ()
        {
            if (MouseLeftDown == null) {
                return null;
            } else {
                return MouseLeftDown.OverMinimap;
            }
        }

        public clsMouseDown.clsOverMinimap GetMouseRightDownOverMinimap ()
        {
            if (MouseRightDown == null) {
                return null;
            } else {
                return MouseRightDown.OverMinimap;
            }
        }

        public class clsMouseOver
        {
            public sXY_int ScreenPos;

            public class clsOverTerrain
            {
                public sWorldPos Pos;
                public SimpleClassList<clsUnit> Units = new SimpleClassList<clsUnit> ();
                public clsBrush.sPosNum Tile;
                public clsBrush.sPosNum Vertex;
                public bool Triangle;
                public sXY_int Side_Num;
                public bool Side_IsV;
            }

            public clsOverTerrain OverTerrain;
        }

        public class clsMouseDown
        {
            public class clsOverTerrain
            {
                public sWorldPos DownPos;
            }

            public clsOverTerrain OverTerrain;

            public class clsOverMinimap
            {
                public sXY_int DownPos;
            }

            public clsOverMinimap OverMinimap;
        }

        public clsMouseOver MouseOver;
        public clsMouseDown MouseLeftDown;
        public clsMouseDown MouseRightDown;

        public bool IsViewPosOverMinimap (sXY_int pos)
        {
            if (pos.X >= 0 & pos.X < Map.Terrain.TileSize.X / Tiles_Per_Minimap_Pixel
                & pos.Y >= 0 & pos.Y < Map.Terrain.TileSize.Y / Tiles_Per_Minimap_Pixel) {
                return true;
            } else {
                return false;
            }
        }

        public void Apply_Terrain ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyVertexTerrain applyVertexTerrain = new clsApplyVertexTerrain ();
            applyVertexTerrain.Map = Map;
            applyVertexTerrain.VertexTerrain = App.SelectedTerrain;
            App.TerrainBrush.PerformActionMapVertices (applyVertexTerrain, mouseOverTerrain.Vertex);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Road ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            sXY_int side_Num = mouseOverTerrain.Side_Num;
            sXY_int tileNum = new sXY_int ();

            if (mouseOverTerrain.Side_IsV) {
                if (Map.Terrain.SideV [side_Num.X, side_Num.Y].Road != App.SelectedRoad) {
                    Map.Terrain.SideV [side_Num.X, side_Num.Y].Road = App.SelectedRoad;

                    if (side_Num.X > 0) {
                        tileNum.X = side_Num.X - 1;
                        tileNum.Y = side_Num.Y;
                        Map.AutoTextureChanges.TileChanged (tileNum);
                        Map.SectorGraphicsChanges.TileChanged (tileNum);
                        Map.SectorTerrainUndoChanges.TileChanged (tileNum);
                    }
                    if (side_Num.X < Map.Terrain.TileSize.X) {
                        tileNum = side_Num;
                        Map.AutoTextureChanges.TileChanged (tileNum);
                        Map.SectorGraphicsChanges.TileChanged (tileNum);
                        Map.SectorTerrainUndoChanges.TileChanged (tileNum);
                    }

                    Map.Update ();

                    Map.UndoStepCreate ("Road Side");

                    MapViewControl.DrawViewLater ();
                }
            } else {
                if (Map.Terrain.SideH [side_Num.X, side_Num.Y].Road != App.SelectedRoad) {
                    Map.Terrain.SideH [side_Num.X, side_Num.Y].Road = App.SelectedRoad;

                    if (side_Num.Y > 0) {
                        tileNum.X = side_Num.X;
                        tileNum.Y = side_Num.Y - 1;
                        Map.AutoTextureChanges.TileChanged (tileNum);
                        Map.SectorGraphicsChanges.TileChanged (tileNum);
                        Map.SectorTerrainUndoChanges.TileChanged (tileNum);
                    }
                    if (side_Num.Y < Map.Terrain.TileSize.X) {
                        tileNum = side_Num;
                        Map.AutoTextureChanges.TileChanged (tileNum);
                        Map.SectorGraphicsChanges.TileChanged (tileNum);
                        Map.SectorTerrainUndoChanges.TileChanged (tileNum);
                    }

                    Map.Update ();

                    Map.UndoStepCreate ("Road Side");

                    MapViewControl.DrawViewLater ();
                }
            }
        }

        public void Apply_Road_Line_Selection ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrian = GetMouseOverTerrain ();

            if (mouseOverTerrian == null) {
                return;
            }

            int num = 0;
            int a = 0;
            int b = 0;
            sXY_int Tile = mouseOverTerrian.Tile.Normal;
            sXY_int SideNum = new sXY_int ();

            if (Map.Selected_Tile_A != null) {
                if (Tile.X == Map.Selected_Tile_A.X) {
                    if (Tile.Y <= Map.Selected_Tile_A.Y) {
                        a = Tile.Y;
                        b = Map.Selected_Tile_A.Y;
                    } else {
                        a = Map.Selected_Tile_A.Y;
                        b = Tile.Y;
                    }
                    for (num = a + 1; num <= b; num++) {
                        Map.Terrain.SideH [Map.Selected_Tile_A.X, num].Road = App.SelectedRoad;
                        SideNum.X = Map.Selected_Tile_A.X;
                        SideNum.Y = num;
                        Map.AutoTextureChanges.SideHChanged (SideNum);
                        Map.SectorGraphicsChanges.SideHChanged (SideNum);
                        Map.SectorTerrainUndoChanges.SideHChanged (SideNum);
                    }

                    Map.Update ();

                    Map.UndoStepCreate ("Road Line");

                    Map.Selected_Tile_A = null;
                    MapViewControl.DrawViewLater ();
                } else if (Tile.Y == Map.Selected_Tile_A.Y) {
                    if (Tile.X <= Map.Selected_Tile_A.X) {
                        a = Tile.X;
                        b = Map.Selected_Tile_A.X;
                    } else {
                        a = Map.Selected_Tile_A.X;
                        b = Tile.X;
                    }
                    for (num = a + 1; num <= b; num++) {
                        Map.Terrain.SideV [num, Map.Selected_Tile_A.Y].Road = App.SelectedRoad;
                        SideNum.X = num;
                        SideNum.Y = Map.Selected_Tile_A.Y;
                        Map.AutoTextureChanges.SideVChanged (SideNum);
                        Map.SectorGraphicsChanges.SideVChanged (SideNum);
                        Map.SectorTerrainUndoChanges.SideVChanged (SideNum);
                    }

                    Map.Update ();

                    Map.UndoStepCreate ("Road Line");

                    Map.Selected_Tile_A = null;
                    MapViewControl.DrawViewLater ();
                } else {
                }
            } else {
                Map.Selected_Tile_A = new clsXY_int (Tile);
            }
        }

        public void Apply_Terrain_Fill (enumFillCliffAction CliffAction, bool Inside)
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            Terrain fillType = default(Terrain);
            Terrain replaceType = default(Terrain);
            sXY_int startVertex = mouseOverTerrain.Vertex.Normal;

            fillType = App.SelectedTerrain;
            replaceType = Map.Terrain.Vertices [startVertex.X, startVertex.Y].Terrain;
            if (fillType == replaceType) {
                return; //otherwise will cause endless loop
            }

            int A = 0;
            sXY_int[] sourceOfFill = new sXY_int[524289];
            int sourceOfFillCount = 0;
            int sourceOfFillNum = 0;
            int moveCount = 0;
            int remainingCount = 0;
            int moveOffset = 0;
            sXY_int currentSource = new sXY_int ();
            sXY_int nextSource = new sXY_int ();
            bool stopForCliff = default(bool);
            bool stopForEdge = default(bool);

            sourceOfFill [0] = startVertex;
            sourceOfFillCount = 1;
            sourceOfFillNum = 0;
            while (sourceOfFillNum < sourceOfFillCount) {
                currentSource = sourceOfFill [sourceOfFillNum];

                if (CliffAction == enumFillCliffAction.StopBefore) {
                    stopForCliff = Map.VertexIsCliffEdge (currentSource);
                } else {
                    stopForCliff = false;
                }
                stopForEdge = false;
                if (Inside) {
                    if (currentSource.X > 0) {
                        if (currentSource.Y > 0) {
                            if (Map.Terrain.Vertices [currentSource.X - 1, currentSource.Y - 1].Terrain != replaceType
                                && Map.Terrain.Vertices [currentSource.X - 1, currentSource.Y - 1].Terrain != fillType) {
                                stopForEdge = true;
                            }
                        }
                        if (Map.Terrain.Vertices [currentSource.X - 1, currentSource.Y].Terrain != replaceType
                            && Map.Terrain.Vertices [currentSource.X - 1, currentSource.Y].Terrain != fillType) {
                            stopForEdge = true;
                        }
                        if (currentSource.Y < Map.Terrain.TileSize.Y) {
                            if (Map.Terrain.Vertices [currentSource.X - 1, currentSource.Y + 1].Terrain != replaceType
                                && Map.Terrain.Vertices [currentSource.X - 1, currentSource.Y + 1].Terrain != fillType) {
                                stopForEdge = true;
                            }
                        }
                    }
                    if (currentSource.Y > 0) {
                        if (Map.Terrain.Vertices [currentSource.X, currentSource.Y - 1].Terrain != replaceType
                            && Map.Terrain.Vertices [currentSource.X, currentSource.Y - 1].Terrain != fillType) {
                            stopForEdge = true;
                        }
                    }
                    if (currentSource.X < Map.Terrain.TileSize.X) {
                        if (currentSource.Y > 0) {
                            if (Map.Terrain.Vertices [currentSource.X + 1, currentSource.Y - 1].Terrain != replaceType
                                && Map.Terrain.Vertices [currentSource.X + 1, currentSource.Y - 1].Terrain != fillType) {
                                stopForEdge = true;
                            }
                        }
                        if (Map.Terrain.Vertices [currentSource.X + 1, currentSource.Y].Terrain != replaceType
                            && Map.Terrain.Vertices [currentSource.X + 1, currentSource.Y].Terrain != fillType) {
                            stopForEdge = true;
                        }
                        if (currentSource.Y < Map.Terrain.TileSize.Y) {
                            if (Map.Terrain.Vertices [currentSource.X + 1, currentSource.Y + 1].Terrain != replaceType
                                && Map.Terrain.Vertices [currentSource.X + 1, currentSource.Y + 1].Terrain != fillType) {
                                stopForEdge = true;
                            }
                        }
                    }
                    if (currentSource.Y < Map.Terrain.TileSize.Y) {
                        if (Map.Terrain.Vertices [currentSource.X, currentSource.Y + 1].Terrain != replaceType
                            && Map.Terrain.Vertices [currentSource.X, currentSource.Y + 1].Terrain != fillType) {
                            stopForEdge = true;
                        }
                    }
                }

                if (!(stopForCliff || stopForEdge)) {
                    if (Map.Terrain.Vertices [currentSource.X, currentSource.Y].Terrain == replaceType) {
                        Map.Terrain.Vertices [currentSource.X, currentSource.Y].Terrain = fillType;
                        Map.SectorGraphicsChanges.VertexChanged (currentSource);
                        Map.SectorTerrainUndoChanges.VertexChanged (currentSource);
                        Map.AutoTextureChanges.VertexChanged (currentSource);

                        nextSource.X = currentSource.X + 1;
                        nextSource.Y = currentSource.Y;
                        if (nextSource.X >= 0 & nextSource.X <= Map.Terrain.TileSize.X
                            & nextSource.Y >= 0 & nextSource.Y <= Map.Terrain.TileSize.Y) {
                            if (CliffAction == enumFillCliffAction.StopAfter) {
                                stopForCliff = Map.SideHIsCliffOnBothSides (new sXY_int (currentSource.X, currentSource.Y));
                            } else {
                                stopForCliff = false;
                            }
                            if (!stopForCliff) {
                                if (Map.Terrain.Vertices [nextSource.X, nextSource.Y].Terrain == replaceType) {
                                    if (sourceOfFill.GetUpperBound (0) < sourceOfFillCount) {
                                        Array.Resize (ref sourceOfFill, sourceOfFillCount * 2 + 1 + 1);
                                    }
                                    sourceOfFill [sourceOfFillCount] = nextSource;
                                    sourceOfFillCount++;
                                }
                            }
                        }

                        nextSource.X = currentSource.X - 1;
                        nextSource.Y = currentSource.Y;
                        if (nextSource.X >= 0 & nextSource.X <= Map.Terrain.TileSize.X
                            & nextSource.Y >= 0 & nextSource.Y <= Map.Terrain.TileSize.Y) {
                            if (CliffAction == enumFillCliffAction.StopAfter) {
                                stopForCliff = Map.SideHIsCliffOnBothSides (new sXY_int (currentSource.X - 1, currentSource.Y));
                            } else {
                                stopForCliff = false;
                            }
                            if (!stopForCliff) {
                                if (Map.Terrain.Vertices [nextSource.X, nextSource.Y].Terrain == replaceType) {
                                    if (sourceOfFill.GetUpperBound (0) < sourceOfFillCount) {
                                        Array.Resize (ref sourceOfFill, sourceOfFillCount * 2 + 1 + 1);
                                    }
                                    sourceOfFill [sourceOfFillCount] = nextSource;
                                    sourceOfFillCount++;
                                }
                            }
                        }

                        nextSource.X = currentSource.X;
                        nextSource.Y = currentSource.Y + 1;
                        if (nextSource.X >= 0 & nextSource.X <= Map.Terrain.TileSize.X
                            & nextSource.Y >= 0 & nextSource.Y <= Map.Terrain.TileSize.Y) {
                            if (CliffAction == enumFillCliffAction.StopAfter) {
                                stopForCliff = Map.SideVIsCliffOnBothSides (new sXY_int (currentSource.X, currentSource.Y));
                            } else {
                                stopForCliff = false;
                            }
                            if (!stopForCliff) {
                                if (Map.Terrain.Vertices [nextSource.X, nextSource.Y].Terrain == replaceType) {
                                    if (sourceOfFill.GetUpperBound (0) < sourceOfFillCount) {
                                        Array.Resize (ref sourceOfFill, sourceOfFillCount * 2 + 1 + 1);
                                    }
                                    sourceOfFill [sourceOfFillCount] = nextSource;
                                    sourceOfFillCount++;
                                }
                            }
                        }

                        nextSource.X = currentSource.X;
                        nextSource.Y = currentSource.Y - 1;
                        if (nextSource.X >= 0 & nextSource.X <= Map.Terrain.TileSize.X
                            & nextSource.Y >= 0 & nextSource.Y <= Map.Terrain.TileSize.Y) {
                            if (CliffAction == enumFillCliffAction.StopAfter) {
                                stopForCliff = Map.SideVIsCliffOnBothSides (new sXY_int (currentSource.X, currentSource.Y - 1));
                            } else {
                                stopForCliff = false;
                            }
                            if (!stopForCliff) {
                                if (Map.Terrain.Vertices [nextSource.X, nextSource.Y].Terrain == replaceType) {
                                    if (sourceOfFill.GetUpperBound (0) < sourceOfFillCount) {
                                        Array.Resize (ref sourceOfFill, sourceOfFillCount * 2 + 1 + 1);
                                    }
                                    sourceOfFill [sourceOfFillCount] = nextSource;
                                    sourceOfFillCount++;
                                }
                            }
                        }
                    }
                }

                sourceOfFillNum++;

                if (sourceOfFillNum >= 131072) {
                    remainingCount = sourceOfFillCount - sourceOfFillNum;
                    moveCount = Math.Min (sourceOfFillNum, remainingCount);
                    moveOffset = sourceOfFillCount - moveCount;
                    for (A = 0; A <= moveCount - 1; A++) {
                        sourceOfFill [A] = sourceOfFill [moveOffset + A];
                    }
                    sourceOfFillCount -= sourceOfFillNum;
                    sourceOfFillNum = 0;
                    if (sourceOfFillCount * 3 < sourceOfFill.GetUpperBound (0) + 1) {
                        Array.Resize (ref sourceOfFill, sourceOfFillCount * 2 + 1 + 1);
                    }
                }
            }

            Map.Update ();

            Map.UndoStepCreate ("Ground Fill");

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Texture ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyTexture applyTexture = new clsApplyTexture ();
            applyTexture.Map = Map;
            applyTexture.TextureNum = App.SelectedTextureNum;
            applyTexture.SetTexture = Program.frmMainInstance.chkSetTexture.Checked;
            applyTexture.Orientation = App.TextureOrientation;
            applyTexture.RandomOrientation = Program.frmMainInstance.chkTextureOrientationRandomize.Checked;
            applyTexture.SetOrientation = Program.frmMainInstance.chkSetTextureOrientation.Checked;
            applyTexture.TerrainAction = Program.frmMainInstance.TextureTerrainAction;
            App.TextureBrush.PerformActionMapTiles (applyTexture, mouseOverTerrain.Tile);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_CliffTriangle (bool remove)
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            if (remove) {
                clsApplyCliffTriangleRemove ApplyCliffTriangleRemove = new clsApplyCliffTriangleRemove ();
                ApplyCliffTriangleRemove.Map = Map;
                ApplyCliffTriangleRemove.PosNum = mouseOverTerrain.Tile.Normal;
                ApplyCliffTriangleRemove.Triangle = mouseOverTerrain.Triangle;
                ApplyCliffTriangleRemove.ActionPerform ();
            } else {
                clsApplyCliffTriangle ApplyCliffTriangle = new clsApplyCliffTriangle ();
                ApplyCliffTriangle.Map = Map;
                ApplyCliffTriangle.PosNum = mouseOverTerrain.Tile.Normal;
                ApplyCliffTriangle.Triangle = mouseOverTerrain.Triangle;
                ApplyCliffTriangle.ActionPerform ();
            }

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Cliff ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyCliff applyCliff = new clsApplyCliff ();
            applyCliff.Map = Map;
            double angle = 0;
            if (!IOUtil.InvariantParse (Program.frmMainInstance.txtAutoCliffSlope.Text, ref angle)) {
                return;
            }
            applyCliff.Angle = MathUtil.Clamp_dbl (angle * MathUtil.RadOf1Deg, 0.0D, MathUtil.RadOf90Deg);
            applyCliff.SetTris = Program.frmMainInstance.cbxCliffTris.Checked;
            App.CliffBrush.PerformActionMapTiles (applyCliff, mouseOverTerrain.Tile);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Cliff_Remove ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyCliffRemove applyCliffRemove = new clsApplyCliffRemove ();
            applyCliffRemove.Map = Map;
            App.CliffBrush.PerformActionMapTiles (applyCliffRemove, mouseOverTerrain.Tile);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Road_Remove ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyRoadRemove applyRoadRemove = new clsApplyRoadRemove ();
            applyRoadRemove.Map = Map;
            App.CliffBrush.PerformActionMapTiles (applyRoadRemove, mouseOverTerrain.Tile);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Texture_Clockwise ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            sXY_int Tile = mouseOverTerrain.Tile.Normal;

            Map.Terrain.Tiles [Tile.X, Tile.Y].Texture.Orientation.RotateClockwise ();
            Map.TileTextureChangeTerrainAction (Tile, Program.frmMainInstance.TextureTerrainAction);

            Map.SectorGraphicsChanges.TileChanged (Tile);
            Map.SectorTerrainUndoChanges.TileChanged (Tile);

            Map.Update ();

            Map.UndoStepCreate ("Texture Rotate");

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Texture_CounterClockwise ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            sXY_int tile = mouseOverTerrain.Tile.Normal;

            Map.Terrain.Tiles [tile.X, tile.Y].Texture.Orientation.RotateAntiClockwise ();
            Map.TileTextureChangeTerrainAction (tile, Program.frmMainInstance.TextureTerrainAction);

            Map.SectorGraphicsChanges.TileChanged (tile);
            Map.SectorTerrainUndoChanges.TileChanged (tile);

            Map.Update ();

            Map.UndoStepCreate ("Texture Rotate");

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Texture_FlipX ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            sXY_int tile = mouseOverTerrain.Tile.Normal;

            Map.Terrain.Tiles [tile.X, tile.Y].Texture.Orientation.ResultXFlip = !Map.Terrain.Tiles [tile.X, tile.Y].Texture.Orientation.ResultXFlip;
            Map.TileTextureChangeTerrainAction (tile, Program.frmMainInstance.TextureTerrainAction);

            Map.SectorGraphicsChanges.TileChanged (tile);
            Map.SectorTerrainUndoChanges.TileChanged (tile);

            Map.Update ();

            Map.UndoStepCreate ("Texture Rotate");

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Tri_Flip ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            sXY_int tile = mouseOverTerrain.Tile.Normal;

            Map.Terrain.Tiles [tile.X, tile.Y].Tri = !Map.Terrain.Tiles [tile.X, tile.Y].Tri;

            Map.SectorGraphicsChanges.TileChanged (tile);
            Map.SectorTerrainUndoChanges.TileChanged (tile);

            Map.Update ();

            Map.UndoStepCreate ("Triangle Flip");

            MapViewControl.DrawViewLater ();
        }

        public void Apply_HeightSmoothing (double ratio)
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyHeightSmoothing applyHeightSmoothing = new clsApplyHeightSmoothing ();
            applyHeightSmoothing.Map = Map;
            applyHeightSmoothing.Ratio = ratio;
            int radius = (int)(Math.Ceiling (App.HeightBrush.Radius));
            sXY_int posNum = App.HeightBrush.GetPosNum (mouseOverTerrain.Vertex);
            applyHeightSmoothing.Offset.X = MathUtil.Clamp_int (posNum.X - radius, 0, Map.Terrain.TileSize.X);
            applyHeightSmoothing.Offset.Y = MathUtil.Clamp_int (posNum.Y - radius, 0, Map.Terrain.TileSize.Y);
            sXY_int posEnd = new sXY_int ();
            posEnd.X = MathUtil.Clamp_int (posNum.X + radius, 0, Map.Terrain.TileSize.X);
            posEnd.Y = MathUtil.Clamp_int (posNum.Y + radius, 0, Map.Terrain.TileSize.Y);
            applyHeightSmoothing.AreaTileSize.X = posEnd.X - applyHeightSmoothing.Offset.X;
            applyHeightSmoothing.AreaTileSize.Y = posEnd.Y - applyHeightSmoothing.Offset.Y;
            applyHeightSmoothing.Start ();
            App.HeightBrush.PerformActionMapVertices (applyHeightSmoothing, mouseOverTerrain.Vertex);
            applyHeightSmoothing.Finish ();

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Height_Change (double rate)
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyHeightChange applyHeightChange = new clsApplyHeightChange ();
            applyHeightChange.Map = Map;
            applyHeightChange.Rate = rate;
            applyHeightChange.UseEffect = Program.frmMainInstance.cbxHeightChangeFade.Checked;
            App.HeightBrush.PerformActionMapVertices (applyHeightChange, mouseOverTerrain.Vertex);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Height_Set (clsBrush brush, byte height)
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            clsApplyHeightSet applyHeightSet = new clsApplyHeightSet ();
            applyHeightSet.Map = Map;
            applyHeightSet.Height = height;
            brush.PerformActionMapVertices (applyHeightSet, mouseOverTerrain.Vertex);

            Map.Update ();

            MapViewControl.DrawViewLater ();
        }

        public void Apply_Gateway ()
        {
            clsMouseOver.clsOverTerrain mouseOverTerrain = GetMouseOverTerrain ();

            if (mouseOverTerrain == null) {
                return;
            }

            sXY_int tile = mouseOverTerrain.Tile.Normal;

            if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Gateway_Delete)) {
                int a = 0;
                sXY_int low = new sXY_int ();
                sXY_int high = new sXY_int ();
                a = 0;
                while (a < Map.Gateways.Count) {
                    MathUtil.ReorderXY (Map.Gateways [a].PosA, Map.Gateways [a].PosB, ref low, ref high);
                    if (low.X <= tile.X
                        & high.X >= tile.X
                        & low.Y <= tile.Y
                        & high.Y >= tile.Y) {
                        Map.GatewayRemoveStoreChange (a);
                        Map.UndoStepCreate ("Gateway Delete");
                        Map.MinimapMakeLater ();
                        MapViewControl.DrawViewLater ();
                        break;
                    }
                    a++;
                }
            } else {
                if (Map.Selected_Tile_A == null) {
                    Map.Selected_Tile_A = new clsXY_int (tile);
                    MapViewControl.DrawViewLater ();
                } else if (tile.X == Map.Selected_Tile_A.X | tile.Y == Map.Selected_Tile_A.Y) {
                    if (Map.GatewayCreateStoreChange (Map.Selected_Tile_A.XY, tile) != null) {
                        Map.UndoStepCreate ("Gateway Place");
                        Map.Selected_Tile_A = null;
                        Map.Selected_Tile_B = null;
                        Map.MinimapMakeLater ();
                        MapViewControl.DrawViewLater ();
                    }
                }
            }
        }

        public void MouseDown (MouseEventArgs e)
        {
            sXY_int screenPos = new sXY_int ();

            Map.SuppressMinimap = true;

            screenPos.X = e.X;
            screenPos.Y = e.Y;
            if (e.Button == MouseButtons.Left) {
                MouseLeftDown = new clsMouseDown ();
                if (IsViewPosOverMinimap (screenPos)) {
                    MouseLeftDown.OverMinimap = new clsMouseDown.clsOverMinimap ();
                    MouseLeftDown.OverMinimap.DownPos = screenPos;
                    sXY_int Pos = new sXY_int ((int)((screenPos.X * Tiles_Per_Minimap_Pixel)),
                                              (int)(screenPos.Y * Tiles_Per_Minimap_Pixel));
                    Map.TileNumClampToMap (Pos);
                    LookAtTile (Pos);
                } else {
                    clsMouseOver.clsOverTerrain MouseOverTerrain = GetMouseOverTerrain ();
                    if (MouseOverTerrain != null) {
                        MouseLeftDown.OverTerrain = new clsMouseDown.clsOverTerrain ();
                        MouseLeftDown.OverTerrain.DownPos = MouseOverTerrain.Pos;
                        if (modTools.Tool == modTools.Tools.ObjectSelect) {
                            if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Picker)) {
                                if (MouseOverTerrain.Units.Count > 0) {
                                    if (MouseOverTerrain.Units.Count == 1) {
                                        Program.frmMainInstance.ObjectPicker (MouseOverTerrain.Units [0].TypeBase);
                                    } else {
                                        MapViewControl.ListSelectBegin (true);
                                    }
                                }
                            } else if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ScriptPosition)) {
                                clsScriptPosition NewPosition = new clsScriptPosition (Map);
                                if (NewPosition != null) {
                                    NewPosition.PosX = MouseLeftDown.OverTerrain.DownPos.Horizontal.X;
                                    NewPosition.PosY = MouseLeftDown.OverTerrain.DownPos.Horizontal.Y;
                                    Program.frmMainInstance.ScriptMarkerLists_Update ();
                                }
                            } else {
                                if (!KeyboardManager.KeyboardProfile.Active (KeyboardManager.UnitMultiselect)) {
                                    Map.SelectedUnits.Clear ();
                                }
                                Program.frmMainInstance.SelectedObject_Changed ();
                                Map.Unit_Selected_Area_VertexA = new clsXY_int (MouseOverTerrain.Vertex.Normal);
                                MapViewControl.DrawViewLater ();
                            }
                        } else if (modTools.Tool == modTools.Tools.TerrainBrush) {
                            if (Map.Tileset != null) {
                                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Picker)) {
                                    Program.frmMainInstance.TerrainPicker ();
                                } else {
                                    Apply_Terrain ();
                                    if (Program.frmMainInstance.cbxAutoTexSetHeight.Checked) {
                                        Apply_Height_Set (App.TerrainBrush,
                                                         Program.frmMainInstance.HeightSetPalette [Program.frmMainInstance.tabHeightSetL.SelectedIndex]);
                                    }
                                }
                            }
                        } else if (modTools.Tool == modTools.Tools.HeightSetBrush) {
                            if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Picker)) {
                                Program.frmMainInstance.HeightPickerL ();
                            } else {
                                Apply_Height_Set (App.HeightBrush, Program.frmMainInstance.HeightSetPalette [Program.frmMainInstance.tabHeightSetL.SelectedIndex]);
                            }
                        } else if (modTools.Tool == modTools.Tools.TextureBrush) {
                            if (Map.Tileset != null) {
                                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Picker)) {
                                    Program.frmMainInstance.TexturePicker ();
                                } else {
                                    Apply_Texture ();
                                }
                            }
                        } else if (modTools.Tool == modTools.Tools.CliffTriangle) {
                            Apply_CliffTriangle (false);
                        } else if (modTools.Tool == modTools.Tools.CliffBrush) {
                            Apply_Cliff ();
                        } else if (modTools.Tool == modTools.Tools.CliffRemove) {
                            Apply_Cliff_Remove ();
                        } else if (modTools.Tool == modTools.Tools.TerrainFill) {
                            if (Map.Tileset != null) {
                                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Picker)) {
                                    Program.frmMainInstance.TerrainPicker ();
                                } else {
                                    Apply_Terrain_Fill (Program.frmMainInstance.FillCliffAction, Program.frmMainInstance.cbxFillInside.Checked);
                                    MapViewControl.DrawViewLater ();
                                }
                            }
                        } else if (modTools.Tool == modTools.Tools.RoadPlace) {
                            if (Map.Tileset != null) {
                                Apply_Road ();
                            }
                        } else if (modTools.Tool == modTools.Tools.RoadLines) {
                            if (Map.Tileset != null) {
                                Apply_Road_Line_Selection ();
                            }
                        } else if (modTools.Tool == modTools.Tools.RoadRemove) {
                            Apply_Road_Remove ();
                        } else if (modTools.Tool == modTools.Tools.ObjectPlace) {
                            if (Program.frmMainInstance.SingleSelectedObjectTypeBase != null && Map.SelectedUnitGroup != null) {
                                clsUnitCreate objectCreator = new clsUnitCreate ();
                                Map.SetObjectCreatorDefaults (objectCreator);
                                objectCreator.Horizontal = MouseOverTerrain.Pos.Horizontal;
                                objectCreator.Perform ();
                                Map.UndoStepCreate ("Place Object");
                                Map.Update ();
                                Map.MinimapMakeLater ();
                                MapViewControl.DrawViewLater ();
                            }
                        } else if (modTools.Tool == modTools.Tools.ObjectLines) {
                            ApplyObjectLine ();
                        } else if (modTools.Tool == modTools.Tools.TerrainSelect) {
                            if (Map.Selected_Area_VertexA == null) {
                                Map.Selected_Area_VertexA = new clsXY_int (MouseOverTerrain.Vertex.Normal);
                                MapViewControl.DrawViewLater ();
                            } else if (Map.Selected_Area_VertexB == null) {
                                Map.Selected_Area_VertexB = new clsXY_int (MouseOverTerrain.Vertex.Normal);
                                MapViewControl.DrawViewLater ();
                            } else {
                                Map.Selected_Area_VertexA = null;
                                Map.Selected_Area_VertexB = null;
                                MapViewControl.DrawViewLater ();
                            }
                        } else if (modTools.Tool == modTools.Tools.Gateways) {
                            Apply_Gateway ();
                        }
                    } else if (modTools.Tool == modTools.Tools.ObjectSelect) {
                        Map.SelectedUnits.Clear ();
                        Program.frmMainInstance.SelectedObject_Changed ();
                    }
                }
            } else if (e.Button == MouseButtons.Right) {
                MouseRightDown = new clsMouseDown ();
                if (IsViewPosOverMinimap (screenPos)) {
                    MouseRightDown.OverMinimap = new clsMouseDown.clsOverMinimap ();
                    MouseRightDown.OverMinimap.DownPos = screenPos;
                } else {
                    clsMouseOver.clsOverTerrain MouseOverTerrain = GetMouseOverTerrain ();
                    if (MouseOverTerrain != null) {
                        MouseRightDown.OverTerrain = new clsMouseDown.clsOverTerrain ();
                        MouseRightDown.OverTerrain.DownPos = MouseOverTerrain.Pos;
                    }
                }
                if (modTools.Tool == modTools.Tools.RoadLines || modTools.Tool == modTools.Tools.ObjectLines) {
                    Map.Selected_Tile_A = null;
                    MapViewControl.DrawViewLater ();
                } else if (modTools.Tool == modTools.Tools.TerrainSelect) {
                    Map.Selected_Area_VertexA = null;
                    Map.Selected_Area_VertexB = null;
                    MapViewControl.DrawViewLater ();
                } else if (modTools.Tool == modTools.Tools.CliffTriangle) {
                    Apply_CliffTriangle (true);
                } else if (modTools.Tool == modTools.Tools.Gateways) {
                    Map.Selected_Tile_A = null;
                    Map.Selected_Tile_B = null;
                    MapViewControl.DrawViewLater ();
                } else if (modTools.Tool == modTools.Tools.HeightSetBrush) {
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.Picker)) {
                        Program.frmMainInstance.HeightPickerR ();
                    } else {
                        Apply_Height_Set (App.HeightBrush, Program.frmMainInstance.HeightSetPalette [Program.frmMainInstance.tabHeightSetR.SelectedIndex]);
                    }
                }
            }
        }

        public void TimedActions (double Zoom, double Move, double Pan, double Roll, double OrbitRate)
        {
            Position.XYZ_dbl XYZ_dbl = default(Position.XYZ_dbl);
            double PanRate = Pan * FieldOfViewY;
            Angles.AnglePY AnglePY = default(Angles.AnglePY);
            Matrix3DMath.Matrix3D matrixA = new Matrix3DMath.Matrix3D ();
            Matrix3DMath.Matrix3D matrixB = new Matrix3DMath.Matrix3D ();
            Position.XYZ_dbl ViewAngleChange = default(Position.XYZ_dbl);
            sXYZ_int ViewPosChangeXYZ = new sXYZ_int ();
            bool AngleChanged = default(bool);

            Move *= FOVMultiplier * (MapViewControl.GLSize.X + MapViewControl.GLSize.Y) * Math.Max (Math.Abs (ViewPos.Y), 512.0D);

            if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewZoomIn)) {
                FOV_Scale_2E_Change (Convert.ToDouble (- Zoom));
            }
            if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewZoomOut)) {
                FOV_Scale_2E_Change (Zoom);
            }

            if (App.ViewMoveType == enumView_Move_Type.Free) {
                ViewPosChangeXYZ.X = 0;
                ViewPosChangeXYZ.Y = 0;
                ViewPosChangeXYZ.Z = 0;
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveForward)) {
                    Matrix3DMath.VectorForwardsRotationByMatrix (ViewAngleMatrix, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveBackward)) {
                    Matrix3DMath.VectorBackwardsRotationByMatrix (ViewAngleMatrix, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveLeft)) {
                    Matrix3DMath.VectorLeftRotationByMatrix (ViewAngleMatrix, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveRight)) {
                    Matrix3DMath.VectorRightRotationByMatrix (ViewAngleMatrix, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveUp)) {
                    Matrix3DMath.VectorUpRotationByMatrix (ViewAngleMatrix, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveDown)) {
                    Matrix3DMath.VectorDownRotationByMatrix (ViewAngleMatrix, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }

                ViewAngleChange.X = 0.0D;
                ViewAngleChange.Y = 0.0D;
                ViewAngleChange.Z = 0.0D;
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewLeft)) {
                    Matrix3DMath.VectorForwardsRotationByMatrix (ViewAngleMatrix, Roll, ref XYZ_dbl);
                    ViewAngleChange += XYZ_dbl;
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewRight)) {
                    Matrix3DMath.VectorBackwardsRotationByMatrix (ViewAngleMatrix, Roll, ref XYZ_dbl);
                    ViewAngleChange += XYZ_dbl;
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewBackward)) {
                    Matrix3DMath.VectorLeftRotationByMatrix (ViewAngleMatrix, PanRate, ref XYZ_dbl);
                    ViewAngleChange += XYZ_dbl;
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewForward)) {
                    Matrix3DMath.VectorRightRotationByMatrix (ViewAngleMatrix, PanRate, ref XYZ_dbl);
                    ViewAngleChange += XYZ_dbl;
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewRollLeft)) {
                    Matrix3DMath.VectorDownRotationByMatrix (ViewAngleMatrix, PanRate, ref XYZ_dbl);
                    ViewAngleChange += XYZ_dbl;
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewRollRight)) {
                    Matrix3DMath.VectorUpRotationByMatrix (ViewAngleMatrix, PanRate, ref XYZ_dbl);
                    ViewAngleChange += XYZ_dbl;
                }

                if (ViewPosChangeXYZ.X != 0.0D | ViewPosChangeXYZ.Y != 0.0D | ViewPosChangeXYZ.Z != 0.0D) {
                    ViewPosChange (ViewPosChangeXYZ);
                }
                //do rotation
                if (ViewAngleChange.X != 0.0D | ViewAngleChange.Y != 0.0D | ViewAngleChange.Z != 0.0D) {
                    Matrix3DMath.VectorToPY (ViewAngleChange, ref AnglePY);
                    Matrix3DMath.MatrixSetToPY (matrixA, AnglePY);
                    Matrix3DMath.MatrixRotationAroundAxis (ViewAngleMatrix, matrixA, ViewAngleChange.GetMagnitude (), matrixB);
                    ViewAngleSet_Rotate (matrixB);
                }
            } else if (App.ViewMoveType == enumView_Move_Type.RTS) {
                ViewPosChangeXYZ = new sXYZ_int ();

                Matrix3DMath.MatrixToPY (ViewAngleMatrix, ref AnglePY);
                Matrix3DMath.MatrixSetToYAngle (matrixA, AnglePY.Yaw);
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveForward)) {
                    Matrix3DMath.VectorForwardsRotationByMatrix (matrixA, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveBackward)) {
                    Matrix3DMath.VectorBackwardsRotationByMatrix (matrixA, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveLeft)) {
                    Matrix3DMath.VectorLeftRotationByMatrix (matrixA, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveRight)) {
                    Matrix3DMath.VectorRightRotationByMatrix (matrixA, Move, ref XYZ_dbl);
                    ViewPosChangeXYZ.Add_dbl (XYZ_dbl);
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveUp)) {
                    ViewPosChangeXYZ.Y += (int)Move;
                }
                if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewMoveDown)) {
                    ViewPosChangeXYZ.Y -= (int)Move;
                }

                AngleChanged = false;

                if (App.RTSOrbit) {
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewForward)) {
                        AnglePY.Pitch = MathUtil.Clamp_dbl (AnglePY.Pitch + OrbitRate, Convert.ToDouble (- MathUtil.RadOf90Deg + 0.03125D * MathUtil.RadOf1Deg),
                                                           MathUtil.RadOf90Deg - 0.03125D * MathUtil.RadOf1Deg);
                        AngleChanged = true;
                    }
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewBackward)) {
                        AnglePY.Pitch = MathUtil.Clamp_dbl (AnglePY.Pitch - OrbitRate, Convert.ToDouble (- MathUtil.RadOf90Deg + 0.03125D * MathUtil.RadOf1Deg),
                                                           MathUtil.RadOf90Deg - 0.03125D * MathUtil.RadOf1Deg);
                        AngleChanged = true;
                    }
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewLeft)) {
                        AnglePY.Yaw = MathUtil.AngleClamp (AnglePY.Yaw + OrbitRate);
                        AngleChanged = true;
                    }
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewRight)) {
                        AnglePY.Yaw = MathUtil.AngleClamp (AnglePY.Yaw - OrbitRate);
                        AngleChanged = true;
                    }
                } else {
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewForward)) {
                        AnglePY.Pitch = MathUtil.Clamp_dbl (AnglePY.Pitch - OrbitRate, Convert.ToDouble (- MathUtil.RadOf90Deg + 0.03125D * MathUtil.RadOf1Deg),
                                                           MathUtil.RadOf90Deg - 0.03125D * MathUtil.RadOf1Deg);
                        AngleChanged = true;
                    }
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewBackward)) {
                        AnglePY.Pitch = MathUtil.Clamp_dbl (AnglePY.Pitch + OrbitRate, Convert.ToDouble (- MathUtil.RadOf90Deg + 0.03125D * MathUtil.RadOf1Deg),
                                                           MathUtil.RadOf90Deg - 0.03125D * MathUtil.RadOf1Deg);
                        AngleChanged = true;
                    }
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewLeft)) {
                        AnglePY.Yaw = MathUtil.AngleClamp (AnglePY.Yaw - OrbitRate);
                        AngleChanged = true;
                    }
                    if (KeyboardManager.KeyboardProfile.Active (KeyboardManager.ViewRight)) {
                        AnglePY.Yaw = MathUtil.AngleClamp (AnglePY.Yaw + OrbitRate);
                        AngleChanged = true;
                    }
                }

                //Dim HeightChange As Double
                //HeightChange = Map.Terrain_Height_Get(view.View_Pos.X + ViewPosChange.X, view.View_Pos.Z + ViewPosChange.Z) - Map.Terrain_Height_Get(view.View_Pos.X, view.View_Pos.Z)

                //ViewPosChange.Y = ViewPosChange.Y + HeightChange

                if (ViewPosChangeXYZ.X != 0.0D | ViewPosChangeXYZ.Y != 0.0D | ViewPosChangeXYZ.Z != 0.0D) {
                    ViewPosChange (ViewPosChangeXYZ);
                }
                if (AngleChanged) {
                    Matrix3DMath.MatrixSetToPY (matrixA, AnglePY);
                    ViewAngleSet_Rotate (matrixA);
                }
            }
        }

        public void TimedTools ()
        {
            if (modTools.Tool == modTools.Tools.HeightSmoothBrush) {
                if (GetMouseOverTerrain () != null) {
                    if (GetMouseLeftDownOverTerrain () != null) {
                        double dblTemp = 0;
                        if (!IOUtil.InvariantParse (Program.frmMainInstance.txtSmoothRate.Text, ref dblTemp)) {
                            return;
                        }
                        Apply_HeightSmoothing (MathUtil.Clamp_dbl (dblTemp * Program.frmMainInstance.tmrTool.Interval / 1000.0D, 0.0D, 1.0D));
                    }
                }
            } else if (modTools.Tool == modTools.Tools.HeightChangeBrush) {
                if (GetMouseOverTerrain () != null) {
                    double dblTemp = 0;
                    if (!IOUtil.InvariantParse (Program.frmMainInstance.txtHeightChangeRate.Text, ref dblTemp)) {
                        return;
                    }
                    if (GetMouseLeftDownOverTerrain () != null) {
                        Apply_Height_Change (MathUtil.Clamp_dbl (dblTemp, -255.0D, 255.0D));
                    } else if (GetMouseRightDownOverTerrain () != null) {
                        Apply_Height_Change (MathUtil.Clamp_dbl (Convert.ToDouble (- dblTemp), -255.0D, 255.0D));
                    }
                }
            }
        }

        public void ApplyObjectLine ()
        {
            if (Program.frmMainInstance.SingleSelectedObjectTypeBase != null && Map.SelectedUnitGroup != null) {
                clsMouseOver.clsOverTerrain mouseOverTerrian = GetMouseOverTerrain ();

                if (mouseOverTerrian == null) {
                    return;
                }

                int num = 0;
                int a = 0;
                int b = 0;
                sXY_int tile = mouseOverTerrian.Tile.Normal;

                if (Map.Selected_Tile_A != null) {
                    if (tile.X == Map.Selected_Tile_A.X) {
                        if (tile.Y <= Map.Selected_Tile_A.Y) {
                            a = tile.Y;
                            b = Map.Selected_Tile_A.Y;
                        } else {
                            a = Map.Selected_Tile_A.Y;
                            b = tile.Y;
                        }
                        clsUnitCreate objectCreator = new clsUnitCreate ();
                        Map.SetObjectCreatorDefaults (objectCreator);
                        for (num = a; num <= b; num++) {
                            objectCreator.Horizontal.X = (int)((tile.X + 0.5D) * App.TerrainGridSpacing);
                            objectCreator.Horizontal.Y = (int)((num + 0.5D) * App.TerrainGridSpacing);
                            objectCreator.Perform ();
                        }

                        Map.UndoStepCreate ("Object Line");
                        Map.Update ();
                        Map.MinimapMakeLater ();
                        Map.Selected_Tile_A = null;
                        MapViewControl.DrawViewLater ();
                    } else if (tile.Y == Map.Selected_Tile_A.Y) {
                        if (tile.X <= Map.Selected_Tile_A.X) {
                            a = tile.X;
                            b = Map.Selected_Tile_A.X;
                        } else {
                            a = Map.Selected_Tile_A.X;
                            b = tile.X;
                        }
                        clsUnitCreate objectCreator = new clsUnitCreate ();
                        Map.SetObjectCreatorDefaults (objectCreator);
                        for (num = a; num <= b; num++) {
                            objectCreator.Horizontal.X = (int)((num + 0.5D) * App.TerrainGridSpacing);
                            objectCreator.Horizontal.Y = (int)((tile.Y + 0.5D) * App.TerrainGridSpacing);
                            objectCreator.Perform ();
                        }

                        Map.UndoStepCreate ("Object Line");
                        Map.Update ();
                        Map.MinimapMakeLater ();
                        Map.Selected_Tile_A = null;
                        MapViewControl.DrawViewLater ();
                    } else {
                    }
                } else {
                    Map.Selected_Tile_A = new clsXY_int (tile);
                }
            }
        }
    }
}