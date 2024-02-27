using APEX.Common.Constraints;
using APEX.Common.Solver;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace APEX.Common.Simulator
{
    public class ApexRopeSimulator : ApexSimulatorBase
    {
        // particle param
        public NativeArray<float3> originPosition = new NativeArray<float3>();
        public NativeArray<float3> previousPosition = new NativeArray<float3>();
        public NativeArray<float3> nowPosition = new NativeArray<float3>();
        public NativeArray<float3> nextPosition = new NativeArray<float3>();

        // particle physics param
        public NativeArray<float> mass = new NativeArray<float>();
        public NativeArray<float3> forceExt = new NativeArray<float3>();

        // particle constraint type
        public NativeArray<EApexParticleConstraintType>
            constraintTypes = new NativeArray<EApexParticleConstraintType>();

        // constraint connect param
        public NativeArray<ApexConstraintParticleDouble>
            doubleConnect = new NativeArray<ApexConstraintParticleDouble>();

        // particle pin(Attachment)
        public NativeArray<ApexPinConstraint> pin = new NativeArray<ApexPinConstraint>();

        // switch constraint
        public bool useForce = true;
        public bool useDistanceConstraint = true;
        public bool useColliderConstraint = true;

        // physics param - force
        public float3 gravity = new float3(0, -9.81f, 0);
        public float3 globalForce = new float3(0, 0, 0);
        public float airDrag = 0.2f;
        public float damping = 0.5f;

        // simulator param
        public int iterator = 10;

        private JobHandle _jobHandle;

        /// <summary>
        /// consider pin, iterator, collider... do the final constraint
        /// </summary>
        /// <param name="depend">job handle depend</param>
        /// <returns>job handle depend</returns>
        private JobHandle DoFinalConstraintJobs(JobHandle depend)
        {
            // float d = 1.0f / (iterator - iterIndex);
            var finalConstraintJob = new FinalConstraintJob()
            {
                position = nextPosition,

                particleConstraintTypes = constraintTypes,

                pin = pin,
            };
            return finalConstraintJob.Schedule(nowPosition.Length, depend);
        }

        /// <summary>
        /// do distance constraint jobs
        /// </summary>
        /// <param name="depend">job handle depend</param>
        /// <param name="iterIndex">the iterator index</param>
        /// <returns>job handle depend</returns>
        private JobHandle DoDistanceConstraintJobs(JobHandle depend, int iterIndex)
        {
            var d = 1.0f / (iterator - iterIndex);
            var distanceConstraintJob = new DistanceConstraintJob()
            {
                nextPosition = nextPosition,
                constraints = doubleConnect,

                restLength = 1.2f,
                stiffness = 0.5f,

                masses = mass,
                d = d,
            };
            return distanceConstraintJob.Schedule(distanceConstraintJob.constraints.Length, depend);
        }

        /// <summary>
        /// do all constraint jobs
        /// </summary>
        /// <param name="depend">job handle depend</param>
        /// <returns>job handle depend</returns>
        private JobHandle DoConstraintJobs(JobHandle depend)
        {
            JobHandle jobDepend = depend;
            for (var i = 0; i < iterator; i++)
            {
                jobDepend = DoDistanceConstraintJobs(jobDepend, i);
                jobDepend = DoFinalConstraintJobs(jobDepend);
            }

            return jobDepend;
        }

        /// <summary>
        /// do force effect and predict particle next position.
        /// </summary>
        /// <param name="dt">delta time</param>
        /// <returns>job handle depend</returns>
        private JobHandle DoForceJobs(float dt)
        {
            var job = new SimulateForceExtJob
            {
                previousPosition = previousPosition,
                nowPosition = nowPosition,
                nextPosition = nextPosition,

                mass = mass,

                forceExt = forceExt,
                gravity = gravity,
                globalForce = globalForce,

                airDrag = airDrag,
                damping = damping,

                dt = dt,
            };

            var depend = new JobHandle();
            return job.ScheduleParallel(job.nowPosition.Length, 64, depend);
        }

        /// <summary>
        /// Do Step:
        ///     1. predict next position, by Varlet integral
        ///     2. Collider
        ///     3. revise next position, by distance &
        /// </summary>
        /// <param name="dt">delta time</param>
        public override void Step(float dt)
        {
            var handle = DoForceJobs(dt); // 1. predict next position
            // TODO: 2. collider constraint.. 
            handle = DoConstraintJobs(handle); // 3. revise next position
            _jobHandle = handle;
        }

        /// <summary>
        /// Complete all jobs
        /// </summary>
        public override void Complete()
        {
            _jobHandle.Complete();
        }
    }
}