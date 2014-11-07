﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RetailCoderVBE.UnitTesting
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(IAssert))]
    public class AssertClass : IAssert
    {
        /// <summary>
        /// Verifies that the specified condition is <c>true</c>. The assertion fails if the condition is <c>false</c>.
        /// </summary>
        /// <param name="condition">Any Boolean value or expression.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        public void IsTrue(bool condition, string message = null)
        {
            if (condition)
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("IsTrue", message);
            }
        }

        /// <summary>
        /// Verifies that the specified condition is <c>false</c>. The assertion fails if the condition is <c>true</c>.
        /// </summary>
        /// <param name="condition">Any Boolean value or expression.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        public void IsFalse(bool condition, string message = null)
        {
            if (!condition)
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("IsFalse", message);
            }
        }

        /// <summary>
        /// Indicates that the assertion cannot be verified.
        /// </summary>
        /// <param name="message">An optional message to display.</param>
        public void Inconclusive(string message = null)
        {
            AssertHandler.OnAssertInconclusive(message);
        }

        /// <summary>
        /// Fails the assertion without checking any conditions.
        /// </summary>
        /// <param name="message">An optional message to display.</param>
        public void Fail(string message = null)
        {
            AssertHandler.OnAssertFailed("Fail", message);
        }

        /// <summary>
        /// Verifies that the specified object is <c>Nothing</c>. The assertion fails if it is not <c>Nothing</c>.
        /// </summary>
        /// <param name="value">The object to verify.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        public void IsNothing(object value, string message = null)
        {
            if (value == null)
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("IsNothing", message);
            }
        }

        /// <summary>
        /// Verifies that the specified object is not <c>Nothing</c>. The assertion fails if it is <c>Nothing</c>.
        /// </summary>
        /// <param name="value">The object to verify.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        public void IsNotNothing(object value, string message = null)
        {
            if (value != null)
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("IsNotNothing", message);
            }
        }

        /// <summary>
        /// Verifies that two specified objects are equal. The assertion fails if the objects are not equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        /// <remarks>
        /// <paramref name="expected"/> and <paramref name="actual"/> must be the same type.
        /// </remarks>
        public void AreEqual(object expected, object actual, string message = null)
        {
            if (expected.GetType() != actual.GetType())
            {
                AssertHandler.OnAssertInconclusive("[expected] and [actual] values are not the same type.");
                return;
            }

            if (expected.Equals(actual))
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("AreEqual", string.Concat("expected: ", expected.ToString(), "; actual: ", actual.ToString(), ". ", message));
            }
        }

        /// <summary>
        /// Verifies that two specified objects are not equal. The assertion fails if the objects are equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        /// <remarks>
        /// <paramref name="expected"/> and <paramref name="actual"/> must be the same type.
        /// </remarks>
        public void AreNotEqual(object expected, object actual, string message = null)
        {
            if (expected.GetType() != actual.GetType())
            {
                AssertHandler.OnAssertInconclusive("[expected] and [actual] values are not the same type.");
                return;
            }

            if (!expected.Equals(actual))
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("AreNotEqual", string.Concat("expected: ", expected.ToString(), "; actual: ", actual.ToString(), ". ", message));
            }
        }

        /// <summary>
        /// Verifies that two specified object variables refer to the same object. The assertion fails if they refer to different objects.
        /// </summary>
        /// <param name="expected">The expected reference.</param>
        /// <param name="actual">The actual reference.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        public void AreSame(object expected, object actual, string message = null)
        {
            if (object.ReferenceEquals(expected, actual))
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("AreSame", string.Concat("expected: ", expected.GetHashCode(), "; actual: ", actual.GetHashCode(), ". ", message));
            }
        }

        /// <summary>
        /// Verifies that two specified object variables refer to different objects. The assertion fails if they refer to the same object.
        /// </summary>
        /// <param name="expected">The expected reference.</param>
        /// <param name="actual">The actual reference.</param>
        /// <param name="message">An optional message to display if the assertion fails.</param>
        public void AreNotSame(object expected, object actual, string message = null)
        {
            if (!object.ReferenceEquals(expected, actual))
            {
                AssertHandler.OnAssertSucceeded();
            }
            else
            {
                AssertHandler.OnAssertFailed("AreNotSame", string.Concat("expected: ", expected.GetHashCode(), "; actual: ", actual.GetHashCode(), ". ", message));
            }
        }
    }
}