using ExpressionBuilder.Common;
using ExpressionBuilder.Exceptions;
using ExpressionBuilder.Generics;
using ExpressionBuilder.Operations;
using ExpressionBuilder.Test.Models;
using ExpressionBuilder.Test.Unit.Helpers;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionBuilder.Test.Integration
{
    [TestFixture]
    public class BuilderTest
    {
        private List<Person> _people;

        public List<Person> People
        {
            get
            {
                if (_people == null)
                {
                    _people = new TestData().People;
                }

                return _people;
            }
        }

        private List<Company> _companies;

        public List<Company> Companies
        {
            get
            {
                if (_companies == null)
                {
                    _companies = new TestData().Companies;
                }

                return _companies;
            }
        }


        private List<SimpleList> _simpleLists;

        public List<SimpleList> SimpleLists
        {
            get
            {
                if (_simpleLists == null)
                {
                    _simpleLists = new TestData().SimpleLists;
                }

                return _simpleLists;
            }
        }

        [TestCase(TestName = "Build expression from an empty filter: should return all records")]
        public void BuilderWithEmptyFilter()
        {
            var filter = new Filter<Person>();
            var people = People.Where(filter);
            var solution = People;
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Build expression from a filter with simple statements")]
        public void BuilderWithSimpleFilterStatements()
        {
            var filter = new Filter<Person>();
            filter.By("Name", Operation.EndsWith, "Doe").Or.By("Gender", Operation.EqualTo, PersonGender.Female);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Name.Trim().ToLower().EndsWith("doe") ||
                                             p.Gender == PersonGender.Female);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Build expression from a filter casting the value to object")]
        public void BuilderCastingTheValueToObject()
        {
            var filter = new Filter<Person>();
            filter.By("Id", Operation.GreaterThan, (object)2);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Id > 2);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Build expression from a filter with property chain filter statements")]
        public void BuilderWithPropertyChainFilterStatements()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Country", Operation.EqualTo, "usa", default(string), Connector.Or);
            filter.By("Birth.Date", Operation.LessThanOrEqualTo, new DateTime(1980, 1, 1), DateTime.MinValue, Connector.Or);
            filter.By("Name", Operation.Contains, "Doe");
            var people = People.Where(filter);
            var solution = People.Where(p => (p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim().ToLower().Equals("usa")) ||
                                             (p.Birth != null && p.Birth.Date <= new DateTime(1980, 1, 1)) ||
                                             p.Name.Trim().ToLower().Contains("doe"));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Build expression from a filter with property list filter statements")]
        public void BuilderWithPropertyListFilterStatements()
        {
            var filter = new Filter<Person>();
            filter.By("Contacts[Type]", Operation.EqualTo, ContactType.Email).And.By("Birth.Country", Operation.StartsWith, " usa ");
            var people = People.Where(filter);
            var solution = People.Where(p => p.Contacts.Any(c => c.Type == ContactType.Email) &&
                                             (p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim().ToLower().StartsWith("usa")));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Build expression from a filter statement with a list of values")]
        public void BuilderWithFilterStatementWithListOfValues()
        {
            var filter = new Filter<Person>();
            filter.By("Id", Operation.In, new[] { 1, 2, 4, 5 });
            var people = People.Where(filter);
            var solution = People.Where(p => new[] { 1, 2, 4, 5 }.Contains(p.Id));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder with a single filter statement using a between operation")]
        public void BuilderWithSingleFilterStatementWithBetween()
        {
            var filter = new Filter<Person>();
            filter.By("Id", Operation.Between, 2, 4);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Id >= 2 && p.Id <= 4);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder with a single filter statement using a between operation and a simple statement")]
        public void BuilderWithBetweenAndSimpleFilterStatements()
        {
            var filter = new Filter<Person>();
            filter.By("Id", Operation.Between, 2, 6).And.By("Birth.Country", Operation.EqualTo, " usa ");
            var people = People.Where(filter);
            var solution = People.Where(p => (p.Id >= 2 && p.Id <= 6) &&
                                             (p.Birth != null && p.Birth.Country.Trim().ToLower().StartsWith("usa")));
            Assert.That(people, Is.EquivalentTo(solution));
            Assert.That(people.All(p => p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim().ToLower() == "usa"), Is.True);
        }

        [TestCase(TestName = "Builder with a single filter statement using a between operation and a list of values statement")]
        public void BuilderWithBetweenAndListOfValuesFilterStatements()
        {
            var filter = new Filter<Person>();
            filter.By("Id", Operation.Between, 2, 6).And.By("Id", Operation.In, new[] { 4, 5 });
            var people = People.Where(filter);
            var solution = People.Where(p => (p.Id >= 2 && p.Id <= 6) &&
                                             new[] { 4, 5 }.Contains(p.Id));
            Assert.That(people, Is.EquivalentTo(solution));
            Assert.That(people.Min(p => p.Id), Is.EqualTo(4));
        }

        [TestCase(TestName = "Builder using 'IsNull' operator")]
        public void BuilderUsingIsNullOperation()
        {
            var filter = new Filter<Person>();
            filter.By("Employer", Operation.IsNull);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Employer == null);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsNull' operator on an inner property")]
        public void BuilderUsingIsNullOperationOnAnInnerProperty()
        {
            var filter = new Filter<Person>();
            filter.By("Employer.Name", Operation.IsNull);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Employer == null || (p.Employer != null && p.Employer.Name == null));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsNotNull' operator")]
        public void BuilderUsingIsNotNullOperation()
        {
            var filter = new Filter<Person>();
            filter.By("Employer", Operation.IsNotNull);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Employer != null);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsNotNull' operator on an inner property")]
        public void BuilderUsingIsNotNullOperationOnAnInnerProperty()
        {
            var filter = new Filter<Person>();
            filter.By("Employer.Name", Operation.IsNotNull);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Employer != null && p.Employer.Name != null);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsEmpty' operator")]
        public void BuilderUsingIsEmptyOperation()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Country", Operation.IsEmpty, null, (object)null, Connector.And);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim() == string.Empty);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsNotEmpty' operator")]
        public void BuilderUsingIsNotEmptyOperation()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Country", Operation.IsNotEmpty);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim() != string.Empty);
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsNullOrWhiteSpace' operator")]
        public void BuilderUsingIsNullOrWhiteSpaceOperation()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Country", Operation.IsNullOrWhiteSpace);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Birth != null && string.IsNullOrWhiteSpace(p.Birth.Country));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using 'IsNotNullNorWhiteSpace' operator")]
        public void BuilderUsingIsNotNullNorWhiteSpaceOperation()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Country", Operation.IsNotNullNorWhiteSpace);
            var people = People.Where(filter);
            var solution = People.Where(p => p.Birth != null && !string.IsNullOrWhiteSpace(p.Birth.Country));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder with wrong number of values when expecting no values at all")]
        public void BuilderWithWrongNumberOrValuesWhenExpectingNoValuesAtAll()
        {
            var filter = new Filter<Person>();
            var ex = Assert.Throws<WrongNumberOfValuesException>(() => filter.By("Id", Operation.IsNull, 1, 2));
            Assert.That(ex.Message, Does.Match(@"The operation '\w*' admits exactly '\w*' values \(not more neither less than this\)."));
        }

        [TestCase(TestName = "Builder with wrong number of values when expecting just one value")]
        public void BuilderWithWrongNumberOrValuesWhenExpectingJustOneValue()
        {
            var filter = new Filter<Person>();
            var ex = Assert.Throws<WrongNumberOfValuesException>(() => filter.By("Id", Operation.EqualTo, 1, 2));
            Assert.That(ex.Message, Does.Match(@"The operation '\w*' admits exactly '\w*' values \(not more neither less than this\)."));
        }

        [TestCase(TestName = "Builder with operation not supported by specific type")]
        public void BuilderWithOperationNotSupportedBySpecificType()
        {
            var filter = new Filter<Person>();
            var ex = Assert.Throws<UnsupportedOperationException>(() => filter.By("Name", Operation.GreaterThan, "John"));
            Assert.That(ex.Message, Does.Match(@"The type '\w*' does not have support for the operation '\w*'."));
        }

        [TestCase(TestName = "Builder working with nullable values")]
        public void BuilderWithNullableValues()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Date", Operation.IsNotNull)
                  .Or.By("Birth.Date", Operation.GreaterThan, new DateTime(1980, 1, 1));
            var people = People.Where(filter);
            var solution = People.Where(p => (p.Birth != null && p.Birth.Date != null)
                                            || (p.Birth != null && p.Birth.Date.HasValue && p.Birth.Date > new DateTime(1980, 1, 1)));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder working with custom supported type")]
        public void BuilderUsingCustomSupportedType()
        {
            var dateOffset = new DateTimeOffset(new DateTime(1980, 1, 1));
            var filter = new Filter<Person>();
            filter.By("Birth.DateOffset", Operation.GreaterThan, dateOffset);
            var people = People.Where(filter);
            var solution = People.Where(p => (p.Birth != null && p.Birth.DateOffset.HasValue && p.Birth.DateOffset > dateOffset));
            Assert.That(people, Is.EquivalentTo(solution));
        }

        private IQueryable<Person> GetPeople()
        {
            var filter = new Filter<Person>();
            filter.By("Employer.Name", Operation.IsNotNull);
            return People.AsQueryable().Where(filter);
        }

        [TestCase(TestName = "Builder using IQueryable")]
        public void BuilderUsingIQueryable()
        {
            var people = GetPeople();
            Assert.That(people, Is.InstanceOf<IQueryable<Person>>());

            var solution = People.AsQueryable().Where(p => p.Employer != null && p.Employer.Name != null).OrderByDescending(p => p.Name).Take(1);
            var person = (people as IQueryable<Person>).OrderByDescending(p => p.Name).Take(1);
            Assert.That(person.Count(), Is.EqualTo(1));
            Assert.That(person, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using complex expressions (fluent interface)", Category = "ComplexExpressions")]
        public void BuilderUsingComplexExpressionsFluentInterface()
        {
            var filter = new Filter<Person>();
            filter.By("Birth.Country", Operation.EqualTo, "USA").And.By("Name", Operation.DoesNotContain, "doe")
                .Or
                .Group.By("Name", Operation.EndsWith, "Doe").And.By("Birth.Country", Operation.IsNullOrWhiteSpace);
            var people = People.Where(filter);
            var solution = People.Where(p => ((p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim().ToLower() == "usa") && !p.Name.Contains("Doe"))
                                || (p.Name.EndsWith("Doe") && (p.Birth != null && string.IsNullOrWhiteSpace(p.Birth.Country))));

            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder using complex expressions", Category = "ComplexExpressions")]
        public void BuilderUsingComplexExpressions()
        {
            var filter = new Filter<Person>();
            filter.StartGroup();
            filter.By("Birth.Country", Operation.EqualTo, "USA", default(string), Connector.And);
            filter.By("Name", Operation.DoesNotContain, "doe", default(string), Connector.Or);
            filter.StartGroup();
            filter.By("Name", Operation.EndsWith, "Doe", default(string), Connector.And);
            filter.By("Birth.Country", Operation.IsNullOrWhiteSpace, default(string), default(string), Connector.And);
            var people = People.Where(filter);
            var solution = People.Where(p => ((p.Birth != null && p.Birth.Country != null && p.Birth.Country.Trim().ToLower() == "usa") && !p.Name.Contains("Doe"))
                                || (p.Name.EndsWith("Doe") && (p.Birth != null && string.IsNullOrWhiteSpace(p.Birth.Country))));

            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Builder not using complex expressions (different results)", Category = "ComplexExpressions")]
        public void BuilderNotUsingComplexExpressions()
        {
            var filter = new Filter<Person>();
            filter.By("Salary", Operation.GreaterThanOrEqualTo, 4000D).And.By("Salary", Operation.LessThanOrEqualTo, 5000D)
                .And
                .By("Birth.Country", Operation.EqualTo, "USA").Or.By("Birth.Country", Operation.EqualTo, "AUS");
            var people = People.Where(filter);
            var solution = People.Where(p => ((p.Birth != null && p.Birth.Country == "USA") || (p.Birth != null && p.Birth.Country == "AUS"))
                                             && (p.Salary >= 4000 && p.Salary <= 5000));

            Assert.That(people, Is.Not.EquivalentTo(solution));
        }

        [TestCase(TestName = "Property value type mismatch with an operation that expects one value")]
        public void PropertyValueTypeMismatchWithOneValue()
        {
            var filter = new Filter<Person>();
            filter.By("Name", Operation.EqualTo, 1);
            var ex = Assert.Throws<PropertyValueTypeMismatchException>(() => People.Where(filter));
            ex.Message.Should().Be("The type of the member 'Name' (String) is different from the type of one of the constants (Int32)");
        }

        [TestCase(TestName = "Property value type mismatch with an operation that expects two values")]
        public void PropertyValueTypeMismatchWithTwoValues()
        {
            var filter = new Filter<Person>();
            filter.By("Id", Operation.Between, 1, 7700000000000007D);
            var ex = Assert.Throws<PropertyValueTypeMismatchException>(() => People.Where(filter));
            ex.Message.Should().Be("The type of the member 'Id' (Int32) is different from the type of one of the constants (Double)");
        }

        [TestCase(TestName = "Should not throw an exception when using the 'In' operator over a list of nullable objects")]
        public void ShouldNotThrowExceptionWhenUsingTheInOperatorOverListOfNullableObjects()
        {
            var filter = new Filter<Person>();
            var idList = new long?[] { 123 };
            filter.By("EmployeeReferenceNumber", Operation.In, idList);
            var result = People.Where(filter);
            result.Should().NotBeEmpty();
        }

        [TestCase(TestName = "Nested property with depth of two", Category = "NestedProperties")]
        public void NestedPropertyDepthTwo()
        {
            var filter = new Filter<Person>();
            filter.By("Manager.Birth.Country", Operation.EqualTo, "USA");
            var people = People.Where(filter);
            var solution = People.Where(p => p.Manager != null && p.Manager.Birth != null && p.Manager.Birth.Country == "USA");

            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Nested property with depth of three", Category = "NestedProperties")]
        public void NestedPropertyDepthThree()
        {
            var filter = new Filter<Person>();
            filter.By("Manager.Employer.Owner.Name", Operation.Contains, "smith");
            var people = People.Where(filter);
            var solution = People.Where(p => p.Manager != null && p.Manager.Employer != null
                                                               && p.Manager.Employer.Owner.Name.Trim().ToLower().Contains("smith"));

            Assert.That(people, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Nested list property with depth of two", Category = "NestedProperties")]
        public void NestedListPropertyDepthTwo()
        {
            var filter = new Filter<Company>();
            filter.By("Managers[Birth.Country]", Operation.EqualTo, "USA");
            var companies = Companies.Where(filter);
            var solution = Companies.Where(c => c.Managers.Any(p => p.Birth != null && p.Birth.Country == "USA"));

            Assert.That(companies, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "Nested list property with depth of three", Category = "NestedProperties")]
        public void NestedListPropertyDepthThree()
        {
            var filter = new Filter<Company>();
            filter.By("Managers[Employer.Owner.Name]", Operation.Contains, "smith");
            var companies = Companies.Where(filter);
            var solution = Companies.Where(c => c.Managers.Any(p => p.Employer != null
                                                                    && p.Employer.Owner.Name.Trim().ToLower().Contains("smith")));

            Assert.That(companies, Is.EquivalentTo(solution));
        }


        #region List of strings

        [TestCase(TestName = "List of strings (StartsWith)", Category = "NestedProperties")]
        public void SimpleListStringsStartsWith()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.StartsWith, "a");
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x.StartsWith("a")));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (EndsWith)", Category = "NestedProperties")]
        public void SimpleListStringsEndsWith()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.EndsWith, "z");
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x.EndsWith("z")));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (EqualTo)", Category = "NestedProperties")]
        public void SimpleListStringsEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.EqualTo, "def");
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x == "def"));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (NotEqualTo)", Category = "NestedProperties")]
        public void SimpleListStringsNotEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.NotEqualTo, "def");
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x != "def"));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (Contains)", Category = "NestedProperties")]
        public void SimpleListStringsContains()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.Contains, "de");
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x.Contains("de")));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (DoesNotContain)", Category = "NestedProperties")]
        public void SimpleListStringsDoesNotContain()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.DoesNotContain, "de");
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && !x.Contains("de")));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (IsEmpty)", Category = "NestedProperties")]
        public void SimpleListStringsIsEmpty()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.IsEmpty);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x == ""));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (IsNotEmpty)", Category = "NestedProperties")]
        public void SimpleListStringsIsNotEmpty()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.IsNotEmpty);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x.Trim().ToLower() != string.Empty));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (IsNull)", Category = "NestedProperties")]
        public void SimpleListStringsIsNull()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.IsNull);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x == null));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (IsNotNull)", Category = "NestedProperties")]
        public void SimpleListStringsIsNotNull()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.IsNotNull);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (IsNullOrWhiteSpace)", Category = "NestedProperties")]
        public void SimpleListStringsIsNullOrWhiteSpace()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.IsNullOrWhiteSpace);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x == null || x.Trim().ToLower() == string.Empty));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of strings (IsNotNullNorWhiteSpace)", Category = "NestedProperties")]
        public void SimpleListStringsIsNotNullNorWhiteSpace()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Strings[]", Operation.IsNotNullNorWhiteSpace);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Strings.Any(x => x != null && x.Trim().ToLower() != string.Empty));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        #endregion

        #region List of integers

        [TestCase(TestName = "List of integeres (EqualTo)", Category = "NestedProperties")]
        public void SimpleListIntegersEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.EqualTo, 100);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x == 100));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (NotEqualTo)", Category = "NestedProperties")]
        public void SimpleListIntegersNotEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.NotEqualTo, 100);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x != 100));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (Between)", Category = "NestedProperties")]
        public void SimpleListIntegersBetween()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.Between, 100, 200);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x >= 100 && x <= 200));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (GreaterThan)", Category = "NestedProperties")]
        public void SimpleListIntegersGreaterThan()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.GreaterThan, 200);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x > 200));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (GreaterThanOrEqualTo)", Category = "NestedProperties")]
        public void SimpleListIntegersGreaterThanOrEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.GreaterThanOrEqualTo, 300);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x >= 300));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (LessThan)", Category = "NestedProperties")]
        public void SimpleListIntegersLessThan()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.LessThan, 200);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x < 200));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (LessThanOrEqualTo)", Category = "NestedProperties")]
        public void SimpleListIntegersLessThanOrEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Integers[]", Operation.LessThanOrEqualTo, 200);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => x <= 200));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (In)", Category = "NestedProperties")]
        public void SimpleListIntegersIn()
        {
            var filter = new Filter<SimpleList>();
            var range = new int[] { 3, 300 };
            filter.By("Integers[]", Operation.In, range);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => range.Contains(x)));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of integeres (NotIn)", Category = "NestedProperties")]
        public void SimpleListIntegersNotIn()
        {
            var filter = new Filter<SimpleList>();
            var range = new int[] { 3, 300 };
            filter.By("Integers[]", Operation.NotIn, range);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Integers.Any(x => !range.Contains(x)));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        #endregion

        #region List of booleans

        [TestCase(TestName = "List of booleans (EqualTo)", Category = "NestedProperties")]
        public void SimpleListBooleansEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Booleans[]", Operation.EqualTo, true);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Booleans.Any(x => x == true));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of booleans (NotEqualTo)", Category = "NestedProperties")]
        public void SimpleListBooleansNotEqualTo()
        {
            var filter = new Filter<SimpleList>();
            filter.By("Booleans[]", Operation.NotEqualTo, true);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Booleans.Any(x => x != true));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of booleans (In)", Category = "NestedProperties")]
        public void SimpleListBooleansIn()
        {
            var filter = new Filter<SimpleList>();
            var range = new bool[] { true };
            filter.By("Booleans[]", Operation.In, range);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Booleans.Any(x => range.Contains(x)));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        [TestCase(TestName = "List of booleans (NotIn)", Category = "NestedProperties")]
        public void SimpleListBooleansNotIn()
        {
            var filter = new Filter<SimpleList>();
            var range = new bool[] { true };
            filter.By("Booleans[]", Operation.NotIn, range);
            var result = SimpleLists.Where(filter);
            var solution = SimpleLists.Where(l => l.Booleans.Any(x => !range.Contains(x)));
            Assert.That(result, Is.EquivalentTo(solution));
        }

        #endregion
    }
}