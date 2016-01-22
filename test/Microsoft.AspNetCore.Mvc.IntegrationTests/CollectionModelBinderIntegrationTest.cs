// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the CollectionModelBinder with other model binders.
    //
    // Note that CollectionModelBinder handles both ICollection{T} and IList{T}
    public class CollectionModelBinderIntegrationTest
    {
        [Fact]
        public async Task CollectionModelBinder_BindsListOfSimpleType_WithPrefix_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0]=10&parameter[1]=11");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int>() { 10, 11 }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        [Theory]
        [InlineData("?prefix[0]=10&prefix[1]=11")]
        [InlineData("?prefix.index=low&prefix.index=high&prefix[low]=10&prefix[high]=11")]
        public async Task CollectionModelBinder_BindsListOfSimpleType_WithExplicitPrefix_Success(string queryString)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(List<int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int>() { 10, 11 }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData("?[0]=10&[1]=11")]
        [InlineData("?index=low&index=high&[high]=11&[low]=10")]
        public async Task CollectionModelBinder_BindsCollectionOfSimpleType_EmptyPrefix_Success(string queryString)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int> { 10, 11 }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfSimpleType_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<List<int>>(modelBindingResult.Model));

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person
        {
            public int Id { get; set; }
        }

        [Theory]
        [InlineData("?[0].Id=10&[1].Id=11")]
        [InlineData("?index=low&index=high&[low].Id=10&[high].Id=11")]
        [InlineData("?parameter[0].Id=10&parameter[1].Id=11")]
        [InlineData("?parameter.index=low&parameter.index=high&parameter[low].Id=10&parameter[high].Id=11")]
        public async Task CollectionModelBinder_BindsListOfComplexType_ImpliedPrefix_Success(string queryString)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(11, model[1].Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData("?prefix[0].Id=10&prefix[1].Id=11")]
        [InlineData("?prefix.index=low&prefix.index=high&prefix[high].Id=11&prefix[low].Id=10")]
        public async Task CollectionModelBinder_BindsListOfComplexType_ExplicitPrefix_Success(string queryString)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(List<Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(11, model[1].Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<List<Person>>(modelBindingResult.Model));

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person2
        {
            public int Id { get; set; }

            [BindRequired]
            public string Name { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_WithRequiredProperty_WithPrefix_PartialData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person2>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Id=10&parameter[1].Id=11");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[0].Name);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
            var error = Assert.Single(entry.Errors);
            Assert.Equal("A value for the 'Name' property was not provided.", error.ErrorMessage);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
            error = Assert.Single(entry.Errors);
            Assert.Equal("A value for the 'Name' property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_WithRequiredProperty_WithExplicitPrefix_PartialData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(List<Person2>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?prefix[0].Id=10&prefix[1].Id=11");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Null(model[0].Name);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[1].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[1].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsCollectionOfComplexType_WithRequiredProperty_EmptyPrefix_PartialData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<Person2>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?[0].Id=10&[1].Id=11");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Null(model[0].Name);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[1].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[1].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfSimpleType_WithIndex_Success()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<int>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.index=low&parameter.index=high&parameter[low]=10&parameter[high]=11");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int>() { 10, 11 }, model);

            // "index" is not stored in ModelState.
            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[low]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[high]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsCollectionOfComplexType_WithRequiredProperty_WithIndex_PartialData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<Person2>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?index=low&index=high&[high].Id=11&[low].Id=10");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Null(model[0].Name);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "[low].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[high].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[low].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[high].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_WithRequiredProperty_NoData()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person2>)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<List<Person2>>(modelBindingResult.Model));

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person4
        {
            public IList<Address4> Addresses { get; set; }
        }

        private class Address4
        {
            public int Zip { get; set; }

            public string Street { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_UsesCustomIndexes()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person4)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                var formCollection = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "Addresses.index", new [] { "Key1", "Key2" } },
                    { "Addresses[Key1].Street", new [] { "Street1" } },
                    { "Addresses[Key2].Street", new [] { "Street2" } },
                });

                request.Form = formCollection;
                request.ContentType = "application/x-www-form-urlencoded";
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.IsType<Person4>(modelBindingResult.Model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState, kvp => kvp.Key == "Addresses[Key1].Street").Value;
            Assert.Equal("Street1", entry.AttemptedValue);
            Assert.Equal("Street1", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "Addresses[Key2].Street").Value;
            Assert.Equal("Street2", entry.AttemptedValue);
            Assert.Equal("Street2", entry.RawValue);
        }

        private class Person5
        {
            public IList<Address5> Addresses { get; set; }
        }

        private class Address5
        {
            public int Zip { get; set; }

            [StringLength(3)]
            public string Street { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_UsesCustomIndexes_AddsErrorsWithCorrectKeys()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person5)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                var formCollection = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "Addresses.index", new [] { "Key1" } },
                    { "Addresses[Key1].Street", new [] { "Street1" } },
                });

                request.Form = formCollection;
                request.ContentType = "application/x-www-form-urlencoded";
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.IsType<Person5>(modelBindingResult.Model);

            Assert.False(modelState.IsValid);

            var kvp = Assert.Single(modelState);
            Assert.Equal("Addresses[Key1].Street", kvp.Key);
            var entry = kvp.Value;
            var error = Assert.Single(entry.Errors);
            Assert.Equal("The field Street must be a string with a maximum length of 3.", error.ErrorMessage);
        }

        [Theory]
        [InlineData("?[0].Street=LongStreet")]
        [InlineData("?index=low&[low].Street=LongStreet")]
        [InlineData("?parameter[0].Street=LongStreet")]
        [InlineData("?parameter.index=low&parameter[low].Street=LongStreet")]
        public async Task CollectionModelBinder_BindsCollectionOfComplexType_ImpliedPrefix_FindsValidationErrors(
            string queryString)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<Address5>),
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var model = Assert.IsType<List<Address5>>(modelBindingResult.Model);
            var address = Assert.Single(model);
            Assert.Equal("LongStreet", address.Street);

            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState).Value;
            var error = Assert.Single(entry.Errors);
            Assert.Equal("The field Street must be a string with a maximum length of 3.", error.ErrorMessage);
        }

        // parameter type, form content, expected type
        public static TheoryData<Type, IDictionary<string, StringValues>, Type> CollectionTypeData
        {
            get
            {
                return new TheoryData<Type, IDictionary<string, StringValues>, Type>
                {
                    {
                        typeof(IEnumerable<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ICollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(IList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(List<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(ClosedGenericCollection)
                    },
                    {
                        typeof(ClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(ClosedGenericList)
                    },
                    {
                        typeof(ExplicitClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(ExplicitClosedGenericCollection)
                    },
                    {
                        typeof(ExplicitClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(ExplicitClosedGenericList)
                    },
                    {
                        typeof(ExplicitCollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(ExplicitCollection<string>)
                    },
                    {
                        typeof(ExplicitList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(ExplicitList<string>)
                    },
                    {
                        typeof(IEnumerable<string>),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ICollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(IList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(List<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(ClosedGenericCollection)
                    },
                    {
                        typeof(ClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(ClosedGenericList)
                    },
                    {
                        typeof(ExplicitClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(ExplicitClosedGenericCollection)
                    },
                    {
                        typeof(ExplicitClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(ExplicitClosedGenericList)
                    },
                    {
                        typeof(ExplicitCollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(ExplicitCollection<string>)
                    },
                    {
                        typeof(ExplicitList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(ExplicitList<string>)
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CollectionTypeData))]
        public async Task CollectionModelBinder_BindsParameterToExpectedType(
            Type parameterType,
            Dictionary<string, StringValues> formContent,
            Type expectedType)
        {
            // Arrange
            var expectedCollection = new List<string> { "hello", "world" };
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = parameterType,
            };

            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.Form = new FormCollection(formContent);
            });
            var modelState = operationContext.ActionContext.ModelState;

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, operationContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            Assert.IsType(expectedType, modelBindingResult.Model);

            var model = modelBindingResult.Model as IEnumerable<string>;
            Assert.NotNull(model); // Guard
            Assert.Equal(expectedCollection, model);

            Assert.True(modelState.IsValid);
            Assert.NotEmpty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
        }

        private class ClosedGenericCollection : Collection<string>
        {
        }

        private class ClosedGenericList : List<string>
        {
        }

        private class ExplicitClosedGenericCollection : ICollection<string>
        {
            private List<string> _data = new List<string>();

            int ICollection<string>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<string>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<string>.Add(string item)
            {
                _data.Add(item);
            }

            void ICollection<string>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<string>.Contains(string item)
            {
                throw new NotImplementedException();
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            bool ICollection<string>.Remove(string item)
            {
                throw new NotImplementedException();
            }
        }

        private class ExplicitClosedGenericList : IList<string>
        {
            private List<string> _data = new List<string>();

            string IList<string>.this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            int ICollection<string>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<string>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<string>.Add(string item)
            {
                _data.Add(item);
            }

            void ICollection<string>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<string>.Contains(string item)
            {
                throw new NotImplementedException();
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            int IList<string>.IndexOf(string item)
            {
                throw new NotImplementedException();
            }

            void IList<string>.Insert(int index, string item)
            {
                throw new NotImplementedException();
            }

            bool ICollection<string>.Remove(string item)
            {
                throw new NotImplementedException();
            }

            void IList<string>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }
        }

        private class ExplicitCollection<T> : ICollection<T>
        {
            private List<T> _data = new List<T>();

            int ICollection<T>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<T>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<T>.Add(T item)
            {
                _data.Add(item);
            }

            void ICollection<T>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<T>.Contains(T item)
            {
                throw new NotImplementedException();
            }

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            bool ICollection<T>.Remove(T item)
            {
                throw new NotImplementedException();
            }
        }

        private class ExplicitList<T> : IList<T>
        {
            private List<T> _data = new List<T>();

            T IList<T>.this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            int ICollection<T>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<T>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<T>.Add(T item)
            {
                _data.Add(item);
            }

            void ICollection<T>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<T>.Contains(T item)
            {
                throw new NotImplementedException();
            }

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            int IList<T>.IndexOf(T item)
            {
                throw new NotImplementedException();
            }

            void IList<T>.Insert(int index, T item)
            {
                throw new NotImplementedException();
            }

            bool ICollection<T>.Remove(T item)
            {
                throw new NotImplementedException();
            }

            void IList<T>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }
        }
    }
}