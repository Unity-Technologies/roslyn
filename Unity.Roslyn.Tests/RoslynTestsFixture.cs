using System.Collections.Generic;
using Unity.Animation.Hybrid;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine;
using System;
using Unity.Collections;

namespace Unity.Roslyn.Tests
{
    public class FloatAbsoluteEqualityComparer : IEqualityComparer<float>
    {
        private readonly float m_AllowedError;

        public FloatAbsoluteEqualityComparer(float allowedError)
        {
            m_AllowedError = allowedError;
        }

        public bool Equals(float expected, float actual)
        {
            return math.abs(expected - actual) < m_AllowedError;
        }

        public int GetHashCode(float value)
        {
            return 0;
        }
    }