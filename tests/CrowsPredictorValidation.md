# CROWS predictor validation and scope

## Implementation
- `CrowsAutoTrack.FixedUpdate` publishes target velocity once per physics tick; render callbacks only consume it. Existing adaptive miss-distance velocity response moved unchanged in principle into the Unity-free sampler (2/s base, long-flight response, 30/s manoeuvre response). Raw weapon `_rb.velocity` remains sampled at solution consumption, matching the weapon's launch inheritance, including Y and excluding rotational point velocity.
- Warmup needs two physical samples and >=0.02 s; target switch/release clears histories, >0.15 s gaps reset, stale reads suppress lead. Solver zero is authoritative (no transform-jitter fallback). Unsupported targets use physics-clock transform differencing; >60 m/s fallback jumps invalidate rather than become plausible velocities. This fallback cannot guarantee animation-clock correctness for nonphysics movers.
- Acceleration estimates derive from raw physical velocity differences. Confidence requires coherent samples and >=0.2 s age, capped 0.65; estimate capped 4 m/s²; >12 m/s² shocks and stationary speed <0.3 m/s clear acceleration. Extrapolation lasts at most 0.75 s, then coasts at predicted velocity. Longitudinal braking stops, never extrapolates into reversal. These are conservative heuristics, not measured vehicle limits.
- At most six real-flight-table fixed-point iterations (1 ms residual), 15 s flight ceiling, 4000 m predicted query envelope and bounded deviation from initial time. Invalid/nonconvergent iterations fall back to the previous constant-velocity solution. Unavailable table falls back to existing flat-time lead without acceleration.
- Lead stays negative transverse relative displacement: FCS subtracts it. No new radial or drop offset is injected. Existing DriveAim inverse yaw/pitch and refreshed servo goals are unchanged.
- Predictor diagnostics every two seconds while consumed: samples/age, filtered velocity, acceleration/confidence, time, iterations, convergence/residual, world lead and raw inherited velocity. Parent-owned shot diagnostics report actual muzzle vs requested solution separately.

## Important limits
This is a bounded table-based lead predictor, **not a full drag/intercept solve**. The table is still for the ammunition's nominal launch conditions; radial own velocity only changes the relative predicted path-length query, not actual launch-speed/drag integration. The game's `LiveRound` does integrate the full launched motion. The FCS's drop/superelevation stays based on CurrentRange, not the iterated future range. Consequently long-range radial engagement and moving-platform results need live validation and may retain systematic vertical/time errors. Flat fallback omits drag and has no claimed percent accuracy. Existing lead-angle readout uses the constant-velocity estimate and can differ slightly from acceleration/iterated lead. No gameplay accuracy or runtime diagnostic observations are claimed from these offline tests.

## Source evidence
- `I:\GHPC\Scripts\Assembly-CSharp\GHPC\Weapons\FireControlSystem.cs:541-555`: `DistanceVectorToImpactPoint` subtracts lead; `CompensatedWeaponVector` independently applies range-table drop and muzzle parallax.
- `WeaponSystem.cs:614-633`: cached rigidbody linear velocity is added to muzzle-forward ammo speed; resultant magnitude/orientation initializes the round.
- `LiveRound.cs:2062-2067`: nonguided projectile advances through ballistic evaluator (RK4 or noncorrected), unlike our flight-table iteration.

## Reproduce
```
dotnet run --project tests\CrowsPredictorTests.csproj -c Release
dotnet build M1A1Abrams.csproj -c Release --no-restore -p:AllowUnsafeBlocks=true
```
The test executable compiles the exact production `CrowsPredictor.cs` without Unity, game assemblies or test framework packages. Assertions cover stationary/crossing motion, approaching/receding analytic vacuum time, inherited vertical velocity, caps and fallback, invalid input, braking stop, warmup/target switch/gap, repeated render samples, acceleration coherence/jitter, world rotation covariance, stale reads, reversal and rapid turn.

No deployment performed. Actual game shooting, frame sequencing, visual stability, ballistic-table integration and dispersion remain runtime validation tasks.
## The five-hypothesis mixture, the launch delay and the vertical bound (2026-09-16)

- `CrowsImm` blends five closed forms over the same filtered (velocity, acceleration, confidence):
  stopped, straight line, carried acceleration, constant turn rate, and brake-to-a-stop. The straight
  line is the fallback every other answer has to out-weigh, per the design note this round follows.
