﻿/*
Copyright 2010 Google Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Discovery;
using Google.Apis.Tests.Apis.Requests;
using Google.Apis.Tools.CodeGen.Generator;
using NUnit.Framework;

namespace Google.Apis.Tools.CodeGen.Tests.Generator
{
    /// <summary>
    /// Test cases for the ResourceContainerGenerator.
    /// </summary>
    [TestFixture]
    public class ResourceClassGeneratorTest
    {
        private static ResourceContainerGenerator ConstructContainerGenerator()
        {
            return new ResourceContainerGenerator(GoogleServiceGenerator.StandardResourceContainerDecorator);   
        }

        private ResourceClassGenerator ConstructGenerator(IResource resource)
        {
            return new ResourceClassGenerator(
                resource, "TestService", GoogleServiceGenerator.GetSchemaAwareResourceDecorators("Generated.Data"),
                ConstructContainerGenerator(), new string[0]);
        }

        /// <summary>
        /// Tests the constructor
        /// </summary>
        [Test]
        public void ConstructTest()
        {
            Assert.IsNotNull(ConstructContainerGenerator());
            Assert.IsNotNull(ConstructGenerator(new MockResource() { Name = "TestResource" }));
        }

        /// <summary>
        /// Confirms that the generator can create a simple resource class.
        /// </summary>
        [Test]
        public void GenerationTest()
        {
            var resource = new MockResource();
            resource.Name = "Test";
            resource.Methods = new Dictionary<string, IMethod>()
                                   { { "TestMethod", new MockMethod() { Name = "TestMethod", HttpMethod = "GET" } } };

            // Run the generator.
            var generator = ConstructGenerator(resource);
            CodeTypeDeclaration clss = generator.CreateClass();
            Assert.IsNotNull(clss);
            Assert.AreEqual("TestResource", clss.Name);

            // Confirm that decorators have run.
            // The exact results are checked in separate tests of the decorators.
            Assert.Greater(clss.Members.Count, 0);
        }

        /// <summary>
        /// Confirms that subresources are generated as nested classes.
        /// </summary>
        [Test]
        public void SubresourceGenerationTest()
        {
            var subresource = new MockResource();
            subresource.Name = "Sub";

            var resource = new MockResource();
            resource.Name = "Test";
            resource.Resources = new Dictionary<string, IResource>() { { "Sub", subresource } };

            // Run the generator.
            var generator = ConstructGenerator(resource);
            CodeTypeDeclaration clss = generator.CreateClass();
            Assert.IsNotNull(clss);

            // Confirm that a subclass has been added.
            var subtypes = from CodeTypeMember m in clss.Members where (m is CodeTypeDeclaration) select m;
            Assert.AreEqual(1, subtypes.Count());
            Assert.AreEqual("SubResource", subtypes.First().Name);
        }
    }
}
