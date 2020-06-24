using NUnit.Framework;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Roslyn.Tests
{
    public class ConstraintTests : RoslynTestsFixture
    {
        const float k_Epsilon = 1e-5f;

        const int k_RootIndex = 1;
        const int k_MidIndex  = 2;
        const int k_TipIndex  = 3;

        const int k_Twist0Index = 1;
        const int k_Twist1Index = 2;
        const int k_HandIndex   = 3;

        BlobAssetReference<RigDefinition> m_Rig;

        BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one() },
                new SkeletonNode { ParentIndex = 0, Id = "UpperArm", AxisIndex = -1, LocalTranslationDefaultValue = new float3(1f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
                new SkeletonNode { ParentIndex = 1, Id = "LowerArm", AxisIndex = -1, LocalTranslationDefaultValue = new float3(1f, 0f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
                new SkeletonNode { ParentIndex = 2, Id = "Hand", AxisIndex = -1, LocalTranslationDefaultValue = new float3(1f, 0.02f, 0f), LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            m_Rig = CreateTestRigDefinition();
        }

        [Test]
        public void TwoBoneIKFollowsTarget()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var ikData = Core.TwoBoneIKData.Default();
            ikData.RootIndex = k_RootIndex;
            ikData.MidIndex = k_MidIndex;
            ikData.TipIndex = k_TipIndex;
            ikData.Target.pos = new float3(0f, 1f, 0f);

            for (int i = 0; i < 5; ++i)
            {
                ikData.Target.pos.y += 0.3f;
                Core.SolveTwoBoneIK(ref stream, ikData, 1f);

                float3 rootToTip = math.normalize(stream.GetLocalToRootTranslation(k_TipIndex) - stream.GetLocalToRootTranslation(k_RootIndex));
                float3 rootToTarget = math.normalize(ikData.Target.pos - stream.GetLocalToRootTranslation(k_RootIndex));

                Assert.That(rootToTarget, Is.EqualTo(rootToTip).Using(TranslationComparer));
            }

            buffer.Dispose();
        }

        [Test]
        public void TwoBoneIKInfluencedByHint()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var ikData = Core.TwoBoneIKData.Default();
            ikData.RootIndex = k_RootIndex;
            ikData.MidIndex = k_MidIndex;
            ikData.TipIndex = k_TipIndex;
            ikData.HintWeight = 1f;

            // Bend considering target is above x axis
            var midPos1 = stream.GetLocalToRootTranslation(k_MidIndex);
            ikData.Target.pos = new float3(2f, 0.5f, 0f);
            ikData.Hint = midPos1 + new float3(0f, -2f, 0f);

            Core.SolveTwoBoneIK(ref stream, ikData, 1f);

            var midPos2 = stream.GetLocalToRootTranslation(k_MidIndex);
            Assert.Less(midPos2.y, 0f, "Expected midPos2.y to be less than zero");
            Assert.AreEqual(midPos1.z, midPos2.z, $"Expected midPos2.z to be '{midPos1.z}' but was '{midPos2.z}'");

            // Bend considering target is below x axis and hint is inverted
            ikData.Target.pos = new float3(2f, -0.5f, 0f);
            ikData.Hint = midPos2 + new float3(0f, 2f, 0f);
            Core.SolveTwoBoneIK(ref stream, ikData, 1f);

            var midPos3 = stream.GetLocalToRootTranslation(k_MidIndex);
            Assert.Greater(midPos3.y, 0f, "Expected midPos3.y to be greater than zero");
            Assert.AreEqual(midPos1.z, midPos3.z, $"Expected midPos3.z to be '{midPos1.z}' but was '{midPos3.z}'");

            // Move hint in z axis
            ikData.Hint += new float3(0f, 0f, 1f);
            Core.SolveTwoBoneIK(ref stream, ikData, 1f);

            var midPos4 = stream.GetLocalToRootTranslation(k_MidIndex);
            Assert.Greater(midPos4.y, 0f, "Expected midPos4.y to be greater than zero");
            Assert.Greater(midPos4.z, 0f, "Expected midPos4.z to be greater than zero");

            // Move hint in -z axis
            ikData.Hint.z *= -1f;
            Core.SolveTwoBoneIK(ref stream, ikData, 1f);

            var midPos5 = stream.GetLocalToRootTranslation(k_MidIndex);
            Assert.Greater(midPos5.y, 0f, "Expected midPos5.y to be greater than zero");
            Assert.Less(midPos5.z, 0f, "Expected midPos5.z to be less than zero");

            buffer.Dispose();
        }

        [Test]
        public void TwoBoneIKInfluencedByWeight()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var ikData = Core.TwoBoneIKData.Default();
            ikData.RootIndex = k_RootIndex;
            ikData.MidIndex = k_MidIndex;
            ikData.TipIndex = k_TipIndex;
            ikData.Target.pos = new float3(2f, 0.5f, 0f);
            var tipPos1 = stream.GetLocalToRootTranslation(k_TipIndex);

            Core.SolveTwoBoneIK(ref stream, ikData, 1f);
            var tipPos2 = stream.GetLocalToRootTranslation(k_TipIndex);

            for (int i = 0; i < 6; ++i)
            {
                float w = i / 5f;

                AnimationStreamUtils.SetDefaultValues(ref stream);
                Core.SolveTwoBoneIK(ref stream, ikData, w);

                float3 weightedTipPos = math.lerp(tipPos1, tipPos2, w);
                float3 tipPos = stream.GetLocalToRootTranslation(k_TipIndex);

                Assert.That(weightedTipPos, Is.EqualTo(tipPos).Using(TranslationComparer));
            }

            buffer.Dispose();
        }

        [Test]
        public void PositionConstraintFollowsSources()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var posConstraintData = Core.PositionConstraintData.Default();
            posConstraintData.Index = 1;
            posConstraintData.SourcePositions = new NativeArray<float3>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            posConstraintData.SourceOffsets = new NativeArray<float3>(2, Allocator.Temp, NativeArrayOptions.ClearMemory);
            posConstraintData.SourceWeights = new NativeArray<float>(2, Allocator.Temp, NativeArrayOptions.ClearMemory);

            posConstraintData.SourcePositions[0] = new float3(2f, 0f, 0f);
            posConstraintData.SourcePositions[1] = new float3(-2f, 0f, 0f);

            var defaultPos = stream.GetLocalToRootTranslation(posConstraintData.Index);

            // w0 = 0, w1 = 0
            Core.SolvePositionConstraint(ref stream, posConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(posConstraintData.Index), Is.EqualTo(defaultPos).Using(TranslationComparer));

            // w0 = 1, w1 = 0
            AnimationStreamUtils.SetDefaultValues(ref stream);
            posConstraintData.SourceWeights[0] = 1f;
            Core.SolvePositionConstraint(ref stream, posConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(posConstraintData.Index), Is.EqualTo(posConstraintData.SourcePositions[0]).Using(TranslationComparer));

            // w0 = 0, w1 = 1
            AnimationStreamUtils.SetDefaultValues(ref stream);
            posConstraintData.SourceWeights[0] = 0f;
            posConstraintData.SourceWeights[1] = 1f;
            Core.SolvePositionConstraint(ref stream, posConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(posConstraintData.Index), Is.EqualTo(posConstraintData.SourcePositions[1]).Using(TranslationComparer));

            // w0 = 1, w1 = 1
            // since source positions are mirrored, we should simply evaluate to origin.
            AnimationStreamUtils.SetDefaultValues(ref stream);
            posConstraintData.SourceWeights[0] = 1f;
            Core.SolvePositionConstraint(ref stream, posConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(posConstraintData.Index), Is.EqualTo(float3.zero).Using(TranslationComparer));

            buffer.Dispose();
            posConstraintData.SourcePositions.Dispose();
            posConstraintData.SourceOffsets.Dispose();
            posConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void PositionConstraintInfluencedByWeight()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var posConstraintData = Core.PositionConstraintData.Default();
            posConstraintData.Index = 1;
            posConstraintData.SourcePositions = new NativeArray<float3>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            posConstraintData.SourceOffsets = new NativeArray<float3>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
            posConstraintData.SourceWeights = new NativeArray<float>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);

            posConstraintData.SourcePositions[0] = new float3(0f, 0f, 2f);
            posConstraintData.SourceWeights[0] = 1f;

            var defaultPos = stream.GetLocalToRootTranslation(posConstraintData.Index);

            for (int i = 0; i < 6; ++i)
            {
                float w = i / 5f;

                AnimationStreamUtils.SetDefaultValues(ref stream);
                Core.SolvePositionConstraint(ref stream, posConstraintData, w);

                float3 weightedPos = math.lerp(defaultPos, posConstraintData.SourcePositions[0], w);
                Assert.That(stream.GetLocalToRootTranslation(posConstraintData.Index), Is.EqualTo(weightedPos).Using(TranslationComparer));
            }

            buffer.Dispose();
            posConstraintData.SourcePositions.Dispose();
            posConstraintData.SourceOffsets.Dispose();
            posConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void RotationConstraintFollowsSources()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var rotConstraintData = Core.RotationConstraintData.Default();
            rotConstraintData.Index = 1;
            rotConstraintData.SourceRotations = new NativeArray<quaternion>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            rotConstraintData.SourceOffsets = new NativeArray<quaternion>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            rotConstraintData.SourceWeights = new NativeArray<float>(2, Allocator.Temp, NativeArrayOptions.ClearMemory);

            for (int i = 0; i < 2; ++i)
            {
                rotConstraintData.SourceRotations[i] = quaternion.identity;
                rotConstraintData.SourceOffsets[i] = quaternion.identity;
            }

            var defaultRot = stream.GetLocalToRootRotation(rotConstraintData.Index);

            // w0 = 0, w1 = 0
            Core.SolveRotationConstraint(ref stream, rotConstraintData, 1f);
            Assert.That(stream.GetLocalToRootRotation(rotConstraintData.Index), Is.EqualTo(defaultRot).Using(RotationComparer));

            // Set source rotations
            rotConstraintData.SourceRotations[0] = quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(70));
            rotConstraintData.SourceRotations[1] = quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(-20));

            // w0 = 1, w1 = 0
            AnimationStreamUtils.SetDefaultValues(ref stream);
            rotConstraintData.SourceWeights[0] = 1f;
            Core.SolveRotationConstraint(ref stream, rotConstraintData, 1f);
            Assert.That(stream.GetLocalToRootRotation(rotConstraintData.Index), Is.EqualTo(rotConstraintData.SourceRotations[0]).Using(RotationComparer));

            // w0 = 0, w1 = 1
            AnimationStreamUtils.SetDefaultValues(ref stream);
            rotConstraintData.SourceWeights[0] = 0f;
            rotConstraintData.SourceWeights[1] = 1f;
            Core.SolveRotationConstraint(ref stream, rotConstraintData, 1f);
            Assert.That(stream.GetLocalToRootRotation(rotConstraintData.Index), Is.EqualTo(rotConstraintData.SourceRotations[1]).Using(RotationComparer));

            // w0 = 1, w1 = 1
            AnimationStreamUtils.SetDefaultValues(ref stream);
            rotConstraintData.SourceWeights[0] = 1f;
            Core.SolveRotationConstraint(ref stream, rotConstraintData, 1f);

            var res = math.normalizesafe(mathex.add(rotConstraintData.SourceRotations[0].value * 0.5f, rotConstraintData.SourceRotations[1].value * 0.5f));
            Assert.That(stream.GetLocalToRootRotation(rotConstraintData.Index), Is.EqualTo(res).Using(RotationComparer));

            buffer.Dispose();
            rotConstraintData.SourceRotations.Dispose();
            rotConstraintData.SourceOffsets.Dispose();
            rotConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void RotationConstraintInfluencedByWeight()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var rotConstraintData = Core.RotationConstraintData.Default();
            rotConstraintData.Index = 1;
            rotConstraintData.SourceRotations = new NativeArray<quaternion>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            rotConstraintData.SourceOffsets = new NativeArray<quaternion>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            rotConstraintData.SourceWeights = new NativeArray<float>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            rotConstraintData.SourceRotations[0] = quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(80));
            rotConstraintData.SourceOffsets[0] = quaternion.identity;
            rotConstraintData.SourceWeights[0] = 1f;

            var defaultRot = stream.GetLocalToRootRotation(rotConstraintData.Index);

            for (int i = 0; i < 6; ++i)
            {
                float w = i / 5f;

                AnimationStreamUtils.SetDefaultValues(ref stream);
                Core.SolveRotationConstraint(ref stream, rotConstraintData, w);

                quaternion weightedRot = mathex.lerp(defaultRot, rotConstraintData.SourceRotations[0], w);
                Assert.That(stream.GetLocalToRootRotation(rotConstraintData.Index), Is.EqualTo(weightedRot).Using(RotationComparer));
            }

            buffer.Dispose();
            rotConstraintData.SourceRotations.Dispose();
            rotConstraintData.SourceOffsets.Dispose();
            rotConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void ParentConstraintFollowsSources()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var parentConstraintData = Core.ParentConstraintData.Default();
            parentConstraintData.Index = 1;
            parentConstraintData.SourceTx = new NativeArray<RigidTransform>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            parentConstraintData.SourceOffsets = new NativeArray<RigidTransform>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            parentConstraintData.SourceWeights = new NativeArray<float>(2, Allocator.Temp, NativeArrayOptions.ClearMemory);

            for (int i = 0; i < 2; ++i)
            {
                parentConstraintData.SourceTx[i] = RigidTransform.identity;
                parentConstraintData.SourceOffsets[i] = RigidTransform.identity;
            }

            RigidTransform defaultTx;
            stream.GetLocalToRootTR(parentConstraintData.Index, out defaultTx.pos, out defaultTx.rot);

            // w0 = 0, w1 = 0
            Core.SolveParentConstraint(ref stream, parentConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(parentConstraintData.Index), Is.EqualTo(defaultTx.pos).Using(TranslationComparer));
            Assert.That(stream.GetLocalToRootRotation(parentConstraintData.Index), Is.EqualTo(defaultTx.rot).Using(RotationComparer));

            // Displace sources
            parentConstraintData.SourceTx[0] = new RigidTransform(mathex.mul(defaultTx.rot, quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(-90))), defaultTx.pos + new float3(0f, 1f, 0f));
            parentConstraintData.SourceTx[1] = new RigidTransform(mathex.mul(defaultTx.rot, quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(60))), defaultTx.pos + new float3(0f, 0f, 1f));

            // w0 = 1, w1 = 0
            parentConstraintData.SourceWeights[0] = 1f;
            AnimationStreamUtils.SetDefaultValues(ref stream);
            Core.SolveParentConstraint(ref stream, parentConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(parentConstraintData.Index), Is.EqualTo(parentConstraintData.SourceTx[0].pos).Using(TranslationComparer));
            Assert.That(stream.GetLocalToRootRotation(parentConstraintData.Index), Is.EqualTo(parentConstraintData.SourceTx[0].rot).Using(RotationComparer));

            // w0 = 0, w1 = 1
            parentConstraintData.SourceWeights[0] = 0f;
            parentConstraintData.SourceWeights[1] = 1f;
            AnimationStreamUtils.SetDefaultValues(ref stream);
            Core.SolveParentConstraint(ref stream, parentConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(parentConstraintData.Index), Is.EqualTo(parentConstraintData.SourceTx[1].pos).Using(TranslationComparer));
            Assert.That(stream.GetLocalToRootRotation(parentConstraintData.Index), Is.EqualTo(parentConstraintData.SourceTx[1].rot).Using(RotationComparer));

            // w0 = 1, w1 = 1
            parentConstraintData.SourceWeights[0] = 1f;
            AnimationStreamUtils.SetDefaultValues(ref stream);
            Core.SolveParentConstraint(ref stream, parentConstraintData, 1f);

            var posRes = (parentConstraintData.SourceTx[0].pos + parentConstraintData.SourceTx[1].pos) * 0.5f;
            var rotRes = math.normalizesafe(mathex.add(parentConstraintData.SourceTx[0].rot.value * 0.5f, parentConstraintData.SourceTx[1].rot.value * 0.5f));
            Assert.That(stream.GetLocalToRootTranslation(parentConstraintData.Index), Is.EqualTo(posRes).Using(TranslationComparer));
            Assert.That(stream.GetLocalToRootRotation(parentConstraintData.Index), Is.EqualTo(rotRes).Using(RotationComparer));

            buffer.Dispose();
            parentConstraintData.SourceTx.Dispose();
            parentConstraintData.SourceOffsets.Dispose();
            parentConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void ParentConstraintInfluencedByWeight()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var parentConstraintData = Core.ParentConstraintData.Default();
            parentConstraintData.Index = 1;
            parentConstraintData.SourceTx = new NativeArray<RigidTransform>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            parentConstraintData.SourceOffsets = new NativeArray<RigidTransform>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            parentConstraintData.SourceWeights = new NativeArray<float>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            RigidTransform defaultTx;
            stream.GetLocalToRootTR(parentConstraintData.Index, out defaultTx.pos, out defaultTx.rot);

            parentConstraintData.SourceTx[0] = new RigidTransform(mathex.mul(defaultTx.rot, quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(60))), defaultTx.pos + new float3(0f, 0f, 1f));
            parentConstraintData.SourceOffsets[0] = RigidTransform.identity;
            parentConstraintData.SourceWeights[0] = 1f;

            for (int i = 0; i < 6; ++i)
            {
                float w = i / 5f;

                AnimationStreamUtils.SetDefaultValues(ref stream);
                Core.SolveParentConstraint(ref stream, parentConstraintData, w);

                float3 weightedPos = math.lerp(defaultTx.pos, parentConstraintData.SourceTx[0].pos, w);
                quaternion weightedRot = mathex.lerp(defaultTx.rot, parentConstraintData.SourceTx[0].rot, w);

                Assert.That(stream.GetLocalToRootTranslation(parentConstraintData.Index), Is.EqualTo(weightedPos).Using(TranslationComparer));
                Assert.That(stream.GetLocalToRootRotation(parentConstraintData.Index), Is.EqualTo(weightedRot).Using(RotationComparer));
            }

            buffer.Dispose();
            parentConstraintData.SourceTx.Dispose();
            parentConstraintData.SourceOffsets.Dispose();
            parentConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void AimConstraintFollowsSources()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var aimConstraintData = Core.AimConstraintData.Default();
            aimConstraintData.Index = 1;
            aimConstraintData.LocalAimAxis = new float3(1f, 0f, 0f);
            aimConstraintData.SourcePositions = new NativeArray<float3>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            aimConstraintData.SourceOffsets = new NativeArray<quaternion>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            aimConstraintData.SourceWeights = new NativeArray<float>(2, Allocator.Temp, NativeArrayOptions.ClearMemory);

            RigidTransform defaultTx;
            stream.GetLocalToRootTR(aimConstraintData.Index, out defaultTx.pos, out defaultTx.rot);

            aimConstraintData.SourcePositions[0] = defaultTx.pos + new float3(1f,  3f, 0f);
            aimConstraintData.SourcePositions[1] = defaultTx.pos + new float3(1f, -3f, 0f);
            aimConstraintData.SourceOffsets[0] = quaternion.identity;
            aimConstraintData.SourceOffsets[1] = quaternion.identity;

            // w0 = 0, w1 = 0
            Core.SolveAimConstraint(ref stream, aimConstraintData, 1f);
            Assert.That(stream.GetLocalToRootTranslation(aimConstraintData.Index), Is.EqualTo(defaultTx.pos).Using(TranslationComparer));
            Assert.That(stream.GetLocalToRootRotation(aimConstraintData.Index), Is.EqualTo(defaultTx.rot).Using(RotationComparer));

            // w0 = 1, w1 = 0
            aimConstraintData.SourceWeights[0] = 1f;
            AnimationStreamUtils.SetDefaultValues(ref stream);
            Core.SolveAimConstraint(ref stream, aimConstraintData, 1f);

            RigidTransform constrainedTx;
            stream.GetLocalToRootTR(aimConstraintData.Index, out constrainedTx.pos, out constrainedTx.rot);
            float3 currAim = math.mul(constrainedTx.rot,  aimConstraintData.LocalAimAxis);
            float3 src0Dir = math.normalize(aimConstraintData.SourcePositions[0] - constrainedTx.pos);
            float3 src1Dir = math.normalize(aimConstraintData.SourcePositions[1] - constrainedTx.pos);
            Assert.AreEqual(0f, mathex.angle(currAim, src0Dir), k_Epsilon);
            Assert.AreNotEqual(0f, mathex.angle(currAim, src1Dir));

            // w0 = 0, w1 = 1
            aimConstraintData.SourceWeights[0] = 0f;
            aimConstraintData.SourceWeights[1] = 1f;
            AnimationStreamUtils.SetDefaultValues(ref stream);
            Core.SolveAimConstraint(ref stream, aimConstraintData, 1f);

            stream.GetLocalToRootTR(aimConstraintData.Index, out constrainedTx.pos, out constrainedTx.rot);
            currAim = math.mul(constrainedTx.rot,  aimConstraintData.LocalAimAxis);
            src0Dir = math.normalize(aimConstraintData.SourcePositions[0] - constrainedTx.pos);
            src1Dir = math.normalize(aimConstraintData.SourcePositions[1] - constrainedTx.pos);
            Assert.AreNotEqual(0f, mathex.angle(currAim, src0Dir));
            Assert.AreEqual(0f, mathex.angle(currAim, src1Dir), k_Epsilon);

            // w0 = 1, w1 = 1
            // Since both sources are opposite, they should cancel each other out
            aimConstraintData.SourceWeights[0] = 1f;
            AnimationStreamUtils.SetDefaultValues(ref stream);
            Core.SolveAimConstraint(ref stream, aimConstraintData, 1f);

            stream.GetLocalToRootTR(aimConstraintData.Index, out constrainedTx.pos, out constrainedTx.rot);
            currAim = math.mul(constrainedTx.rot,  aimConstraintData.LocalAimAxis);
            src0Dir = math.normalize(aimConstraintData.SourcePositions[0] - constrainedTx.pos);
            src1Dir = math.normalize(aimConstraintData.SourcePositions[1] - constrainedTx.pos);
            Assert.AreNotEqual(0f, mathex.angle(currAim, src0Dir));
            Assert.AreNotEqual(0f, mathex.angle(currAim, src1Dir));
            Assert.AreEqual(0f, mathex.angle(currAim, math.mul(defaultTx.rot, aimConstraintData.LocalAimAxis)), k_Epsilon);

            buffer.Dispose();
            aimConstraintData.SourcePositions.Dispose();
            aimConstraintData.SourceOffsets.Dispose();
            aimConstraintData.SourceWeights.Dispose();
        }

        [Test]
        public void AimConstraintInfluencedByWeight()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var aimConstraintData = Core.AimConstraintData.Default();
            aimConstraintData.Index = 1;
            aimConstraintData.LocalAimAxis = new float3(1f, 0f, 0f);
            aimConstraintData.SourcePositions = new NativeArray<float3>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            aimConstraintData.SourceOffsets = new NativeArray<quaternion>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            aimConstraintData.SourceWeights = new NativeArray<float>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);

            RigidTransform defaultTx;
            stream.GetLocalToRootTR(aimConstraintData.Index, out defaultTx.pos, out defaultTx.rot);

            aimConstraintData.SourcePositions[0] = defaultTx.pos + new float3(1f,  3f, 0f);
            aimConstraintData.SourceOffsets[0] = quaternion.identity;
            aimConstraintData.SourceWeights[0] = 1f;

            float angle = 180f;
            RigidTransform constrainedTx;
            for (int i = 0; i < 6; ++i)
            {
                float w = i / 5f;

                AnimationStreamUtils.SetDefaultValues(ref stream);
                Core.SolveAimConstraint(ref stream, aimConstraintData, w);

                stream.GetLocalToRootTR(aimConstraintData.Index, out constrainedTx.pos, out constrainedTx.rot);
                float3 currAim = math.mul(constrainedTx.rot, aimConstraintData.LocalAimAxis);
                float3 src0Dir = math.normalize(aimConstraintData.SourcePositions[0] - constrainedTx.pos);

                float test = mathex.angle(currAim, src0Dir);
                Assert.Less(test, angle, "Angle between currAim and src0Dir should be smaller than last evaluation since constraint weight is greater");
                angle = test;
            }
        }

        [Test]
        public void TwistCorrectionFollowsSource()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var defaultTwist0Rot = stream.GetLocalToParentRotation(k_Twist0Index);
            var defaultTwist1Rot = stream.GetLocalToParentRotation(k_Twist1Index);

            var twistCorrectionData = Core.TwistCorrectionData.Default();
            twistCorrectionData.SourceRotation = stream.GetLocalToParentRotation(k_HandIndex);
            twistCorrectionData.SourceInverseDefaultRotation = math.conjugate(twistCorrectionData.SourceRotation);
            twistCorrectionData.LocalTwistAxis = math.float3(1f, 0f, 0f);
            twistCorrectionData.TwistIndices = new NativeArray<int>(new int[] { k_Twist0Index, k_Twist1Index }, Allocator.Temp);
            twistCorrectionData.TwistWeights = new NativeArray<float>(twistCorrectionData.TwistIndices.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Apply rotation to source
            twistCorrectionData.SourceRotation = math.mul(twistCorrectionData.SourceRotation, quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(90)));

            // twist_w0 = 0, twist_w1 = 0
            Core.SolveTwistCorrection(ref stream, twistCorrectionData, 1f);
            Assert.That(stream.GetLocalToParentRotation(k_Twist0Index), Is.EqualTo(defaultTwist0Rot).Using(RotationComparer));
            Assert.That(stream.GetLocalToParentRotation(k_Twist1Index), Is.EqualTo(defaultTwist1Rot).Using(RotationComparer));

            // twist_w0 = 1, twist_w1 = 1 [twist nodes should be equal to source]
            twistCorrectionData.TwistWeights[0] = 1f;
            twistCorrectionData.TwistWeights[1] = 1f;
            Core.SolveTwistCorrection(ref stream, twistCorrectionData, 1f);

            // Verify twist on X axis
            var twist0Rot = stream.GetLocalToParentRotation(k_Twist0Index);
            var twist1Rot = stream.GetLocalToParentRotation(k_Twist1Index);
            Assert.That(twist0Rot.value.x, Is.EqualTo(twistCorrectionData.SourceRotation.value.x).Using(FloatComparer));
            Assert.That(twist0Rot.value.w, Is.EqualTo(twistCorrectionData.SourceRotation.value.w).Using(FloatComparer));
            Assert.That(twist1Rot.value.x, Is.EqualTo(twistCorrectionData.SourceRotation.value.x).Using(FloatComparer));
            Assert.That(twist1Rot.value.w, Is.EqualTo(twistCorrectionData.SourceRotation.value.w).Using(FloatComparer));

            // twistNode0.w = -1f, twistNode1.w = -1f [twist nodes should be inverse to source]
            twistCorrectionData.TwistWeights[0] = -1f;
            twistCorrectionData.TwistWeights[1] = -1f;
            Core.SolveTwistCorrection(ref stream, twistCorrectionData, 1f);

            // Verify twist on X axis
            var invTwist = math.inverse(twistCorrectionData.SourceRotation);
            twist0Rot = stream.GetLocalToParentRotation(k_Twist0Index);
            twist1Rot = stream.GetLocalToParentRotation(k_Twist1Index);
            Assert.That(twist0Rot.value.x, Is.EqualTo(invTwist.value.x).Using(FloatComparer));
            Assert.That(twist0Rot.value.w, Is.EqualTo(invTwist.value.w).Using(FloatComparer));
            Assert.That(twist1Rot.value.x, Is.EqualTo(invTwist.value.x).Using(FloatComparer));
            Assert.That(twist1Rot.value.w, Is.EqualTo(invTwist.value.w).Using(FloatComparer));
        }

        [Test]
        public void TwistCorrectionInfluencedByWeight()
        {
            var buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(m_Rig, buffer);
            AnimationStreamUtils.SetDefaultValues(ref stream);

            var sourceRotationDefault = stream.GetLocalToParentRotation(k_HandIndex);

            var twistCorrectionData = Core.TwistCorrectionData.Default();
            twistCorrectionData.SourceRotation = stream.GetLocalToParentRotation(k_HandIndex);
            twistCorrectionData.SourceInverseDefaultRotation = math.conjugate(twistCorrectionData.SourceRotation);
            twistCorrectionData.LocalTwistAxis = math.float3(1f, 0f, 0f);
            twistCorrectionData.TwistIndices = new NativeArray<int>(new int[] { k_Twist0Index, k_Twist1Index }, Allocator.Temp);
            twistCorrectionData.TwistWeights = new NativeArray<float>(twistCorrectionData.TwistIndices.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Apply rotation to source
            twistCorrectionData.SourceRotation = math.mul(twistCorrectionData.SourceRotation, quaternion.AxisAngle(math.float3(1f, 0f, 0f), math.radians(90)));
            twistCorrectionData.TwistWeights[0] = 1f;

            for (int i = 0; i <= 5; ++i)
            {
                float w = i / 5.0f;
                Core.SolveTwistCorrection(ref stream, twistCorrectionData, w);

                var weightedRot = mathex.lerp(sourceRotationDefault, twistCorrectionData.SourceRotation, w);
                var twist0Rot = stream.GetLocalToParentRotation(k_Twist0Index);
                Assert.That(twist0Rot.value.x, Is.EqualTo(weightedRot.value.x).Using(FloatComparer));
                Assert.That(twist0Rot.value.w, Is.EqualTo(weightedRot.value.w).Using(FloatComparer));
            }
        }
    }
}