- Weights come from residuals over 0.1 / 0.2 / 0.4 / 0.8 / 1.6 s lags, measured in metres. The span is
  the point: over 0.2 s a brake and a carried acceleration predict the same millimetres, and only a lag
  long enough for the difference to become a metre separates them. Metres and not metres per second,
  or the four short lags average the one discriminating lag away.
- A 4% Markov mixing floor is applied every update so a hypothesis that stops being right can recover,
  and it is taken back out before the blend so it does not vote. Manoeuvre models lose probability when
  their acceleration/turn/braking premise disappears, allowing a turn to switch back to straight-line
  tracking. A blend with no hypothesis above an even share falls back to the straight line.
- The braking hypothesis asserts nothing at all - a zero displacement, not the straight line - when the
  target is not slowing, so it cannot act as a second constant-velocity model voting from no evidence.
- `LAUNCH_DELAY_SECONDS = 0.08` is flown as extra time of flight on the converged solution; the
  reported flight time stays the round's own air time. The 0.65 iteration damping applies only to a
  step that reverses the one before it, so the monotone (crossing) case keeps its full convergence
  rate and its 1e-5 analytic accuracy, and the convergence test still reads the true difference.
- The vertical axis is bounded twice: 3 m/s^2 over a 0.3 s horizon inside the carried terms, and an
  envelope on the blend's own vertical part. The result is gravity-referenced: invariant under a yaw of
  the whole scene (asserted to 1e-9), deliberately not invariant under a rotation about any other axis.
  The legacy single-model solve keeps full rotation invariance and its own test still pins it.
- Degenerate paths: an unprimed mixture returns `TargetDisplacement` unchanged, so a lock that is still
  acquiring behaves byte-identically to the previous build, and every pre-existing assertion holds.
- Hit-feedback bias correction now uses the `WeaponSystem.Fire` and `OldLiveRound.logHit` Harmony hooks.
  It keeps a fixed 32-slot in-flight shot ring, expires entries after 12 seconds, clears it on
  release/destruction, and stores correction in four fixed range bands.
- Tests: 92 assertions, run with the `dotnet run` command above.
## The unscented filter (2026-09-16)

- `CrowsUkf` is a nine state (relative position, velocity, acceleration) unscented Kalman filter fed
  the target's relative position every physics tick, plus its velocity where that velocity came from
  the game's solver rather than from differencing the same transform. Nineteen sigma points, Cholesky
  factors of both covariances, the gain taken by solving S rather than inverting it, NIS gating
  against the chi-square 0.999 points, a bounded adaptive jerk, a two point warm start, and a restart
  from any non-finite or non-positive-definite step.
- The separation of duties is estimator plus model bank: the filter owns the state and its
  uncertainty, the five hypotheses own what the target is doing with it. A per-model filter bank
  (IMM-UKF) is deliberately not implemented; it cannot be tuned against real vehicle data offline.
- Measured, and each pinned by a test: constant acceleration comes back as 2.000 m/s^2, a steady turn
  keeps its 20.000 m/s and reports its 4.000 m/s^2 centripetal, a 900 m teleport is refused with the
  state untouched, the estimate is invariant under a yaw of the scene to 2e-12, and with 0.15 m of
  measurement noise the position error is 0.057 m rms against a claimed 0.058 m.
- Allocations: 20000 ticks retain less than 4096 bytes and provoke at most two generation-zero
  collections, for the filter and for the mixture ring alike. Both take their fixed scratch buffers
  lazily on first use. Four assertions cover this so it cannot regress silently.
- Tests: 92.

## Runtime allocation and lifetime audit (2026-09-16)

- Reflection fields and properties now use one cache per runtime type. Repeated lookups no longer build
  `Type::member` strings, and typed field reads/writes use compiled delegates where the requested type
  exactly matches the field, avoiding value-type boxing on the tracking path.
- Method lookup caches all same-name candidates and checks their parameter types on each call. Calls to
  same-name, same-arity overloads can no longer reuse an incompatible method selected by an earlier call.
- Sight range, ammunition, fire-state and zoom text is assigned only when the displayed value changes.
  The target-size fallback reuses its collider array, and the instancing repair reuses a bounded camera
  array instead of allocating `Camera.allCameras` every second.
- Tracker destruction unregisters the fire control and releases cached renderer/collider references.
  Scene changes clear the previous scene's vehicle and game-manager references.
- The reflection regression checks include 200000 allocation-free typed reads and both branches of a
  same-arity overload pair. Release validation is 92 assertions and a zero-warning build.
