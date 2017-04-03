﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Kinematics
{
    public class FrankenBot : Kinematic
    {
        private double[] qc;            // array of joint angles in radians
        private double[] minErrAngle;
        private double[] thetaOffset;   // array of theta offsets from DH parameters
        private Vector3D[,] frame;      // array of joint frame vectors
        private Vector3D Pd;            // desired position vector
        private Vector3D Ph;            // position of end effector
        private Vector3D[] Rd;          // desired orientation of end effector
        private Vector3D[] Rh;          // orientation of end effector
        private Vector3D Pih;         // array of relative position of end effector with respect to each frame
        private double deltaP;          // position error
        private double deltaO;          // orientation error
        private double Ec;              // current error
        private double Ep = 10;         // previous error
        private double Epp = 10;        // interation before last error
        private double minError = 100;  // tracks min error
        private bool Initialized = false;
        private double maxForce = 4;

        const int IK_MAX_TRIES = 1000;      // max number of CCD iterations
        private double wp = 1;              // position weight
        private double wo = 3;            // orientation weight
        private double hiErrThresh = 0.01;  // High error threshold, use factored rptCheck for errors beyond this magnitude
        private double hiLoFac = 10000;     // factor between high and low rptCheck
        private int rptCt = 0;              // counter for repeated error
        private double rptCheck;            // minimum change for repeat counter
        private int maxRpt = 5;             // maximum number of repeat errors
        private int nudgeCt = 0;            // counts nudges
        private double maxNudge = 5;        // maximum number of possible nudges
        const int H_SIZE = 4;
        const int NUM_JOINTS = 5;

        /// <summary>
        /// index = 0        1          2          3
        /// alpha(i-1)     a(i-i)     d(i)      theta(i)
        /// </summary>
        public double[,] DHparameters { get; set; }

        /// <summary>
        /// This returns the number of links in the manipulator
        /// </summary>
        public int N { get; set; }

        /// <summary>
        /// This returns the weights (0 or 1) of each end effector orientations
        /// </summary>
        public bool[] Sigma { get; set; }

        /// <summary>
        /// This returns the minimum and maximum angles for each joint in degrees
        /// </summary>
        public Point[] MinMax { get; set; }

        /// <summary>
        /// This returns the joint coupling of the manipulator
        /// </summary>
        public CouplingType Coupling { get; set; }

        /// <summary>
        /// This returns whether or not to output workspace forces
        /// </summary>
        public bool OutputWorkspace { get; set; }

        /// <summary>
        /// This returns whether or not to invert workspace forces
        /// </summary>
        public bool[] InvertForces { get; set; }

        /// <summary>
        /// This returns the names of the angle outputs
        /// </summary>
        public string[] OutputStrings { get; set; }

        /// <summary>
        /// Stop criteria for CCD
        /// </summary>
        public double eps { get; set; }

        /// <summary>
        /// Criteria to begin BGFS optimizer
        /// </summary>
        public double BETA { get; set; }

        /// <summary>
        /// This returns an array of joint angles from an input XYZ position
        /// </summary>
        /// <param name="Position"></param>
        /// <returns></returns>
        protected override double[] getJointAngles(Vector3D Position, Vector3D Orientation, double[,] RotM)
//        void KinInverse(double[,] Hd, double[] qcOut)
            if (!Initialized)
            {
                qc = new double[N + 1];
                qc.Initialize();

                thetaOffset = new double[N + 1];
                thetaOffset[0] = 0;
                for (int i = 1; i <= N; i++)
                {
                    thetaOffset[i] = DHparameters[i - 1, 3] * Math.PI / 180;
                }

                Initialized = true;
            }

            bool fCont = true;					// while loop continues while var = 1
            int stop = 0;
            double[] angles = new double[4];
            double deltaP = 0;      //total positional error
            double[] dj = new double[3];			//column placeholder for orientation calculation
            //CCD Specific Variables
            double k1, k2, k3;
	        double[] qLo = new double[NUM_JOINTS];
            qLo[0] = DegToRad(MinMax[0].X);
			qHi[0] = DegToRad(MinMax[0].Y);
	        for(int i=0; i<=NUM_JOINTS-1; i++){  //initialize to qc
		        qcMin[i] = qc[i];
	        }

            Pd = new Vector3D(Position.X, Position.Y, Position.Z);
            Rd = new Vector3D[3];
            Rd[0].X = RotM[0, 0];
            Rd[0].Y = RotM[0, 1];
            Rd[0].Z = RotM[0, 2];
            Rd[1].X = RotM[1, 0];
            Rd[1].Y = RotM[1, 1];
            Rd[1].Z = RotM[1, 2];
            Rd[2].X = RotM[2, 0];
            Rd[2].Y = RotM[2, 1];
            Rd[2].Z = RotM[2, 2];

            Rh = new Vector3D[3];
            // declare 3D array for each joint frame axis (xi, yi, zi, Pi)
            frame = new Vector3D[N + 1, 4];


                deltaP = Vector3D.DotProduct(Vector3D.Subtract(Pd, Ph), Vector3D.Subtract(Pd, Ph));

		        //calculate orientation error
                for (int i = 0; i < 3; i++)
                {
                    if (Sigma[i])
                    {
                        deltaO += Math.Pow((Vector3D.DotProduct(Rd[i], Rh[i]) - 1), 2);
                    }
                }

		        //Repeat Check 1 - normal repeating, high error
		        //save min error angles if current combined error is less than the current minimum error
		        //try to push algorithmn out of rut
				        fCont = false; //set value to end loop
			        //else if, perform BFS (code in BFScode.txt to keep function clean)
			        //else perform CCD -----------------------------
				        for(int i=NUM_JOINTS-1; i>=1; i--){ //- CS Edit
                            Pih = Vector3D.Subtract(Ph, frame[i, 3]);
                            Pid = Vector3D.Subtract(Pd, frame[i, 3]);
                            k1 = 0;
                            for (int j = 0; j < 3; j++)
                            {
                                if (Sigma[j])
                                    k1 += wo * Vector3D.DotProduct(Rd[j], frame[i, 2]) * Vector3D.DotProduct(Rh[j], frame[i, 2]);
                            }
                            k1 += wp * Vector3D.DotProduct(Pid, frame[i, 2]) * Vector3D.DotProduct(Pih, frame[i, 2]);

                            k2 = 0;
                            for (int j = 0; j < 3; j++)
                            {
                                if (Sigma[j])
                                    k2 += wo * Vector3D.DotProduct(Rd[j], Rh[j]);
                            }
                            k2 += wp * Vector3D.DotProduct(Pid, Pih);

                            k3 = 0;
                            Vector3D ko3 = new Vector3D();
                            for (int j = 0; j < 3; j++)
                            {
                                if (Sigma[j])
                                    //ko3 = Vector3D.Add(ko3, wo * Vector3D.CrossProduct(Rh[i], Rd[i]));
                                    ko3 = Vector3D.Add(ko3, wo * Vector3D.CrossProduct(Rd[j], Rh[j]));
                            }
                            //k3 = Vector3D.DotProduct(frame[link, 2], Vector3D.Add(wp * Vector3D.CrossProduct(Pih[link], Pid), ko3));
                            k3 = Vector3D.DotProduct(frame[i, 2], Vector3D.Add(wp * Vector3D.CrossProduct(Pid, Pih), ko3));

					        /*calculate phi
					        phi = -1*Math.Atan2(-k3, (k1-k2));
					        if ( (d2g>0) || (d2g<0.01) )
                                if (phi < 0)
                                    phi = phi + Math.PI;
                                else
                                    phi = phi - Math.PI;
					        }

					        /* phi is a CW rotation about zi from current position. Using this
					        /*phMax = qHi[i-1] - qc[i-1];
					        phMax = qHi[i] - qc[i];
                                phi -= 2 * Math.PI;
					        else if (phi < phMin)
                            {
						        //try the closest periodic solution
                                phi += 2 * Math.PI;
						        //if it is still outside of limits
                                gphMax = k1 * (1 - Math.Cos(phMax)) + k2 * Math.Cos(phMax) + k3 * Math.Sin(phMax);
                                gphMin = k1 * (1 - Math.Cos(phMin)) + k2 * Math.Cos(phMin) + k3 * Math.Sin(phMin);
				            //update angle
				            //update Hh
                            KinForward();
			        }//end CCD if statement
			        // TODO_CS: Consider returning fault here.
			        //check if maximum number of iterations has been reached
			        if (ct > maxIter)
	        //convert back to degrees
	        for (int s=0; s<=NUM_JOINTS-2; s++)
                angles[s] = RadToDeg(qc[s + 1]);
	        }

            return angles;

        void KinForward()
        {
            Vector3D[] Pstar = new Vector3D[N];
            Pstar.Initialize();

            // initialize frame positions
            for (int i = 0; i < N + 1; i++)
            {
                frame[i, 3].X = 0;
                frame[i, 3].Y = 0;
                frame[i, 3].Z = 0;
            }
            // forward recurrsion formulas for frame position and orientation
            for (int i = 1; i <= N; i++)
            {
                // x(i) orientation vector
                frame[i, 0] = Vector3D.Add((Vector3D.Multiply((Math.Cos(qc[i - 1] + thetaOffset[i])), frame[(i - 1), 0])), (Vector3D.Multiply((Math.Sin(qc[i - 1] + thetaOffset[i])), frame[(i - 1), 1])));
                // z(i) orientation vector
                frame[i, 2] = Vector3D.Add((Vector3D.Multiply(Math.Cos(DHparameters[i - 1, 0] * Math.PI / 180), frame[(i - 1), 2])), Vector3D.Multiply(Math.Sin(DHparameters[(i - 1), 0] * Math.PI / 180), Vector3D.CrossProduct(frame[i, 0], frame[(i - 1), 2])));
                // y(i) orientation vector
                frame[i, 1] = Vector3D.CrossProduct(frame[i, 2], frame[i, 0]);
            }
            // P* --> relative positions of next frame wrt present frame
            for (int i = 0; i < N; i++)
            {
                Pstar[i] = Vector3D.Add(Vector3D.Multiply(DHparameters[i, 2], frame[i, 2]), Vector3D.Multiply(DHparameters[i, 1], frame[i + 1, 0]));
            }
            //P(i) --> frame positions wrt base frame
            for (int i = 1; i < N + 1; i++)
            {
                frame[i, 3] = frame[i - 1, 3] + Pstar[i - 1];
            }
            // set position of end effector
            Ph = frame[N, 3];
            // set end effector orientation
            Rh[0] = frame[N, 0];
            Rh[1] = frame[N, 1];
            Rh[2] = frame[N, 2];
        }

        double DegToRad(double deg)
        {
            return deg / 180 * Math.PI; 
        }

        double RadToDeg(double rad)
        {
            return rad * 180 / Math.PI;
        }

        public override string[] OutputNames
        {
            get 
            {
                return OutputStrings;
            }
        }
    }
}