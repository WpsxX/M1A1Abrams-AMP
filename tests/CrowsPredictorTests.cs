using System;
using M1A1Abrams;
class CrowsPredictorTests
{
    static int passed;
    static LeadVector V(double x=0,double y=0,double z=0) { return new LeadVector(x,y,z); }
    static void Check(bool ok,string name) { if(!ok) throw new Exception("FAIL "+name); Console.WriteLine("PASS "+name); passed++; }
    static uint noise=12345u;
    static double Noise(double sigma) { unchecked { noise=noise*1664525u+1013904223u; } return ((noise>>8)/8388608.0-1.0)*sigma; }
    static bool Near(double a,double b,double tolerance=1e-8) { return Math.Abs(a-b)<tolerance; }
    static LeadVector Rotate(LeadVector v)
    {
        // Rodrigues rotation about a non-cardinal world axis.
        var n=V(1,2,3)*(1/Math.Sqrt(14)); double c=Math.Cos(0.83), s=Math.Sin(0.83);
        var cross=V(n.Y*v.Z-n.Z*v.Y,n.Z*v.X-n.X*v.Z,n.X*v.Y-n.Y*v.X);
        return v*c+cross*s+n*(LeadVector.Dot(n,v)*(1-c));
    }
    static LeadVector Yaw(LeadVector v,double angle)
    {
        double c=Math.Cos(angle), s=Math.Sin(angle);
        return V(v.X*c+v.Z*s, v.Y, -v.X*s+v.Z*c);
    }
    static void Main()
    {
        Func<double,double> flat=r=>r/1000;
        var zero=V(); var line=V(0,0,1000);
        var s=CrowsPredictor.Solve(line,zero,zero,zero,0,flat);
        Check(s.Valid && s.Converged && Near(s.TransverseDisplacement.Length,0),"stationary zero lead");
        s=CrowsPredictor.Solve(line,V(10),zero,zero,0,flat);
        Check(s.Converged && Near(s.FlightTime,1/Math.Sqrt(1-0.0001),0.00001),"crossing fixed point matches analytic vacuum time");
        Check(s.TransverseDisplacement.X>10 && Near(s.TransverseDisplacement.Z,0),"crossing displacement positive / transverse only");
        s=CrowsPredictor.Solve(line,zero,V(10,3),zero,0,flat);
        Check(s.TransverseDisplacement.X<0 && s.TransverseDisplacement.Y<0,"own inherited XYZ subtraction");
        var verticalClimb=CrowsPredictor.Solve(line,V(0,8),zero,zero,0,flat);
        var verticalDescent=CrowsPredictor.Solve(line,V(0,-8),zero,zero,0,flat);
        Check(verticalClimb.Valid && verticalClimb.TransverseDisplacement.Y>8
            && Near(verticalClimb.TransverseDisplacement.Y,-verticalDescent.TransverseDisplacement.Y),
            "target climb/descent produces opposite vertical lead with refined flight time");
        var ownClimb=CrowsPredictor.Solve(line,zero,V(0,8),zero,0,flat);
        Check(ownClimb.Valid && Near(ownClimb.TransverseDisplacement.Y,verticalDescent.TransverseDisplacement.Y),
            "own climb against stationary target equals target descent");
        var formation=CrowsPredictor.Solve(line,V(0,8),V(0,8),zero,0,flat);
        Check(formation.Valid && Near(formation.TransverseDisplacement.Length,0),
            "equal target/platform vertical motion cancels without duplicate compensation");
        s=CrowsPredictor.Solve(line,V(0,0,50),zero,zero,0,flat);
        Check(s.Converged && Near(s.FlightTime,1000.0/950,0.0001),"receding table time iteration");
        s=CrowsPredictor.Solve(line,V(0,0,-50),zero,zero,0,flat);
        Check(s.Converged && Near(s.FlightTime,1000.0/1050,0.0001),"approaching table time iteration");
        s=CrowsPredictor.Solve(line,V(10),zero,zero,0,r=>double.NaN);
        Check(!s.Valid,"invalid table rejected");
        int calls=0;
        s=CrowsPredictor.Solve(line,V(10),zero,V(2),0.65,r=>++calls%2==1?1:1.3);
        Check(s.Valid && !s.Converged && s.Iterations==6 && Near(s.TransverseDisplacement.X,10),"iteration cap falls back to constant velocity");
        s=CrowsPredictor.Solve(V(double.NaN),zero,zero,zero,0,flat);
        Check(!s.Valid,"nonfinite input rejected");
        var d=CrowsPredictor.TargetDisplacement(V(0.5),V(-4),0.65,10);
        Check(d.X>=0 && d.X<0.1,"braking cannot reverse over long flight");
        d=CrowsPredictor.TargetDisplacement(V(10),V(100),1,10);
        Check(Near(d.X,100+2.6*0.75*(10-0.375)),"acceleration magnitude confidence and horizon capped");
        Check(Near(CrowsPredictor.TargetDisplacement(zero,V(4),0.65,10).Length,0),"stopped target cannot accelerate from stale history");
        var ctrv=CrowsPredictor.TargetDisplacement(V(20,0,0),V(0,0,4),1,1);
        Check(ctrv.X>19 && ctrv.Z>1,"constant-turn-rate displacement");
        s=CrowsPredictor.SolveRelative(line,V(10),V(0,2),0.65,flat);
        Check(s.Valid && s.TransverseDisplacement.X>9 && s.TransverseDisplacement.Y>0,"relative acceleration solve");
        var m=new CrowsMotionSampler();
        m.Sample(1,0,V(10),2);
        Check(m.Samples==1 && m.Confidence==0,"first sample no acceleration warmup");
        Check(!m.Sample(1,0,V(90),2) && Near(m.Velocity.X,10),"duplicate physical timestamp ignored");
        for(int i=1;i<=50;i++) m.Sample(1,i*0.02,V(10+i*0.04),2);
        Check(m.Confidence>0.5 && m.Acceleration.X>1.8 && m.Confidence<=0.65,"sustained coherent acceleration gains bounded confidence");
        m.Sample(2,1.02,V(-3),2);
        Check(m.Samples==1 && m.Confidence==0 && Near(m.Velocity.X,-3),"target switch resets history");
        m.Sample(2,2,V(8),2);
        Check(m.Samples==1 && m.Confidence==0,"long physical gap resets history");
        m.Sample(2,2.02,V(double.NaN),2);
        Check(m.Samples==0,"invalid measurement clears stale prediction");
        m.Reset(); m.Sample(3,0,V(10),3);
        for(int i=1;i<=50;i++) m.Sample(3,i*0.02,V(10+(i%2==0?0.05:-0.05)),3);
        Check(m.Confidence<0.1,"alternating chassis jitter not trusted acceleration");
        m.Sample(3,1.02,zero,3);
        Check(m.Confidence==0,"stop rejects residual acceleration");
        var a=new CrowsMotionSampler(); var b=new CrowsMotionSampler();
        for(int i=0;i<100;i++) { var v=V(5+i*0.01); a.Sample(1,i*0.02,v,2); b.Sample(1,i*0.02,v,2); for(int j=0;j<5;j++) b.Sample(1,i*0.02,v,2); }
        Check(Near(a.Velocity.X,b.Velocity.X) && Near(a.Acceleration.X,b.Acceleration.X),"render-rate duplicates cannot change physics filtering");
        Check(a.Fresh(1.98) && !a.Fresh(2.2) && !a.Fresh(1),"stale or backwards physics clock rejected");
        var before=CrowsPredictor.Solve(line,V(12,1,8),V(-2,3,4),V(1,0.2,-0.3),0.5,flat);
        var rotated=CrowsPredictor.Solve(Rotate(line),Rotate(V(12,1,8)),Rotate(V(-2,3,4)),Rotate(V(1,0.2,-0.3)),0.5,flat);
        Check(before.Valid && rotated.Valid && (Rotate(before.TransverseDisplacement)-rotated.TransverseDisplacement).Length<1e-8,
            "world rotation invariance including inherited vertical velocity");
        m.Reset(); m.Sample(4,0,V(10),2);
        for(int i=1;i<=40;i++) m.Sample(4,i*0.02,V(10+i*0.04),2);
        m.Sample(4,0.82,V(-10),2);
        Check(m.Confidence==0 && Near(m.Acceleration.Length,0),"rapid reversal rejects acceleration history");
        m.Sample(4,0.84,V(0,0,10),2);
        Check(m.Confidence==0,"rapid turn rejects acceleration history");
        // ---------------------------------------------------------------- the five-hypothesis mixture
        //
        // The scenarios below feed the mixture exactly what a perfect physics filter would have
        // reported - position, velocity and acceleration at each 50 Hz tick - so what is being tested
        // is the model set and its weighting, not the sampler that is already covered above.

        var mixture=new CrowsImm();
        var seeded=CrowsPredictor.TargetDisplacement(V(10,1,2),V(4,-1,0.5),0.65,2);
        var fresh=mixture.Displace(V(10,1,2),V(4,-1,0.5),0.65,2);
        Check(!mixture.Primed && mixture.Dominant==CrowsImm.Constant,"mixture starts unprimed on the straight line");
        Check(Near(fresh.X,seeded.X)&&Near(fresh.Y,seeded.Y)&&Near(fresh.Z,seeded.Z),
            "unprimed mixture returns the single model solve unchanged");

        var straight=new CrowsImm();
        for(int i=0;i<=90;i++) { double t=i*0.02; straight.Sample(t,V(-500+12*t,40,900),V(12),V(),0); }
        var straightLead=straight.Displace(V(12),V(),0,2);
        Check(straight.Primed && straight.Dominant==CrowsImm.Constant,"straight-line target keeps the straight line");
        Check(Near(straightLead.X,24,1e-9) && Near(straightLead.Y,0) && Near(straightLead.Z,0),
            "a straight line is led by exactly its own travel");

        // A parked target is led by nothing. Every hypothesis predicts nothing here, so which one of
        // them is nominally dominant says more about the prior than about the target - what matters is
        // that the answer is zero and not the filter's history.
        var parked=new CrowsImm();
        for(int i=0;i<=90;i++) parked.Sample(i*0.02,V(300,5,-40),V(),V(),0);
        Check(parked.Displace(V(),V(),0,2).Length<1e-9,"a parked target is led by nothing");

        // And one that has just stopped, which is the case that matters: the filter still reports the
        // speed it had a moment ago, and a lead built on that is a lead the target will never use.
        var halted=new CrowsImm();
        for(int i=0;i<=110;i++)
        {
            double t=i*0.02;
            double rolled=Math.Min(t,1.2);
            double stale=12*Math.Exp(-2*Math.Max(0,t-1.2));
            halted.Sample(t,V(-500+12*rolled,40,900),V(stale),V(),0);
        }
        var haltedLead=halted.Displace(V(1.62),V(),0,2);

        Check(haltedLead.Length<1 && haltedLead.Length<1.62*2*0.4,
            "a stopped target is not led on the speed the filter still reports");

        // A target braking to a stop from 15 m/s at 3 m/s^2: the straight line and the carried
        // acceleration both keep it rolling past its own stop, and only the brake model does not.
        var braked=new CrowsImm();
        for(int i=0;i<=130;i++) { double t=i*0.02; braked.Sample(t,V(-800+15*t-1.5*t*t,20,600),V(15-3*t),V(-3),0.65); }
        double stopped=15-3*2.6;
        var brakedLead=braked.Displace(V(stopped),V(-3),0.65,4);
        var carriedLead=CrowsPredictor.TargetDisplacement(V(stopped),V(-3),0.65,5);
        var brakedFar=braked.Displace(V(stopped),V(-3),0.65,5);
        Check(braked.Dominant==CrowsImm.Braking && braked.Weight(CrowsImm.Braking)>0.8,
            "a braking target's stop is what the blend believes");
        Check(brakedLead.X<stopped*4*0.6 && brakedFar.X<carriedLead.X*0.6,
            "the braking lead stops short of the straight line instead of rolling past the stop");

        // A target holding a steady turn: the arc is the model that answers it, and the blend has to
        // beat the straight line by more than the arc's own margin over it.
        var turning=new CrowsImm();
        for(int i=0;i<=90;i++)
        {
            double t=i*0.02;
            turning.Sample(t,V(100*Math.Sin(0.2*t),0,100*Math.Cos(0.2*t)),V(20*Math.Cos(0.2*t),0,-20*Math.Sin(0.2*t)),
                V(-4*Math.Sin(0.2*t),0,-4*Math.Cos(0.2*t)),0.65);
        }
        var turnVel=V(20*Math.Cos(0.36),0,-20*Math.Sin(0.36));
        var turnAcc=V(-4*Math.Sin(0.36),0,-4*Math.Cos(0.36));
        var turnNow=V(100*Math.Sin(0.36),0,100*Math.Cos(0.36));
        var turnNear=turning.Displace(turnVel,turnAcc,0.65,0.5);
        var turnFar=turning.Displace(turnVel,turnAcc,0.65,2);
        var lineNear=CrowsImm.Hypothesis(CrowsImm.Constant,turnVel,turnAcc,0.65,0.5);
        var lineFar=CrowsImm.Hypothesis(CrowsImm.Constant,turnVel,turnAcc,0.65,2);
        var nearTrue=V(100*Math.Sin(0.46),0,100*Math.Cos(0.46))-turnNow;
        var farTrue=V(100*Math.Sin(0.76),0,100*Math.Cos(0.76))-turnNow;
        Check(turning.Dominant==CrowsImm.Turning && turning.Weight(CrowsImm.Turning)>0.5,
            "a steady turn is answered by the arc");
        Check((turnNear-nearTrue).Length < (lineNear-nearTrue).Length*0.5,
            "the arc beats the straight line over the horizon it is trusted for");
        Check((turnFar-farTrue).Length < (lineFar-farTrue).Length*0.8,
            "and is still better than the straight line over a whole flight");

        // The transition floor must let a model recover after another one has become dominant.
        var switched=new CrowsImm(); bool heldTurn=false;
        double switchAt=1.8, switchAngle=0.2*1.8;
        var switchPoint=V(100*Math.Sin(switchAngle),0,100*Math.Cos(switchAngle));
        var switchVelocity=V(20*Math.Cos(switchAngle),0,-20*Math.Sin(switchAngle));
        for(int i=0;i<=240;i++)
        {
            double t=i*0.02;
            if(t<=switchAt)
            {
                switched.Sample(t,V(100*Math.Sin(0.2*t),0,100*Math.Cos(0.2*t)),
                    V(20*Math.Cos(0.2*t),0,-20*Math.Sin(0.2*t)),
                    V(-4*Math.Sin(0.2*t),0,-4*Math.Cos(0.2*t)),0.65);
            }
            else
            {
                switched.Sample(t,switchPoint+switchVelocity*(t-switchAt),switchVelocity,V(),0);
            }
            if(i==90) heldTurn=switched.Dominant==CrowsImm.Turning;
        }
        Check(heldTurn && switched.Dominant==CrowsImm.Constant
            && switched.Weight(CrowsImm.Constant)>switched.Weight(CrowsImm.Turning),
            "IMM weights switch from a turn back to a straight line");

        // A straight line whose acceleration estimate is the vertical ring a hull gets over rough
        // ground: the horizontal lead stands, the vertical one is bounded.
        var bounced=new CrowsImm();
        for(int i=0;i<=90;i++) { double t=i*0.02; bounced.Sample(t,V(-500+12*t,40+2*t*t,900),V(12,4*t,0),V(0,4,0),0.65); }
        var bounceLead=bounced.Displace(V(12),V(0,4,0),0.65,2);
        var bounceLegacy=CrowsPredictor.TargetDisplacement(V(12),V(0,4,0),0.65,2);
        Check(bounceLegacy.Y>2 && bounceLead.Y<bounceLegacy.Y*0.6 && bounceLead.Y<1.7,
            "bounce cannot be led as a climb");
        Check(bounceLead.X>20,"and the horizontal lead is untouched by the vertical bound");

        // The vertical bound is a bound, not a cap: a target with a real rate of climb keeps it.
        var climbing=new CrowsImm();
        for(int i=0;i<=90;i++) { double t=i*0.02; climbing.Sample(t,V(-500+12*t,40+6*t,900),V(12,6,0),V(),0); }
        var climbLead=climbing.Displace(V(12,6,0),V(),0,2);
        Check(climbLead.Y>11 && Near(climbLead.X,24,1e-9),"a real climb is carried by its own vertical velocity");

        // A teleport is not evidence for anything, and a switch of target clears the ring.
        var jumped=new CrowsImm();
        for(int i=0;i<=40;i++) jumped.Sample(i*0.02,V(12*i*0.02,0,900),V(12),V(),0);
        Check(!jumped.Sample(0.82,V(900,0,-400),V(12),V(),0) && !jumped.Primed,
            "a teleport resets the mixture");

        // The whole mixture is gravity-referenced, so a rotation about the world vertical has to pass
        // through it unchanged; a rotation about any other axis need not, which is the trade the
        // vertical bound above is.
        var yawed=new CrowsImm();
        for(int i=0;i<=90;i++)
        {
            double t=i*0.02;
            yawed.Sample(t,Yaw(V(-500+12*t,40,900),0.7),Yaw(V(12,0.5,0),0.7),Yaw(V(0,1,0),0.7),0.65);
        }
        var yawMix=yawed.Displace(Yaw(V(12,0.5,0),0.7),Yaw(V(0,1,0),0.7),0.65,2);
        var yawExpected=Yaw(yawed.Displace(V(12,0.5,0),V(0,1,0),0.65,2),0.7);
        Check((yawMix-yawExpected).Length<1e-9,"the mixture is invariant under a yaw of the whole scene");

        // The launch delay is flown as extra time of flight, and the flight time the solver reports is
        // still the round's own air time.
        var delayTable=(Func<double,double>)(r=>r/1000.0);
        var delayed=CrowsPredictor.SolveRelative(V(0,0,1000),V(10),V(),0,delayTable,straight,0.08);
        var undelayed=CrowsPredictor.SolveRelative(V(0,0,1000),V(10),V(),0,delayTable);
        Check(delayed.Valid && delayed.Converged && Near(delayed.FlightTime,undelayed.FlightTime,1e-12),
            "the launch delay does not change the time of flight");
        Check(Near(delayed.TransverseDisplacement.X,undelayed.TransverseDisplacement.X+0.8,1e-6),
            "the launch delay adds its own displacement to the lead");

        // And the mixture reaches the solve: a braking target is led shorter by it than by the legacy
        // single model, over a flight long enough for the difference to matter.
        var brakedSolve=CrowsPredictor.SolveRelative(V(0,0,2000),V(stopped),V(-3),0.65,delayTable,braked,0);
        var legacySolve=CrowsPredictor.SolveRelative(V(0,0,2000),V(stopped),V(-3),0.65,delayTable);
        Check(brakedSolve.Valid && brakedSolve.Converged && brakedSolve.TransverseDisplacement.X<legacySolve.TransverseDisplacement.X,
            "the blend's braking answer reaches the solve");
        // ---------------------------------------------------------------- the unscented filter
        //
        // The filter is fed the same relative position and velocity a tick would give it in game, at
        // the 50 Hz physics rate, with an independent velocity measurement where the test is about the
        // state estimate rather than about the transform it has to fall back to.

        const double dt=0.02;

        // Two positions are enough to know the speed, which is the point of the warm start: a filter
        // started at rest would spend the next second finding out.
        var warm=new CrowsUkf();
        warm.Update(0,V(0,0,900),V(),false);
        warm.Update(dt,V(12*dt,0,900),V(),false);
        Check(warm.Healthy && warm.Samples==1 && Near(warm.Velocity.X,12,0.01),
            "the filter takes its speed from the first interval");
        Check(ModUtil.Reflect.Get<double[]>(warm,"sigma").Length==19*9
            && ModUtil.Reflect.Get<double[]>(warm,"cross_covariance").Length==9*6,
            "the UKF carries all sigma points and the full state-measurement covariance");

        var filterLine=new CrowsUkf();
        for(int i=0;i<=75;i++) { double t=i*dt; filterLine.Update(t,V(-500+12*t,40+3*t,900),V(12,3,0),true); }
        Check(filterLine.Healthy && Near(filterLine.Velocity.X,12,0.05) && Near(filterLine.Velocity.Y,3,0.05)
            && Near(filterLine.Velocity.Z,0,0.05),"a straight line is filtered to its own velocity");
        Check(Near(filterLine.Acceleration.X,0,0.1) && Near(filterLine.Acceleration.Y,0,0.1)
            && filterLine.VelocitySigma<0.2 && filterLine.PositionSigma<0.2,
            "and its acceleration to nothing, with a covariance that says so");

        // The capability the sampler before it did not have: acceleration as a state, not as the
        // difference of a smoothed signal.
        var accelerating=new CrowsUkf();
        for(int i=0;i<=100;i++) { double t=i*dt; accelerating.Update(t,V(20*t+t*t,0,800),V(20+2*t,0,0),true); }
        Check(Near(accelerating.Acceleration.X,2,0.05) && Near(accelerating.Velocity.X,24,0.05),
            "constant acceleration is estimated as a state");
        Check(accelerating.Confidence>0.2 && accelerating.Confidence<=0.65,
            "and is confident enough to be carried, inside the models' own scale");

        var stopping=new CrowsUkf();
        for(int i=0;i<=150;i++)
        {
            double t=i*dt; double rolled=Math.Min(t,2.0); double distance=12*rolled-3*rolled*rolled;
            stopping.Update(t,V(-500+distance,40,900),V(),false);
        }
        Check(Near(stopping.Velocity.X,0,0.15),"a target that stops is followed to a stop");

        var filterTurn=new CrowsUkf();
        for(int i=0;i<=150;i++)
        {
            double t=i*dt;
            filterTurn.Update(t,V(100*Math.Sin(0.2*t),0,100*Math.Cos(0.2*t)),
                V(20*Math.Cos(0.2*t),0,-20*Math.Sin(0.2*t)),true);
        }
        Check(Near(filterTurn.Velocity.Length,20,0.05) && Near(filterTurn.Acceleration.Length,4,0.05),
            "a steady turn keeps its speed and reports the centripetal acceleration");

        // A measurement the filter cannot account for is set aside, never absorbed: six in a row
        // restarts it, because that is a different target rather than a bad frame.
        var gated=new CrowsUkf();
        for(int i=0;i<=50;i++) { double t=i*dt; gated.Update(t,V(12*t,0,900),V(12),true); }
        bool refused=gated.Update(51*dt,V(12*51*dt+900,0,900),V(12),true);
        Check(!refused && gated.Rejections>=1 && Near(gated.Position.X,12*51*dt,0.01),
            "an impossible measurement is refused and leaves the state alone");
        Check(gated.Update(52*dt,V(12*52*dt,0,900),V(12),true) && Near(gated.Position.X,12*52*dt,0.01),
            "and the next good one is taken");

        // The covariance has to be honest: over noisy measurements of a straight line the errors the
        // filter makes have to be about the size of the errors it claims. Measured ratio is 0.99.
        noise=12345u;
        var noisy=new CrowsUkf(); double sum=0; int counted=0;
        for(int i=0;i<=250;i++)
        {
            double t=i*dt;
            noisy.Update(t,V(12*t+Noise(0.15),40+Noise(0.15),900+Noise(0.15)),V(),false);
            if(i>25) { var e=noisy.Position-V(12*t,40,900); sum+=e.X*e.X+e.Y*e.Y+e.Z*e.Z; counted++; }
        }
        double rms=Math.Sqrt(sum/counted);
        Check(rms<0.15 && rms<noisy.PositionSigma*2 && rms>noisy.PositionSigma*0.5,
            "the filter's errors are the size of the errors it claims");

        var spun=new CrowsUkf(); var plain=new CrowsUkf();
        for(int i=0;i<=80;i++)
        {
            double t=i*dt;
            plain.Update(t,V(12*t,3*t,800-5*t),V(12,3,-5),true);
            spun.Update(t,Yaw(V(12*t,3*t,800-5*t),0.9),Yaw(V(12,3,-5),0.9),true);
        }
        Check((spun.Position-Yaw(plain.Position,0.9)).Length<1e-9
            && (spun.Velocity-Yaw(plain.Velocity,0.9)).Length<1e-9,
            "the filter is invariant under a yaw of the whole scene");

        var restarted=new CrowsUkf();
        restarted.Update(0,V(0,0,900),V(),false);
        restarted.Update(double.NaN,V(0,0,900),V(),false);
        Check(!restarted.Healthy && restarted.Samples==0,"a nonsense sample restarts the filter");
        for(int i=0;i<=20;i++) { double t=1.0+i*dt; restarted.Update(t,V(12*t,0,900),V(),false); }
        Check(restarted.Healthy && Near(restarted.Velocity.X,12,0.05),"and it picks the track back up");

        var predicted=new CrowsUkf();
        for(int i=0;i<=100;i++) { double t=i*dt; predicted.Update(t,V(t*t,0,900),V(2*t,0,0),true); }
        Check(Near(predicted.Displacement(1.5).X,2*2.0*1.5+0.5*2*2.25,0.01),
            "its prediction is the constant-acceleration one");

        // The process noise it believes in moves with the evidence, inside fixed bounds: a long
        // straight line asks for less, and the brake at the end of it asks for more.
        var adaptive=new CrowsUkf();
        double travelled=0;
        for(int i=0;i<=200;i++)
        {
            double t=i*dt;
            double speed=t<=1.0 ? 12 : t<=1.5 ? 12-24*(t-1.0) : 0;
            travelled+=speed*dt;
            adaptive.Update(t,V(travelled,0,900),V(speed,0,0),true);
            if(i==50) Check(adaptive.Jerk<4 && adaptive.Jerk>=1,"a straight line asks the model for less jerk");
        }
        Check(adaptive.Jerk<=16 && adaptive.Healthy && Near(adaptive.Velocity.X,0,0.2),
            "a hard stop asks for more, and the bound holds");
        // Nothing in either estimator may allocate on a tick. A tracker runs at the physics rate for a
        // whole mission, and an allocation per tick is a garbage collection every second that the
        // game's own frame time pays for - which is why every buffer in both classes is a field. That
        // is a claim about two files, so it is measured rather than read: twenty thousand ticks of
        // each, with the managed heap taken before and after.
        var budget=new CrowsUkf();
        double budgetTime=0, budgetX=0;
        for(int i=0;i<200;i++) { budgetTime+=dt; budgetX+=12*dt; budget.Update(budgetTime,V(budgetX,0,900),V(12),true); }
        long heapBefore=GC.GetTotalMemory(true);
        int collections=GC.CollectionCount(0);
        for(int i=0;i<20000;i++) { budgetTime+=dt; budgetX+=12*dt; budget.Update(budgetTime,V(budgetX,0,900),V(12),true); }
        Check(GC.GetTotalMemory(true)-heapBefore<4096,"twenty thousand filter ticks retain nothing");
        Check(GC.CollectionCount(0)-collections<=2,"and provoke no collections worth the name");

        var ring=new CrowsImm();
        double ringTime=0;
        for(int i=0;i<200;i++) { ringTime+=dt; ring.Sample(ringTime,V(12*ringTime,0,900),V(12),V(),0); }
        long ringBefore=GC.GetTotalMemory(true);
        int ringCollections=GC.CollectionCount(0);
        for(int i=0;i<20000;i++) { ringTime+=dt; ring.Sample(ringTime,V(12*ringTime,0,900),V(12),V(),0); }
        Check(GC.GetTotalMemory(true)-ringBefore<4096,"twenty thousand mixture ticks retain nothing");
        Check(GC.CollectionCount(0)-ringCollections<=2,"and provoke no collections either");
        // And the scratch is taken lazily, so a station that never locks does not pay for two
        // estimators' worth of it: measured at 2144 B for an idle filter and 288 B for an idle
        // mixture, against 9192 and 8600 when both were allocated at construction. Bounded loosely
        // rather than pinned - what matters is that the constructor takes no buffers, not the header.
        long idleBefore=GC.GetTotalMemory(true);
        var idle=new object[2000];
        for(int i=0;i<1000;i++) idle[i]=new CrowsUkf();
        for(int i=1000;i<2000;i++) idle[i]=new CrowsImm();
        GC.KeepAlive(idle);
        long idleAfter=GC.GetTotalMemory(true);
        Check((idleAfter-idleBefore)/2000<4096,"an idle tracker costs kilobytes, not its scratch");
        // ---------------------------------------------------------------- the multi-shot correction
        var bias=new CrowsBallisticBias();
        for(int i=0;i<8;i++) bias.Observe(1.5,-1.0,1000);
        double wantRight=-1.5*180/(Math.PI*1000), wantUp=1.0*180/(Math.PI*1000);
        Check(bias.Right(1000)<0 && bias.Up(1000)>0,"the correction moves the aim away from where the rounds went");
        Check(Near(bias.Right(1000),wantRight,0.02) && Near(bias.Up(1000),wantUp,0.015),
            "eight rounds converge on the error they are all making");
        Check(Math.Abs(bias.Right(1000))<=CrowsBallisticBias.LimitRight && Math.Abs(bias.Up(1000))<=CrowsBallisticBias.LimitUp,
            "and stay inside the limit the design note sets");

        var wild=new CrowsBallisticBias();
        wild.Observe(400,-400,1000);
        Check(Math.Abs(wild.Right(1000))<=0.12*0.25+1e-9 && Math.Abs(wild.Up(1000))<=0.10*0.25+1e-9,"one wild round moves the aim by under a metre at a kilometre");

        var faded=new CrowsBallisticBias();
        for(int i=0;i<8;i++) faded.Observe(1.5,-1.0,1000);
        double prior=faded.Right(1000);
        faded.Decay(25);
        Check(Near(faded.Right(1000),prior*0.5,1e-9),"the correction halves over its halflife");

        var hit=new CrowsBallisticBias();
        for(int i=0;i<8;i++) hit.Observe(1.5,-1.0,1000);
        double wasHit=Math.Abs(hit.Right(1000));
        hit.Confirm(1000);
        Check(Math.Abs(hit.Right(1000))<wasHit,"a round on the aim pulls the correction back");

        var ignored=new CrowsBallisticBias();
        ignored.Observe(5,5,10);
        Check(ignored.Shots==0 && ignored.Right(1000)==0,"an observation off the range band is ignored");
        ignored.Observe(double.NaN,1,1000);
        Check(ignored.Shots==0,"and so is a nonsense one");
        for(int i=0;i<8;i++) ignored.Observe(1.5,-1.0,1000);
        ignored.Reset();
        Check(ignored.Shots==0 && ignored.Right(1000)==0 && ignored.Up(1000)==0,"and the correction can be dropped outright");
        // The correction is kept per range band; see CrowsBallisticBias.
        var banded=new CrowsBallisticBias();
        for(int i=0;i<8;i++) banded.Observe(2.0,0,700);
        Check(banded.Right(700)<-0.05 && banded.Right(2000)==0,"a correction learned at 700 m stays at 700 m");
        for(int i=0;i<8;i++) banded.Observe(0,2.0,2000);
        Check(banded.Up(2000)<-0.05 && banded.Right(700)<-0.05,"and a second band does not disturb the first");

        // The ballistic replica, against the flight times the game's own table produces for a 1565 m/s
        // fin round: about 0.96 s to 1500 m and 1.30 s to 2000 m, measured from the same integrator.
        CrowsBallistics.Configure(1565,0.20,0.0009,5.8);
        double replica1500=0, replica2000=0;
        bool at1500=CrowsBallistics.Ready && CrowsBallistics.FlightTime(1500,out replica1500);
        Check(at1500 && Near(replica1500,0.960,0.03),
            "the replica answers the flight time the game's table would have given");
        Check(CrowsBallistics.FlightTime(2000,out replica2000) && Near(replica2000,1.300,0.03),"and does it at 2000 m");
        double replaced;
        CrowsBallistics.Configure(1565,0.20,0.0009,5.8);
        Check(CrowsBallistics.FlightTime(1500,out replaced) && replaced==replica1500,
            "reconfiguring with the same round keeps the cache");
        double beyond;
        Check(!CrowsBallistics.FlightTime(20000,out beyond),"and a range past the far side of the curve is refused");
        // Reflect keeps one cache per declaring type and uses compiled typed accessors on hot paths.
        var reflectTarget=new CrowsUkf();
        ModUtil.Reflect.Set(reflectTarget,"jerk",5.0);
        Check(Near(ModUtil.Reflect.Get<double>(reflectTarget,"jerk"),5.0),"the object overload still sets a field");
        ModUtil.Reflect.Set<double>(reflectTarget,"jerk",6.0);
        Check(Near(ModUtil.Reflect.Get<double>(reflectTarget,"jerk"),6.0),"the generic overload sets the same field");
        ModUtil.Reflect.Set<double>(reflectTarget,"no_such_field",1.0);
        Check(Near(ModUtil.Reflect.Get<double>(reflectTarget,"jerk"),6.0),"a name that is not there is a no-op");
        int reflect_collections=GC.CollectionCount(0);
        for(int i=0;i<200000;i++) ModUtil.Reflect.Set<double>(reflectTarget,"jerk",i);
        Check(Near(ModUtil.Reflect.Get<double>(reflectTarget,"jerk"),199999),"it did the work");
        Check(GC.CollectionCount(0)-reflect_collections==0,"and the generic path allocates nothing");
        for(int i=0;i<10000;i++) ModUtil.Reflect.Get<double>(reflectTarget,"jerk");
        long reflect_bytes=GC.GetAllocatedBytesForCurrentThread();
        double reflect_sum=0;
        for(int i=0;i<200000;i++) reflect_sum+=ModUtil.Reflect.Get<double>(reflectTarget,"jerk");
        long reflect_read_bytes=GC.GetAllocatedBytesForCurrentThread()-reflect_bytes;
        Check(reflect_sum>0 && reflect_read_bytes==0,"the generic getter avoids cache-key strings and boxing");
        var overloads=new ReflectOverloads();
        Check((string)ModUtil.Reflect.Call(overloads,"Pick",7)=="int","method lookup selects an integer overload");
        Check((string)ModUtil.Reflect.Call(overloads,"Pick","x")=="string","and does not reuse it for a same-arity string overload");
        Console.WriteLine("TOTAL "+passed+" passed");
    }

    private sealed class ReflectOverloads
    {
        private string Pick(int value) { return "int"; }
        private string Pick(string value) { return "string"; }
    }
}

