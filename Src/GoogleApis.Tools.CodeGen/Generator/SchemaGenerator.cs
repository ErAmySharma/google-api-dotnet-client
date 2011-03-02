/*
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
using System;
using System.CodeDom;
using System.Collections.Generic;

using Newtonsoft.Json.Schema;
    
using Google.Apis.Discovery.Schema;
using Google.Apis.Tools.CodeGen.Decorator.SchemaDecorator;
using Google.Apis.Util;
using Google.Apis.Testing;

namespace Google.Apis.Tools.CodeGen.Generator
{
    public class SchemaGenerator : BaseGenerator
    {
        
        
        private readonly IEnumerable<ISchemaDecorator> decorators;
        public SchemaGenerator (IEnumerable<ISchemaDecorator> decorators)
        {
            decorators.ThrowIfNull("decorators");
            this.decorators = decorators;
        }
        
        public CodeTypeDeclaration CreateClass(ISchema schema)
        {
            schema.ThrowIfNull("schema");
            string className = GeneratorUtils.GetClassName (schema);
            var typeDeclaration = new CodeTypeDeclaration(className);
            var nestedClassGenerator = new NestedClassGenerator(typeDeclaration, decorators, "");
            foreach( ISchemaDecorator schemaDecorator in decorators)
            {
                schemaDecorator.DecorateClass(typeDeclaration, schema, nestedClassGenerator);
            }
            nestedClassGenerator.GenerateNestedClasses();
            
            return typeDeclaration;
        }
        
        [VisibleForTestOnly]
        internal class NestedClassGenerator: INestedClassProvider
        {
            private static readonly log4net.ILog logger = log4net.LogManager.GetLogger (typeof(NestedClassGenerator));
            
            /// <summary>A string to make this nested class names unique</summary>
            private readonly string uniquefier;
            private readonly CodeTypeDeclaration typeDeclaration;
            private readonly IEnumerable<ISchemaDecorator> decorators;
            /// <summary>
            /// Maps Schemas to the index they appared so schemas found multiple time will resolve to the same name.
            /// This also allows us to generate the internal classes at the end instead of as we find them.
            /// </summary>
            private readonly IDictionary<JsonSchema, int> schemaOrder;
            private int nextSchemaNumber; 
            
            public NestedClassGenerator(CodeTypeDeclaration typeDeclaration, IEnumerable<ISchemaDecorator> decorators, string uniquefier)
            {
                this.typeDeclaration = typeDeclaration;
                this.decorators = decorators;
                this.schemaOrder = new Dictionary<JsonSchema, int>();
                nextSchemaNumber = 1;
                this.uniquefier = uniquefier;
            }
            
            /// <summary>
            /// Gets a class name as a CodeTypeReference for the given schema of the form "IntenalClassN" where 
            /// N is an integer. Given the same JsonSchema this will return the same classname.
            /// </summary>
            public CodeTypeReference GetClassName (JsonSchema definition)
            {
                if( schemaOrder.ContainsKey(definition) )
                {
                    return GetSchemaName(schemaOrder[definition]);
                }
                int schemaNumber = nextSchemaNumber++;
                schemaOrder.Add(definition, schemaNumber);
                return GetSchemaName(schemaNumber);
            }
            
            public void GenerateNestedClasses()
            {
                foreach(var pair in schemaOrder)
                {
                    typeDeclaration.Members.Add(GenerateNestedClass(pair.Key, pair.Value));
                }
            }
            
            [VisibleForTestOnly]
            internal CodeTypeDeclaration GenerateNestedClass(JsonSchema schema, int orderOfNestedClass)
            {
                schema.ThrowIfNull("schema");
                string className = GetClassName (schema).BaseType;
                var typeDeclaration = new CodeTypeDeclaration(className);
                typeDeclaration.Attributes = MemberAttributes.Public;
                var nestedClassGenerator = new NestedClassGenerator(typeDeclaration, decorators, uniquefier + orderOfNestedClass + "_");
                foreach( ISchemaDecorator schemaDecorator in decorators)
                {
                    if(schemaDecorator is INestedClassSchemaDecorator)
                    {
                        logger.DebugFormat("Found IInternalClassSchemaDecorator {0} - decorating {1}", schemaDecorator.ToString(), className);
                        ((INestedClassSchemaDecorator)schemaDecorator).
                            DecorateInternalClass(typeDeclaration, className, schema, nestedClassGenerator);
                    }
                }
                nestedClassGenerator.GenerateNestedClasses();
                
                return typeDeclaration;
            }
            
            private CodeTypeReference GetSchemaName(int schemaNumber)
            {
                return new CodeTypeReference(string.Format("NestedClass{0}{1}", uniquefier, schemaNumber));
            }
            
        }
    }
}
