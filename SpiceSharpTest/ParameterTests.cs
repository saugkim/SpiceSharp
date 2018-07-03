﻿using NUnit.Framework;
using SpiceSharp;
using SpiceSharp.Attributes;

namespace SpiceSharpTest.Parameters
{
    /// <summary>
    /// Summary description for ParameterTests
    /// </summary>
    [TestFixture]
    public class ParameterTests
    {
        /// <summary>
        /// Example parameter class that contains parameters of every type
        /// </summary>
        public class ParameterExample : ParameterSet
        {
            [ParameterName("field1")]
            public double Field1;

            [ParameterName("property1")]
            public double Property1 { get; private set; }

            [ParameterName("property2")]
            public double Property2 { get; set; }

            [ParameterName("method1")]
            public void SetMethod1(double value) => Property1 = value;

            [ParameterName("method2")]
            public double GetMethod() => 1.0;

            [ParameterName("parameter1")]
            public GivenParameter Parameter1 { get; } = new GivenParameter();

            [ParameterName("parameter2")]
            public GivenParameter Parameter2 { get; set; } = new GivenParameter();

            [ParameterName("principal"), ParameterInfo("Principal parameter", IsPrincipal = true)]
            public Parameter Principal { get; } = new GivenParameter(0.8);
        }

        [Test]
        public void When_CopyPropertiesAndFields_CopiesField()
        {
            var source = new ParameterExample();
            source.Field1 = 1;
            var destination = new ParameterExample();
            Utility.CopyPropertiesAndFields(source, destination);
            Assert.AreEqual(1, destination.Field1);

            destination.Field1 = 2;

            Assert.AreEqual(1, source.Field1);
            Assert.AreEqual(2, destination.Field1);

            source.Field1 = 3;
            Assert.AreEqual(3, source.Field1);
            Assert.AreEqual(2, destination.Field1);
        }

        [Test]
        public void When_CopyPropertiesAndFields_CopiesPropertyWithPrivateSetter()
        {
            var source = new ParameterExample();
            source.SetMethod1(1);
            var destination = new ParameterExample();
            Utility.CopyPropertiesAndFields(source, destination);
            Assert.AreEqual(1, destination.Property1);

            destination.SetMethod1(2);

            Assert.AreEqual(1, source.Property1);

            source.SetMethod1(3);

            Assert.AreEqual(3, source.Property1);
            Assert.AreEqual(2, destination.Property1);
        }

        [Test]
        public void When_CopyPropertiesAndFields_CopiesProperty()
        {
            var source = new ParameterExample();
            source.Property2 = 1;
            var destination = new ParameterExample();
            Utility.CopyPropertiesAndFields(source, destination);
            Assert.AreEqual(1, destination.Property2);

            destination.Property2 = 2;

            Assert.AreEqual(1, source.Property2);
            Assert.AreEqual(2, destination.Property2);

            source.Property2 = 3;
            Assert.AreEqual(3, source.Property2);
            Assert.AreEqual(2, destination.Property2);
        }

        [Test]
        public void When_CopyPropertiesAndFields_CopiesReadonlyParameter()
        {
            var source = new ParameterExample();
            source.Parameter1.Value = 1;
            var destination = new ParameterExample();
            Utility.CopyPropertiesAndFields(source, destination);
            Assert.AreEqual(1, destination.Parameter1.Value);

            destination.Parameter1.Value = 2;

            Assert.AreEqual(1, source.Parameter1.Value);
            Assert.AreEqual(2, destination.Parameter1.Value);

            source.Parameter1.Value = 3;
            Assert.AreEqual(3, source.Parameter1.Value);
            Assert.AreEqual(2, destination.Parameter1.Value);
        }

        [Test]
        public void When_CopyPropertiesAndFields_CopiesWritableParameter()
        {
            var source = new ParameterExample();
            source.Parameter2.Value = 1;
            var destination = new ParameterExample();
            Utility.CopyPropertiesAndFields(source, destination);
            Assert.AreEqual(1, destination.Parameter2.Value);

            destination.Parameter2.Value = 2;

            Assert.AreEqual(1, source.Parameter2.Value);
            Assert.AreEqual(2, destination.Parameter2.Value);

            source.Parameter2.Value = 3;
            Assert.AreEqual(3, source.Parameter2.Value);
            Assert.AreEqual(2, destination.Parameter2.Value);
        }

        [Test]
        public void When_SetterForField_Expect_DirectAccess()
        {
            var p = new ParameterExample();
            var setter = p.GetSetter("field1");
            setter(1.0);
            Assert.AreEqual(1.0, p.Field1, 1e-12);
            setter(10.0);
            Assert.AreEqual(10.0, p.Field1, 1e-12);
        }

        [Test]
        public void When_SetterForGetOnlyProperty_Expect_Null()
        {
            var p = new ParameterExample();
            Assert.AreEqual(null, p.GetSetter("property1"));
        }

        [Test]
        public void When_SetterForProperty_Expect_DirectAccess()
        {
            var p = new ParameterExample();
            var setter = p.GetSetter("property2");
            setter(1.0);
            Assert.AreEqual(1.0, p.Property2, 1e-12);
            setter(10.0);
            Assert.AreEqual(10.0, p.Property2, 1e-12);
        }

        [Test]
        public void When_SetterForMethod_Expect_DirectAccess()
        {
            var p = new ParameterExample();
            var setter = p.GetSetter("method1");
            setter(1.0);
            Assert.AreEqual(1.0, p.Property1, 1e-12);
            setter(10.0);
            Assert.AreEqual(10.0, p.Property1, 1e-12);
        }

        [Test]
        public void When_SetterForParameterProperty_Expect_DirectAccess()
        {
            var p = new ParameterExample();
            var setter = p.GetSetter("parameter1");
            Assert.AreEqual(false, p.Parameter1.Given);
            setter(1.0);
            Assert.AreEqual(1.0, p.Parameter1.Value, 1e-12);
            Assert.AreEqual(true, p.Parameter1.Given);
        }

        [Test]
        public void When_GetParameter_Expect_Parameter()
        {
            var p = new ParameterExample();
            var param = p.GetParameter("parameter1");
            Assert.AreEqual(p.Parameter1, param);
        }

        [Test]
        public void When_PrincipalParameter_Expect_DirectAccess()
        {
            var p = new ParameterExample();
            var param = p.GetParameter();
            Assert.AreEqual(param, p.Principal);
        }

        [Test]
        public void When_PrincipalSetter_Expect_DirectAccess()
        {
            var p = new ParameterExample();
            var setter = p.GetSetter();
            setter(1.0);
            Assert.AreEqual(1.0, p.Principal.Value, 1e-12);
            setter(10.0);
            Assert.AreEqual(10.0, p.Principal.Value, 1e-12);
        }
    }
}