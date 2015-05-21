using System;
using System.Linq;
using System.Web.Mvc;

using Psns.Common.Persistence.Definitions;
using Psns.Common.Web.Adapters;

namespace Psns.Common.Mvc.Extensions
{
    /// <summary>
    /// Binds Collection Properties of TEntity by getting an IRepositoryFactory from the DependencyResolver and
    /// using the IRepository of the Collection Property Type to find build the collection.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class CollectionPropertyModelBinder<TEntity> : DefaultModelBinder 
        where TEntity : class, new()
    {
        IRepositoryFactory _repositoryFactory;

        public override object BindModel(ControllerContext controllerContext, 
            ModelBindingContext bindingContext)
        {
            TEntity entity = base.BindModel(controllerContext, bindingContext) as TEntity;
            _repositoryFactory = DependencyResolverAdapter.Resolver.GetService<IRepositoryFactory>();

            foreach(var property in entity.GetEnumerableProperties())
            {
                Type[] collectionTypes = property.PropertyType.GetGenericArguments();
                if(collectionTypes.Length > 0)
                {
                    var providerResult = bindingContext.ValueProvider.GetValue(property.Name);
                    if(providerResult != null)
                    {
                        string[] ids = providerResult.RawValue as string[];
                        if(ids != null)
                        {
                            var targetType = property.PropertyType.GetGenericArguments()[0];
                            var repositoryGetMethod = _repositoryFactory.GetType().GetMethod("Get");
                            var genericGetMethod = repositoryGetMethod.MakeGenericMethod(targetType);
                            var repository = genericGetMethod.Invoke(_repositoryFactory, null);
                            var collection = property.GetValue(entity, null);

                            var find = repository.GetType().GetMethods().Where(m => m.Name.Equals("Find") && 
                                m.ReturnType == targetType).Single();

                            var add = collection.GetType().GetMethod("Add");

                            foreach(var idString in ids)
                            {
                                int id;
                                if(int.TryParse(idString, out id))
                                {
                                    var collectionInstance = find.Invoke(repository, 
                                        new object[] { new object[] { id } });

                                    var collectionEntity = add.Invoke(collection, 
                                        new object[] { collectionInstance });
                                }
                            }
                        }
                    }
                }
            }

            bindingContext.ModelState.Clear();
            var metaData = ModelMetadataProviders.Current.GetMetadataForType(() => entity, typeof(TEntity));
            var validator = ModelValidator.GetModelValidator(metaData, controllerContext);
            validator.Validate(null).ToList().ForEach(result =>
            {
                bindingContext.ModelState.AddModelError(result.MemberName, result.Message);
            });

            return entity;
        }
    }
}