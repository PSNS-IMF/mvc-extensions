using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.ComponentModel.DataAnnotations;

using Psns.Common.Test.BehaviorDrivenDevelopment;
using Psns.Common.Mvc.Extensions;
using Psns.Common.Persistence.Definitions;
using Psns.Common.Web.Adapters;

namespace MvcExtensions.UnitTests.CollectionPropertyModelBinderTests
{
    public class CollectionType : IIdentifiable
    {
        public int Id { get; set; }
    }

    public class EntityWithCollectionProperty : IIdentifiable
    {
        public EntityWithCollectionProperty()
        {
            CollectionTypes = new List<CollectionType>();
        }

        public int Id { get; set; }

        [Required]
        public string Required { get; set; }

        [Required]
        public int? NullableInt { get; set; }

        public ICollection<CollectionType> CollectionTypes { get; set; }
    }

    public class WhenWorkingWithTheCollectionPropertyModelBinder : BehaviorDrivenDevelopmentCaseTemplate
    {
        protected CollectionPropertyModelBinder<EntityWithCollectionProperty> ModelBinder;
        protected ControllerContext ControllerContext;
        protected ModelBindingContext BindingContext;
        protected Mock<ControllerBase> ControllerBase;
        protected Mock<IRepositoryFactory> MockRepositoryFactory;
        protected Mock<IDependencyResolver> MockDependencyResolver;
        protected NameValueCollection Form;
        protected EntityWithCollectionProperty Entity;

        public override void Arrange()
        {
            base.Arrange();

            ControllerBase = new Mock<ControllerBase>();

            Form = new NameValueCollection();

            var mockHttpRequest = new Mock<HttpRequestBase>();
            mockHttpRequest.Setup(r => r.Form)
                .Returns(Form);

            var mockHttpContext = new Mock<HttpContextBase>();
            mockHttpContext
                .Setup(ctx => ctx.Request)
                .Returns(mockHttpRequest.Object);

            var requestContext = new RequestContext(mockHttpContext.Object, new RouteData());

            ControllerContext = new ControllerContext(requestContext, ControllerBase.Object);
            ModelBinder = new CollectionPropertyModelBinder<EntityWithCollectionProperty>();

            Form["Required"] = "fullfilled";
            Form["CollectionTypes"] = "1";
            Form["NullableInt"] = "345";

            SetBindingContext();

            var mockRepository = new Mock<IRepository<CollectionType>>();
            mockRepository.Setup(r => r.Find(1)).Returns(new CollectionType { Id = 1 });

            MockRepositoryFactory = new Mock<IRepositoryFactory>();
            MockRepositoryFactory.Setup(f => f.Get<CollectionType>()).Returns(mockRepository.Object);

            MockDependencyResolver = new Mock<IDependencyResolver>();

            MockDependencyResolver.Setup(r => r.GetService(typeof(IRepositoryFactory)))
                .Returns(MockRepositoryFactory.Object);

            DependencyResolverAdapter.Resolver = MockDependencyResolver.Object;
        }

        protected void SetBindingContext()
        {
            BindingContext = new ModelBindingContext
            {
                ModelName = "",
                ValueProvider = new NameValueCollectionValueProvider(Form, System.Globalization.CultureInfo.CurrentCulture),
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(EntityWithCollectionProperty))
            };
        }

        public override void Act()
        {
            Entity = ModelBinder.BindModel(ControllerContext, BindingContext) as EntityWithCollectionProperty;
        }
    }


    [TestClass]
    public class AndCallingBindModelForAValidEntityWithCollectionProperties : WhenWorkingWithTheCollectionPropertyModelBinder
    {
        [TestMethod]
        public void ThenTheEntityShouldHaveTheRightCollectionTypesWithNoValidationErrors()
        {
            Assert.AreEqual(1, Entity.CollectionTypes.ElementAt(0).Id);
            Assert.IsTrue(BindingContext.ModelState.Values.Count == 0);
        }
    }

    [TestClass]
    public class AndCallingBindModelForAnInvalidEntityWithCollectionProperties : WhenWorkingWithTheCollectionPropertyModelBinder
    {
        public override void Arrange()
        {
            base.Arrange();

            Form.Remove("Required");
            Form.Remove("NullableInt");

            SetBindingContext();
        }

        [TestMethod]
        public void ThenTheEntityShouldHaveTheRightCollectionTypesAndValidationErrors()
        {
            Assert.AreEqual(1, Entity.CollectionTypes.ElementAt(0).Id);
            Assert.IsTrue(BindingContext.ModelState.Values.Count == 2);
        }
    }

    [TestClass]
    public class AndCallingBindModelForATechSheetWithoutVariationIds : WhenWorkingWithTheCollectionPropertyModelBinder
    {
        public override void Arrange()
        {
            base.Arrange();

            Form.Clear();

            SetBindingContext();
        }

        [TestMethod]
        public void ThenTheEntityShouldHaveNoCollectionTypes()
        {
            Assert.AreEqual(0, Entity.CollectionTypes.Count);
        }
    }

    [TestClass]
    public class AndCallingBindModelForATechSheetWithNonIntegerVariationIds : WhenWorkingWithTheCollectionPropertyModelBinder
    {
        public override void Arrange()
        {
            base.Arrange();

            Form["CollectionTypes"] = "a";

            SetBindingContext();
        }

        [TestMethod]
        public void ThenTheEntityShouldHaveNoCollectionTypes()
        {
            Assert.AreEqual(0, Entity.CollectionTypes.Count);
        }
    }
}